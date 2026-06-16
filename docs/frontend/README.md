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
| Primera app | **`Mimo.Tenant.Web`** (CRUD) para cimentar `Mimo.Ui`, auth y patrones |

## 3. Arquitectura de proyectos

```text
src/
├── Mimo.Ui/            # Razor Class Library: SISTEMA DE DISEÑO.
│                       # Único proyecto que referencia Flowbite Blazor + Tailwind.
│                       # Expone componentes propios: <MimoButton>, <MimoTable>, etc.
├── Mimo.ApiClient/     # Cliente(s) tipados generados de OpenAPI (Mimo.Api y Mimo.Admin.Api)
│                       # + handlers de auth/tenant. Sin lógica de UI.
├── Mimo.Tenant.Web/    # WASM — consola del TenantAdmin (consume Mimo.Api)
├── Mimo.Agent.Web/     # WASM — consola del agente, estilo Callbell (consume Mimo.Api)
├── Mimo.Admin.Web/     # WASM — panel del SuperAdmin (consume Mimo.Admin.Api)
└── Mimo.WebChat/       # WASM — widget embebible del ciudadano (consume Mimo.Api)
```

Regla de dependencias: `App.Web → Mimo.Ui` y `App.Web → Mimo.ApiClient`. **Ninguna app
referencia Flowbite ni Tailwind directamente.**

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

### Catálogo inicial de componentes
Formularios (`MimoInput`, `MimoSelect`, `MimoCheckbox`, `MimoForm`, validación), navegación
(`MimoSidebar`, `MimoNavbar`, `MimoTabs`, `MimoBreadcrumb`), datos (`MimoTable`/DataGrid con
paginación server-side, `MimoBadge`, `MimoAvatar`, `MimoCard`), feedback (`MimoModal`,
`MimoToast`, `MimoSpinner`, `MimoEmptyState`), acciones (`MimoButton`, `MimoDropdown`).

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

## 7. Pipeline de Tailwind

- Tailwind CLI (binario standalone o vía npm) con el plugin de Flowbite.
- `content` debe escanear **`Mimo.Ui/**/*.razor`** y los `*.razor` de cada app + los assets de
  Flowbite Blazor, para no purgar clases usadas en la RCL.
- CSS compilado servido desde `Mimo.Ui` (wwwroot) y referenciado por las apps. JS de Flowbite
  (componentes interactivos) incluido por `Mimo.Ui`.

## 8. Roadmap por fases

- **Fase A — Cimientos:** crear `Mimo.Ui` (tokens + pipeline Tailwind/Flowbite + ~8 componentes
  base), `Mimo.ApiClient` (Kiota) y `Mimo.Tenant.Web` con login (JWT + tenant) + 1–2 pantallas
  CRUD (funcionarios, roles) usando solo `Mimo.Ui`.
- **Fase B — Tenant admin completo:** documentos (con estados), configuración del tenant.
- **Fase C — Consola de agente (Callbell):** inbox omnicanal en vivo (SignalR), conversación,
  cola, transferencias, chat interno.
- **Fase D — WebChat embebible** (ciudadano).
- **Fase E — SuperAdmin:** tenants, planes, conectores de IA, estadísticas de uso.

## 9. Directrices de código (frontend)

- Comentarios/documentación en español; nombres de tipos/métodos/parámetros en inglés
  (igual que `CONTRIBUTING.md`).
- Componentes de `Mimo.Ui` con API tipada (parámetros explícitos), documentados y con valores
  por defecto sensatos.
- Sin llamadas HTTP directas en componentes de página: usar servicios sobre `Mimo.ApiClient`.
- Cada pantalla maneja los estados estándar (cargando/vacío/error).
- Reutilizar la paginación del backend (`PagedResult<T>`) en `MimoTable`.
