# 0011. Arquitectura de frontend: Blazor WASM aislado con biblioteca de UI y cliente generado

- Estado: Aceptado
- Fecha: 2026-06-16
- Decisores: evill

## Contexto

MIMO necesita frontends (admin de plataforma, admin de tenant, consola de agente, WebChat) con
diseño moderno tipo Callbell. El usuario fijó tres requisitos: stack **Tailwind + Flowbite
Blazor**, **componentización total sin hardcode** en las apps (cambiar la capa visual debe tocar
un solo proyecto) y un **proyecto de interfaces totalmente aislado** del backend. El detalle del
plan vive en [docs/frontend/README.md](../frontend/README.md).

## Decisión

- **Blazor WebAssembly standalone** para todas las apps: UI 100% en el cliente que consume las
  APIs por HTTP. Encaja con el aislamiento (se despliega como estáticos; sin estado ni
  acoplamiento a un host del backend).
- **Biblioteca de UI `Mimo.Ui`** (Razor Class Library) como único sistema de diseño y único
  proyecto que referencia Flowbite Blazor + Tailwind. Las apps **solo** componen componentes de
  `Mimo.Ui`; no usan HTML crudo, clases Tailwind sueltas ni Flowbite directo (regla de oro).
- **Cliente de API generado desde OpenAPI** (Kiota) en `Mimo.ApiClient`, regenerable ante
  cambios de contrato; nada de clientes escritos a mano.
- **Primera app: `Mimo.Tenant.Web`** (CRUD) para cimentar `Mimo.Ui`, auth JWT, manejo de tenant
  y patrones antes de la consola de agente.

## Consecuencias

### Positivas

- Aislamiento real: cambiar look & feel o la librería de componentes = tocar solo `Mimo.Ui`.
- Apps desplegables como estáticos (CDN/IIS); backend desacoplado.
- Cliente tipado sin drift respecto al contrato de las APIs.

### Negativas / Costos

- WASM: descarga inicial mayor y sin renderizado en servidor (SEO no relevante aquí).
- La regla "cero hardcode" exige disciplina y revisión; conviene apoyarla con linters/revisión.
- Aislar la librería de componentes NO inmuniza ante un cambio de framework (Blazor→JS), que
  reescribiría `Mimo.Ui`; el beneficio es a nivel de librería de componentes y consistencia.
- Sin refresh token (heredado de ADR 0002): expiración → re-login.

## Alternativas consideradas

- **Blazor Server / Auto:** menos aislado (host .NET vivo por app) o más complejo; descartado
  frente al requisito de UI totalmente aislada.
- **Clientes de API a mano:** más mantenimiento y riesgo de desincronización; descartado frente
  a la generación desde OpenAPI.
- **Usar Flowbite directamente en las apps:** viola la componentización; se envuelve en `Mimo.Ui`.
