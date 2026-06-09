# Guía de contribución

Este documento define las reglas de colaboración para el CRM omnicanal multi-tenant. Aplica a código, documentación, commits, branches, releases y cambios hechos por agentes IA.

## Principios

- La documentación y los comentarios van en español.
- Los nombres de clases, métodos, propiedades, variables, tablas y columnas van en inglés.
- La lógica multi-tenant debe preservar aislamiento de datos.
- La IA no decide visibilidad de información; el sistema filtra el contexto antes de invocar el LLM.
- Los cambios deben ser pequeños, trazables y documentados.
- No se commitean secretos, tokens, API keys, connection strings reales ni datos personales.

## Flujo de trabajo

1. Crear una rama desde `develop`.
2. Implementar el cambio con alcance claro.
3. Actualizar documentación si cambia arquitectura, flujo, configuración o comportamiento.
4. Ejecutar build y tests aplicables.
5. Abrir PR hacia `develop`.
6. Promover a `main` solo versiones estables o hotfixes.

## Branches

```text
main              producción estable
develop           integración
feature/...       nueva funcionalidad
fix/...           corrección
hotfix/...        corrección urgente desde main
docs/...          documentación
chore/...         mantenimiento
```

Formato recomendado:

```text
feature/feat-autor-descripcion
fix/fix-autor-descripcion
docs/autor-descripcion
hotfix/descripcion
```

Ejemplos:

```text
feature/feat-jvillarreal-resolucion-tenant
fix/fix-jvillarreal-search-path-postgres
docs/jvillarreal-actualizar-plan
hotfix/webhook-whatsapp-signature
```

## Commits

Formato obligatorio:

```text
<tipo>: <autor> <descripcion-en-kebab-case>
```

Tipos permitidos:

| Tipo | Uso | Impacto SemVer |
|---|---|---|
| `feat` | Funcionalidad nueva | MINOR |
| `fix` | Corrección de bug | PATCH |
| `hotfix` | Corrección urgente en producción | PATCH |
| `perf` | Mejora de rendimiento observable | PATCH |
| `refactor` | Reorganización sin funcionalidad nueva | PATCH si cambia comportamiento |
| `docs` | Solo documentación | Sin tag obligatorio |
| `test` | Pruebas | Sin tag obligatorio |
| `chore` | Configuración, dependencias, scripts | Sin tag obligatorio |
| `style` | Formato sin cambio lógico | Sin tag obligatorio |

Ejemplos correctos:

```text
docs: jvillarreal agregar-plan-arquitectura-crm
feat: jvillarreal crear-ticket-queue-service
fix: jvillarreal corregir-filtro-documentos-privados
test: jvillarreal agregar-tests-tenant-resolution
```

Ejemplos incorrectos:

```text
feat: agregar cosas
fix: JVillarreal Bug
docs: actualizar README
feat: jvillarreal add-nuevo-ticket
```

## Versionado

El proyecto usa Semantic Versioning:

```text
vMAJOR.MINOR.PATCH
```

Pre-releases:

```text
v0.1.0-alpha.1
v0.1.0-beta.1
v1.0.0-rc.1
```

Reglas:

| Cambio | Incremento |
|---|---|
| Breaking change | MAJOR |
| Funcionalidad nueva compatible | MINOR |
| Corrección o mejora menor | PATCH |
| Documentación o mantenimiento | No requiere tag |

Crear tag anotado:

```bash
git tag -a v0.1.0 -m "feat: jvillarreal crear-fundacion-multitenant"
git push origin v0.1.0
```

## Convenciones de código

### Idioma

| Elemento | Idioma |
|---|---|
| Documentación | Español |
| Comentarios XML y comentarios de código | Español |
| Clases, métodos, propiedades y variables | Inglés |
| Tablas y columnas | Inglés |
| Endpoints | Inglés, estable y descriptivo |

Ejemplo:

```csharp
/// <summary>
/// Representa una sesión de atención al ciudadano.
/// </summary>
public class Ticket
{
    public Guid Id { get; set; }
    public TicketStatus Status { get; set; }
    public Guid? AssignedAgentId { get; set; }
}
```

### Arquitectura

- `Mimo.Core` contiene modelos, enums, DTOs e interfaces.
- `Mimo.Infrastructure` contiene EF Core, repositorios, conectores, servicios externos, búsqueda vectorial, Redis y DeepSeek.
- `Mimo.Api` contiene Minimal APIs tenant-facing, hubs SignalR y middleware de tenant.
- `Mimo.Admin.Api` contiene exclusivamente los endpoints de administración de la plataforma (tenants, planes, facturación, monitoreo). Este proyecto se despliega en un host separado y no debe estar expuesto junto a `Mimo.Api`.
- Los frontends Blazor consumen su API correspondiente (`Mimo.Admin.Web` → `Mimo.Admin.Api`; los demás → `Mimo.Api`); ninguno accede a infraestructura directamente.
- La resolución de tenant debe ocurrir antes de consultar datos tenant-scoped.

## Documentación obligatoria

Actualizar documentación cuando:

| Cambio | Documento |
|---|---|
| Cambio arquitectónico | `docs/PLAN.md` |
| Nuevo endpoint o hub | Documento de API futuro |
| Cambio en flujo de sesión | `docs/PLAN.md` |
| Cambio multi-tenant | `docs/PLAN.md` |
| Cambio de contribución/versionado | `CONTRIBUTING.md` |
| Cambio visible para release | `CHANGELOG.md` |

## Seguridad

- No incluir secretos reales en archivos del repositorio.
- No enviar datos personales al LLM sin pasar por filtros del sistema.
- Validar firmas de webhooks externos.
- Registrar auditoría para acciones administrativas y transferencias.
- Aplicar aislamiento tenant por middleware y pruebas automatizadas.
- Revisar logs para evitar exposición de tokens, correos, teléfonos o identificadores sensibles.

## Tests esperados

Cuando exista la solución:

```bash
dotnet build
dotnet test
```

Áreas críticas para pruebas:

- Resolución de tenant.
- `search_path` y aislamiento de esquemas.
- Filtro de documentos públicos/privados.
- Asignación de tickets y colas.
- Transferencias entre roles/agentes.
- Validación de webhooks.
- Herramientas MCP expuestas al LLM.

## Pull requests

Checklist:

- [ ] El cambio tiene alcance claro.
- [ ] El commit cumple `<tipo>: <autor> <descripcion-en-kebab-case>`.
- [ ] La documentación fue actualizada si aplica.
- [ ] `CHANGELOG.md` fue actualizado si hay cambio relevante.
- [ ] Build y tests pasan, o se documenta por qué no se ejecutaron.
- [ ] No hay secretos ni datos sensibles en el diff.
- [ ] No se rompe aislamiento multi-tenant.

## Agentes IA

Cuando un agente IA contribuya:

- Debe respetar las mismas reglas de commits, documentación y seguridad.
- Debe evitar tocar archivos no relacionados.
- Debe explicar pruebas ejecutadas y no ejecutadas.
- Debe preservar cambios previos del usuario.

Ejemplo de commit:

```text
docs: codex crear-documentacion-inicial-proyecto
```
