# 0018. WebChat embebible: widget vanilla JS servido por la API

- Estado: Aceptado
- Fecha: 2026-06-19
- Decisores: evill

## Contexto

El WebChat es el canal propio por el que un ciudadano/cliente conversa con el bot y, tras escalar,
con un funcionario. La infraestructura ya existía: endpoints anónimos de conversación
(`/conversations`, tenant por `X-Tenant-Slug`), `ChatHub` (SignalR, anónimo) y `WebChatConnector`.
Faltaba la pieza de **distribución**: un widget que el cliente incruste en **su propio sitio**
(dominio de terceros) con un snippet.

Las apps internas (`Mimo.App`, `Mimo.Admin.App`) son Blazor WebAssembly: pesan MB y arrancan un
runtime; **no** sirven para incrustarse en sitios ajenos, donde el peso y el aislamiento importan.

## Decisión

- **Widget en JavaScript vanilla, sin dependencias ni paso de build** (`wwwroot/webchat/widget.js`,
  ~8 KB). Se incrusta con `<script src="…/webchat/widget.js" data-tenant="SLUG"></script>`; opcional
  `data-api` para apuntar a otra base. Inyecta sus estilos con prefijo `mimo-wc-` para no chocar con
  el sitio anfitrión.
- **Servido como archivo estático por `Mimo.Api`** (`UseStaticFiles`). Simple para empezar; podrá
  moverse a un CDN sin cambiar el contrato.
- **Superficie pública con CORS abierto** (política `webchat`, cualquier origen) sobre lo que el
  widget consume: `GET /webchat/config`, `/conversations*` y `/conversations/survey`. Es seguro
  porque es **anónimo, sin cookies**, y el tenant se resuelve por el slug público del snippet.
- **`GET /webchat/config`**: config pública mínima (nombre, mensaje de bienvenida, color/logo desde
  `ChannelCustomization`). No expone secretos ni configuración interna.
- **Identidad del usuario**: `externalUserId` aleatorio persistido en `localStorage` por tenant, para
  reanudar la conversación del mismo navegador.
- **Alcance por cortes.** Corte 1 (este): conversación con el **bot por REST** (config + start +
  mensajes). Corte 2: **funcionario en vivo** vía SignalR (`ChatHub`), posición de cola y encuesta
  embebida.

## Consecuencias

### Positivas

- Cero dependencias y peso mínimo: se incrusta en cualquier sitio sin tooling. **Verificado:**
  `widget.js` se sirve como `text/javascript`; `/webchat/config` y `/conversations` responden con
  `Access-Control-Allow-Origin: *` desde un `Origin` de prueba; el inicio de conversación funciona.
- Reutiliza los endpoints anónimos existentes; no duplica lógica de conversación.

### Negativas / Costos

- **CORS abierto a cualquier origen** en la superficie del widget: necesario (dominios arbitrarios),
  pero cualquier sitio puede abrir conversaciones para un tenant → conviene **rate limiting / captcha**
  antes de exponerlo público (pendiente).
- El widget se sirve desde la API (acopla un asset estático al proceso de API); mover a CDN queda
  como mejora.
- La respuesta del bot depende del LLM: **verificado que sin credenciales de LLM el orquestador
  devuelve 500**; el widget degrada con un aviso, pero la resiliencia del orquestador
  (fallback/escalar cuando el LLM no está) queda pendiente.
- Corte 1 no cubre el funcionario en vivo (SignalR) ni la encuesta en el widget (corte 2).
