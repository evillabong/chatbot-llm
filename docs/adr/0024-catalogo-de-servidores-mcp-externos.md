# 0024. Catálogo de servidores MCP externos por organización

- Estado: Aceptado
- Fecha: 2026-06-20
- Decisores: evill

## Contexto

El [anexo técnico de integraciones](../anexo-tecnico-integraciones.md) §4 describe que el asistente
podrá ejecutar **herramientas (tool-use)** vía **MCP (Model Context Protocol)**, y que cada
organización podrá **registrar servidores MCP de terceros** para sumar capacidades sin desarrollarlas
en el producto. El gateway de IA media y audita la invocación; nada se invoca sin estar declarado,
habilitado y en una allowlist. Es la pendiente #23.

La invocación efectiva (el gateway ofreciendo las herramientas al modelo y ejecutándolas) depende del
LLM y del cliente MCP. Este corte entrega la **base imprescindible y verificable sin LLM**: el
**catálogo** por tenant.

## Decisión

- **Entidad `McpServer`** en el **esquema del tenant** (`mcp_servers`): nombre (único por tenant),
  `Endpoint` (URL absoluta http/https), `AuthToken` (cifrado, write-only), `AllowedTools` (allowlist,
  **deny-by-default**: vacío = ninguna), `IsEnabled`, `TimeoutSeconds`. Aislado por organización
  (ADR 0009); nunca cross-tenant.
- **Endpoints `/mcp-servers`** (política **TenantAdmin**): listar, crear, actualizar (`?id`), eliminar
  (`?id`). Convención del repo: query/headers/body, sin parámetros de dominio en la ruta.
- **Secreto cifrado en reposo y write-only** (ADR 0010, `ISecretProtector`): el `AuthToken` se cifra al
  guardar, **no** se devuelve nunca (la respuesta expone solo `HasAuthToken`), y un guardado en blanco
  **conserva** el token existente (no lo borra) — mismo patrón que el token de canal de Telegram.
- **Validación de entrada:** endpoint debe ser URL absoluta http/https (→ 400); nombre duplicado → 409;
  allowlist normalizada (trim, sin vacíos, sin duplicados); `TimeoutSeconds` acotado a 1–120.
- **UI `/mcp-servers`** (`Mimo.App`): alta/edición/borrado con allowlist por CSV y token write-only.

## Consecuencias

### Positivas

- Habilita registrar capacidades MCP por organización de forma segura **antes** de cablear la
  invocación. **Verificado E2E:** crear (con token + tools); endpoint inválido → 400; nombre duplicado
  → 409; el token **no** aparece en las respuestas y queda **cifrado en reposo** (blob de Data
  Protection, no texto plano); guardado en blanco conserva el token; rotación lo reemplaza; allowlist
  deduplicada y timeout acotado; authz (sin token → 401); borrado 204 → 404.
- Deny-by-default en la allowlist y aislamiento por tenant alinean con los controles de seguridad ya
  usados (anti-SSRF del chatbot, cifrado de secretos).

### Negativas / Costos

- **No** incluye la invocación: el gateway aún no ofrece estas herramientas al modelo ni las ejecuta.
  Es el **corte 2** (requiere LLM + cliente MCP) y deberá aplicar los mismos controles anti-SSRF que
  `ChatbotApiCaller` (allow-list de hosts, bloqueo de IPs internas/metadata, sin redirecciones, límites)
  al llamar al endpoint del servidor.
- Tampoco incluye **scopes por herramienta** ni cuotas/auditoría de invocación (se sumarán con la
  ejecución).

## Alternativas consideradas

- **Guardar la config MCP en `TenantConfiguration` (jsonb global):** descartado; son varias entradas por
  tenant con secreto propio y consultas por estado/nombre — una tabla en el esquema del tenant encaja
  mejor y mantiene el secreto cifrado por fila.
- **Registrar servidores MCP a nivel plataforma (compartidos):** descartado por aislamiento; las
  capacidades y credenciales son de cada organización.
