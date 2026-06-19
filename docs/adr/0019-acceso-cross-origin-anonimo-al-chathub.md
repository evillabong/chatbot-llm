# 0019. Acceso cross-origin anónimo al ChatHub (WebChat en vivo)

- Estado: Aceptado
- Fecha: 2026-06-19
- Decisores: evill

## Contexto

El corte 2 del WebChat embebible ([ADR 0018](0018-webchat-embebible-widget-vanilla-js.md)) necesita
que el widget —incrustado en el sitio del cliente (origen arbitrario), anónimo— reciba en vivo los
mensajes del funcionario y los cambios de estado vía `ChatHub` (SignalR). Dos obstáculos:

1. **CORS:** el *negotiate* de SignalR es una petición HTTP cross-origin; sin CORS el navegador la
   bloquea.
2. **Resolución de tenant en el handshake WebSocket:** el tenant de las conexiones anónimas se resolvía
   por la cabecera `X-Tenant-Slug` ([ADR 0014](0014-resolucion-de-tenant-en-hubs-signalr.md)), pero
   **el navegador no puede fijar cabeceras en un WebSocket**. La cabecera podría ir en el *negotiate*
   (HTTP) pero no en el upgrade WS, así que el `TenantHubFilter` no tendría el esquema.

## Decisión

- **CORS abierto en `/hubs/chat`** mediante la política pública `webchat` (`RequireCors`), la misma de
  la superficie del widget. El cliente SignalR conecta con **`withCredentials: false`** (sin cookies),
  compatible con `AllowAnyOrigin`.
- **Tenant por query string en rutas `/hubs`:** `TenantResolutionMiddleware` acepta `?tenant_slug=…`
  como *client slug* cuando la ruta empieza por `/hubs` y no hay cabecera. Es el análogo a cómo el JWT
  viaja por `?access_token` en los hubs. El widget conecta a `/hubs/chat?tenant_slug=SLUG`; tanto el
  *negotiate* como el upgrade WS llevan el query, así que el middleware resuelve el tenant y fija el
  esquema en ambos (el `TenantHubFilter` lo lee del handshake, ADR 0014).

## Consecuencias

### Positivas

- El widget recibe en vivo `MessageReceived` (bot/funcionario), `AgentJoined` y `StatusChanged`.
  **Verificado E2E** con un cliente SignalR real: conexión WebSocket cross-origin, tenant por
  `?tenant_slug`, `JoinConversation` y `SendMessage` → eco del ciudadano + respuesta del bot.
- Reutiliza la resolución de tenant existente; no duplica lógica.

### Negativas / Costos

- `?tenant_slug` en la URL es un identificador **público** (no secreto): correcto para esta superficie
  anónima, pero refuerza la necesidad de **rate limiting** antes de exponerla (pendings #32).
- Sigue faltando un evento de **posición de cola** por ciudadano; el widget informa la escalada por el
  mensaje del bot y muestra `AgentJoined` al llegar el funcionario, pero no la posición exacta.
