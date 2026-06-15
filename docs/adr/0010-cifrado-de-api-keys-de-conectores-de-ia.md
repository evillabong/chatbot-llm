# 0010. Cifrado de las API keys de conectores de IA en reposo

- Estado: Aceptado
- Fecha: 2026-06-15
- Decisores: evill

## Contexto

Las API keys de los conectores de IA se guardaban en texto plano en la columna JSONB
`ai_connectors.settings` (ver ADR 0004). Cualquiera con acceso de lectura al esquema `public`
podía verlas. Es la deuda de seguridad pendiente del modelo de IA.

Particularidad: el cifrado y el descifrado ocurren en **procesos distintos** —
`Mimo.Admin.Api` cifra al crear/editar el conector, y `Mimo.Api` descifra al invocar el LLM.

## Decisión

Cifrar la API key en reposo con **ASP.NET Core Data Protection** mediante una abstracción
`ISecretProtector` (Core) implementada por `DataProtectionSecretProtector` (Infrastructure).

- Se cifra en los puntos de escritura: endpoints de administración de conectores y el seeder.
- Se descifra en `LlmClientFactory`, justo antes de construir el cliente del proveedor.
- Las respuestas de administración nunca exponen la key (solo `HasApiKey`).

Como dos procesos comparten los datos, ambas APIs configuran un **anillo de llaves
compartido**: mismo `SetApplicationName("MIMO")` y misma ubicación de persistencia
(`PersistKeysToFileSystem`, por defecto `%ProgramData%\MIMO\dp-keys`, configurable con
`DataProtection:KeysPath`). El script `scripts/deploy-iis.ps1` crea esa carpeta y concede
acceso a `IIS_IUSRS` para que los App Pools puedan leer/escribir las llaves.

Una key vacía se trata como "sin secreto" (no se cifra), para no romper conectores sin clave.

## Consecuencias

### Positivas

- Las API keys dejan de estar en texto plano en la BD.
- Abstracción reutilizable (`ISecretProtector`) para otros secretos en reposo.

### Negativas / Costos

- Ambas APIs dependen de un anillo de llaves compartido; si se pierde, las keys cifradas
  no se pueden descifrar (hay que volver a capturarlas).
- Bajo IIS, las identidades de los App Pools necesitan acceso a la carpeta de llaves (lo
  resuelve el script de despliegue).
- Conectores con keys en texto plano previas a este cambio deben recapturarse (en dev solo
  existía el conector sembrado con key vacía, sin impacto).

## Alternativas consideradas

- **Persistir las llaves en la BD** (PersistKeysToDbContext): centraliza el anillo y evita el
  problema de ACL de carpeta, a costa de otra tabla/migración. Reconsiderable si se complica
  el manejo de la carpeta compartida.
- **Cifrado a nivel de columna en PostgreSQL (pgcrypto)**: mueve el secreto de la clave a la
  BD/ď conexión; se prefirió Data Protection por mantener la clave fuera de la BD.
