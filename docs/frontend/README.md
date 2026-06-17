# Frontend de MIMO — Plan y directrices

Documento de planificación y **directrices obligatorias** del frontend. Debe leerse antes de
escribir cualquier código de UI. Decisiones de arquitectura registradas en
[ADR 0011](../adr/0011-arquitectura-frontend-blazor-wasm-aislado.md).

## 1. Objetivo y referencias

- **Norte de UX:** [Callbell](https://www.callbell.eu/es/) — atención omnicanal con bandeja
  unificada, enrutamiento, colaboración de agentes y analítica.
- **Foco de producto:** atención al cliente (núcleo). Las **ventas** son una capacidad opcional
  por tenant/plan, no el centro (ver memoria `enfoque-producto-mimo`).
- **Calidad visual:** diseño moderno, limpio, accesible y responsive.

## 2. Decisiones (confirmadas)

| Tema | Decisión |
|---|---|
| Framework | **Blazor WebAssembly standalone** (UI 100% cliente, consume las APIs por HTTP) |
| UI kit | **Tailwind CSS + Flowbite Blazor** |
| Componentización | Toda la UI vive en **`Mimo.Ui`**; las apps solo componen sus componentes |
| Cliente de API | **Generado desde OpenAPI** (Kiota) — sin escribir clientes a mano |
| Primera app | **`Mimo.App`**, empezando por el admin de tenant (CRUD) para cimentar `Mimo.Ui`, auth y patrones |

## 3. Arquitectura de proyectos

Dos apps de frontend (alineadas con los sitios IIS `mimo.app` y `mimo.admin.app`):

```text
src/
├── Mimo.Ui/            # Razor Class Library: SISTEMA DE DISEÑO.
│                       # Único proyecto que referencia Flowbite Blazor + Tailwind.
│                       # Expone componentes propios: <MimoButton>, <MimoTable>, etc.
├── Mimo.ApiClient/     # Cliente(s) tipados generados de OpenAPI (Mimo.Api y Mimo.Admin.Api)
│                       # + handlers de auth/tenant. Sin lógica de UI.
├── Mimo.App/           # WASM tenant-facing: admin de tenant + consola de agente (Callbell).
│                       # Consume Mimo.Api. Despliega al sitio IIS "mimo.app".
└── Mimo.Admin.App/     # WASM del SuperAdmin (tenants, planes, conectores de IA, uso).
                        # Consume Mimo.Admin.Api. Despliega al sitio IIS "mimo.admin.app".
```

El WebChat embebible del ciudadano se resolverá después (ruta/modo dentro de `Mimo.App` o
proyecto aparte). Regla de dependencias: `App → Mimo.Ui` y `App → Mimo.ApiClient`. **Ninguna
app referencia Flowbite ni Tailwind directamente.**

## 4. Regla de oro (directriz innegociable)

> **Cero hardcode de UI en las apps.** Las páginas de las apps NO contienen HTML crudo, clases
> de Tailwind sueltas, ni componentes de Flowbite. Solo componen componentes de `Mimo.Ui`.

- ✅ `<MimoButton Variant="Primary" OnClick="Guardar">Guardar</MimoButton>`
- ❌ `<button class="px-4 py-2 bg-blue-600 rounded ...">Guardar</button>`
- ❌ `<Flowbite.Button>...</Flowbite.Button>` dentro de una app.

Objetivo: cambiar el look & feel o la librería de componentes = tocar **solo `Mimo.Ui`**.
Si una app necesita algo que `Mimo.Ui` no ofrece, **se agrega el componente a `Mimo.Ui`**, no
se improvisa en la app. (Caveat honesto: esto aísla la *librería de componentes* y el diseño;
un cambio de *framework* Blazor→JS reescribiría `Mimo.Ui` igual.)

## 5. Sistema de diseño (en `Mimo.Ui`)

- **Design tokens** vía variables CSS + config de Tailwind: paleta (incl. semánticos
  success/warning/danger/info), tipografía, escala de espaciado, radios, sombras.
- **Tema claro/oscuro** (Flowbite soporta `dark:`); preferencia persistida.
- **Accesibilidad:** objetivo WCAG 2.1 AA (foco visible, roles ARIA, contraste, navegación por teclado).
- **Responsive:** mobile-first; la consola de agente prioriza desktop.
- **Estados estándar** en todo componente de datos: cargando, vacío, error, sin permisos.
- **i18n-ready:** textos de UI en español; estructura preparada para localización.

### Catálogo de componentes

Implementados (al cerrar la Fase A de funcionarios):

- **Formularios:** `MimoTextField` (genérico, soporta `@bind-Value`), `MimoTextArea`, `MimoSelect`,
  `MimoCheckbox`, `MimoFormGroup`.
- **Datos:** `MimoTable` (con estados de carga/vacío) + `MimoTr`/`MimoTh`/`MimoTd`, `MimoBadge`,
  `MimoCard`.
- **Feedback:** `MimoAlert`, `MimoModal`, `MimoSpinner`.
- **Acciones / layout:** `MimoButton` (submit/loading/disabled/full-width), `MimoActions`,
  `MimoPageHeader`, `MimoHeading`, `MimoText`, `MimoShell`, `MimoNavLink`, `MimoAuthScreen`.
- **Enums propios** para no filtrar tipos de Flowbite a las apps: `MimoButtonVariant`,
  `MimoAlertVariant`, `MimoBadgeVariant`, `MimoModalSize`.

Pendientes del catálogo (siguientes fases): `MimoDropdown`, `MimoTabs`,
`MimoSidebar`/`MimoBreadcrumb`, `MimoToast`, `MimoAvatar`, `MimoEmptyState`, paginación
server-side en `MimoTable`.

## 6. Consumo de APIs

- **Generación:** [Kiota](https://learn.microsoft.com/openapi/kiota/) genera clientes tipados
  desde el OpenAPI de `Mimo.Api` y `Mimo.Admin.Api`. Se regenera ante cambios de contrato
  (script en `scripts/`). Alternativa evaluada: NSwag.
- **Auth (JWT):** login contra `/auth/login`; token en `sessionStorage`. Un `DelegatingHandler`
  añade `Authorization: Bearer` y, en apps de tenant, el header `X-Tenant-Slug`. Sin refresh
  token aún → ante 401/expiración se re-loguea (mejora futura).
- **Tenant:** las apps de tenant (`Tenant.Web`, `Agent.Web`) envían `X-Tenant-Slug`; el
  backend exige que coincida con el del token (ADR 0008). `Admin.Web` no usa tenant.
- **Tiempo real:** `Microsoft.AspNetCore.SignalR.Client` para la consola de agente
  (`/hubs/tickets`, `/hubs/chat`); el token viaja por query string (ya soportado).

## 6.bis. Integración de Flowbite Blazor (confirmada)

Hallazgos al validar el stack contra el sitio oficial y la plantilla `Flowbite.Blazor.Templates`:

- **No hay un paquete `Flowbite.Blazor`**; la librería de componentes es el paquete NuGet
  **`Flowbite`** (+ `Flowbite.ExtendedIcons`), en **prerelease** (por eso no aparecía en la
  búsqueda por defecto). Verificado: `Flowbite 0.2.6-beta` **restaura y compila en net10**
  dentro de `Mimo.Ui` (el paquete es net8/beta pero es consumible desde net10).
- **`Mimo.Ui`** referencia `Flowbite` y expone sus componentes envueltos en los nuestros
  (`MimoButton`, etc.); las apps NO referencian `Flowbite` directamente.
- **Tailwind v3 standalone** (no v4, no npm): se versiona el binario `tailwindcss(.exe)` en
  `tools/` y un target MSBuild lo ejecuta antes de Build: `tailwindcss -i wwwroot/css/app.css
  -o wwwroot/css/app.min.css`. `tailwind.config.js` con `darkMode:'class'`, color `primary` y
  `content` que **debe incluir los `.razor` de `Mimo.Ui`** además de los de la app.
- **Assets de Flowbite** vía web assets del paquete: en `index.html` referenciar
  `_content/Flowbite/flowbite.min.css` y `_content/Flowbite/flowbite.js`, más el JS de
  posicionamiento `@floating-ui/dom` (CDN) para dropdown/tooltip/popover.
- **`Program.cs`**: `builder.Services.AddFlowbite();`. **`_Imports.razor`** (en `Mimo.Ui`):
  `@using Flowbite.Components`, `Flowbite.Services`, etc.

## 7. Pipeline de Tailwind

- **Todo el pipeline vive en `Mimo.Ui`** (config, binario, CSS de entrada y target de MSBuild):
  cambiar de framework de UI = tocar solo este proyecto. Las apps no tienen `tailwind.config.js`
  ni generan CSS.
- `Mimo.Ui/tailwind.config.js` escanea **solo `./**/*.razor` de `Mimo.Ui`** (ahí viven todas las
  clases; las apps no usan clases sueltas, regla de oro). El binario standalone está en
  `Mimo.Ui/tools/tailwindcss.exe` (gitignorado; ver `docs/pendings`).
- El target `BuildTailwindCss` compila `Mimo.Ui/Styles/mimo.css` → `Mimo.Ui/wwwroot/css/mimo.min.css`,
  que se sirve a las apps como **activo estático del RCL**: `_content/Mimo.Ui/css/mimo.min.css`.
- Cada app referencia en su `index.html`: `_content/Flowbite/flowbite.min.css`,
  `_content/Mimo.Ui/css/mimo.min.css` y un `css/app.css` propio solo con estilos del host Blazor
  (validación, UI de error, progreso de carga). El JS de Flowbite lo incluye la app vía
  `_content/Flowbite/flowbite.js`.

## 8. Roadmap por fases

- **Fase A — Cimientos:** crear `Mimo.Ui` (tokens + pipeline Tailwind/Flowbite + ~8 componentes
  base), `Mimo.ApiClient` (Kiota) y `Mimo.App` con login (JWT + tenant) + 1–2 pantallas CRUD de
  admin de tenant (funcionarios, roles) usando solo `Mimo.Ui`.
  - **Login: hecho.** `Mimo.App` autentica contra `/auth/login` (tenant por `X-Tenant-Slug`),
    persiste el JWT en `sessionStorage` y lo inyecta vía `AuthHeaderHandler`; rutas protegidas con
    `[Authorize]` y `AuthorizeRouteView` (detalle en [ADR 0012](../adr/0012-autenticacion-en-frontend-wasm-con-jwt-en-sessionstorage.md)).
    Componentes nuevos en `Mimo.Ui`: `MimoTextField`, `MimoCard`, `MimoAlert`, `MimoShell`
    (+ `MimoButton` ampliado con submit/loading/disabled).
  - **CRUD de funcionarios: hecho.** Página `/funcionarios` (listar, crear, editar, desactivar)
    con tabla + modal + asignación de roles, sobre `AgentsService`/`RolesService` (sin HTTP
    directo en la página). Sumó al catálogo: `MimoTable`/`MimoTr`/`MimoTh`/`MimoTd`, `MimoBadge`,
    `MimoCheckbox`, `MimoModal`, `MimoSpinner`, `MimoFormGroup`, `MimoActions`, `MimoPageHeader`,
    `MimoNavLink`.
  - **CRUD de roles: hecho.** Página `/roles` (listar, crear, editar, desactivar) sobre
    `RolesService`, reutilizando los componentes de la página de funcionarios.
  - **Fase A completa** (login + admin de tenant: funcionarios y roles).
  - **Datos de prueba:** ambas APIs siembran datos de demo en Development (credenciales conocidas);
    ver [docs/dev-seed.md](../dev-seed.md). Para entrar: tenant `acme`, `tenantadmin@acme.local` / `Mimo123$`.
- **Fase B — Admin de tenant completo** (en `Mimo.App`): documentos (con estados), configuración.
  - **Conocimiento (documentos): hecho.** Página `/conocimiento` (listar, crear, editar, reindexar,
    desactivar) sobre `DocumentsService`; visibilidad pública/privada, etiquetas, rol relacionado,
    estado de indexado (embedding). Sumó `MimoTextArea` y `MimoSelect` al catálogo.
  - **Pendiente de Fase B:** configuración del tenant; CRUD de categorías de documentos (hoy no hay
    endpoint de categorías, ver `docs/pendings`).
- **Fase C — Consola de agente (Callbell)** (en `Mimo.App`): inbox omnicanal en vivo (SignalR),
  conversación, cola, transferencias, chat interno.
- **Fase D — `Mimo.Admin.App` (SuperAdmin):** tenants, planes, conectores de IA, uso.
- **Fase E — WebChat embebible** (ciudadano).

## 9. Directrices de código (frontend)

- Comentarios/documentación en español; nombres de tipos/métodos/parámetros en inglés
  (igual que `CONTRIBUTING.md`).
- Componentes de `Mimo.Ui` con API tipada (parámetros explícitos), documentados y con valores
  por defecto sensatos.
- Sin llamadas HTTP directas en componentes de página: usar servicios sobre `Mimo.ApiClient`.
- Cada pantalla maneja los estados estándar (cargando/vacío/error).
- Reutilizar la paginación del backend (`PagedResult<T>`) en `MimoTable`.
