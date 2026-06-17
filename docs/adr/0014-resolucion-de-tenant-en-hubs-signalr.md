# 0014. Resolución de tenant en hubs SignalR

- Estado: Aceptado
- Fecha: 2026-06-17
- Decisores: evill

## Contexto

El aislamiento multi-tenant fija el `search_path` por conexión vía `ITenantSchemaProvider`
(scoped) + un interceptor de conexión ([ADR 0009](0009-search-path-por-interceptor-de-conexion.md)).
El `TenantResolutionMiddleware` resuelve el tenant y fija el esquema, pero **solo corre en el
handshake HTTP** de SignalR, no por cada invocación de método del hub; y el provider es **scoped
por invocación**. Resultado: las operaciones de BD dentro de los hubs (persistir el mensaje del
agente en `SendMessageToCitizen`, resolver tickets) no apuntaban al esquema del tenant —un bug
multi-tenant latente que aflora al construir la consola de agente en vivo (Fase C).

## Decisión

- Un **`IHubFilter` (`TenantHubFilter`)** fija `ITenantSchemaProvider.Schema` en cada invocación de
  método y al conectar, leyendo el esquema que el middleware ya resolvió y guardó en el
  `HttpContext` del handshake (`Context.GetHttpContext().Items`), que vive durante toda la conexión.
- El middleware ahora también guarda el **nombre del esquema** en `HttpContext.Items`
  (`GetTenantSchema()`), para no duplicar la lógica slug→schema en el filtro.
- Se registra global en `AddSignalR(o => o.AddFilter<TenantHubFilter>())`. Funciona para hubs
  autenticados (`TicketHub`, tenant por el token) y anónimos (`ChatHub`, tenant por `X-Tenant-Slug`
  del handshake).

## Consecuencias

### Positivas

- Las escrituras de los hubs apuntan al esquema correcto. **Verificado E2E:** un mensaje de agente
  enviado por `SendMessageToCitizen` persiste en `tenant_<slug>.messages` y **no** se filtra a otro
  tenant.
- Reutiliza la resolución del middleware; una conexión queda atada al tenant de su handshake
  (correcto: una conexión = un tenant).

### Negativas / Costos

- Acopla los hubs al detalle de que el middleware publique el esquema en `HttpContext.Items`.
- CORS para SignalR cross-origin en producción (orígenes explícitos) sigue pendiente
  (`docs/pendings` #5); en Development el CORS permisivo lo cubre.
