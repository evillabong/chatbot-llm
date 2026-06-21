# 0028. Sincronización con CRM externo mediante proveedor simulado

- Estado: Aceptado
- Fecha: 2026-06-21
- Decisores: evill

## Contexto

El módulo de Ventas (#26, propuesta §4.10) contempla **sincronizar las oportunidades con un CRM
externo** (HubSpot, Salesforce, Pipedrive…). Integrar y **certificar** contra un CRM real es costoso y
**requiere credenciales/cuenta del proveedor**, que no están disponibles en este entorno: no se podría
verificar E2E.

La decisión (consultada con el responsable) fue **no bloquear** la capacidad: construir la
sincronización completa contra un **proveedor simulado** que sí funciona y se verifica, dejando un
**punto de extensión** limpio para enchufar proveedores reales después.

## Decisión

- **Abstracción `ICrmSyncProvider`** (`SyncOpportunityAsync` → `CrmSyncResult` con id externo y
  resultado) + nombre del proveedor para auditoría. El proveedor real (HubSpot, etc.) implementará esta
  misma interfaz sin tocar el núcleo.
- **`SimulatedCrmSyncProvider`** (por defecto): no llama a ningún sistema externo; asigna un id externo
  **determinista** por oportunidad (`SIM-…`), conserva el existente en re-sincronizaciones y simula
  éxito.
- **`ICrmSyncService`** orquesta: invoca al proveedor, actualiza el estado de sync en la oportunidad
  (`ExternalCrmId`, `LastSyncedAt`) y registra una **bitácora** (`CrmSyncLog`, esquema del tenant). No
  propaga excepciones del proveedor: devuelve un resultado fallido y lo deja en la bitácora.
- **Disparo manual y automático:**
  - Manual: `POST /opportunities/sync` (query `id`); bitácora en `GET /opportunities/sync-log`. UI:
    botón **«Sincronizar»** y columna de estado CRM en `/ventas`.
  - Automático: la creación y el **cambio de etapa** de una oportunidad emiten eventos de dominio
    (`opportunity.created`, `opportunity.stage_changed`) por `IDomainEventPublisher`; una nueva acción de
    automatización **`SyncCrm`** (#24) sincroniza la oportunidad del evento. Así el tenant decide,
    mediante una regla, cuándo sincronizar.

## Consecuencias

### Positivas

- La capacidad de sync **existe y se ejercita de extremo a extremo** sin depender de un proveedor real;
  solo la llamada final al vendor está simulada. **Verificado E2E:** sync manual fija `ExternalCrmId`/
  `LastSyncedAt` y registra bitácora; una regla `opportunity.created → SyncCrm` sincroniza
  **automáticamente** la oportunidad recién creada (confirmado en logs). Unit tests del proveedor
  simulado (id determinista, conserva el existente).
- Reutiliza el motor de eventos/automatización (ADR 0026) y el patrón de proveedores por tenant; el
  proveedor real se añade implementando `ICrmSyncProvider`.

### Negativas / Costos

- **No hay sincronización real** todavía: el proveedor real (con endpoint, OAuth/API key cifrada,
  mapeo de campos, rate limits y anti-SSRF como en `ChatbotApiCaller`) es un corte posterior, gatillado
  cuando haya credenciales.
- **Sin registro de proveedor por tenant** aún: hoy el proveedor activo es el simulado por
  configuración de DI; elegir/credenciales por organización se añadirá con el proveedor real.
- La sincronización es **saliente** (MIMO → CRM); la entrada (CRM → MIMO) no está contemplada en este
  corte.

## Alternativas consideradas

- **Aplazar la capacidad por completo** hasta tener un CRM real: descartado; dejaba el módulo de ventas
  a medias y sin la orquestación (estado, bitácora, disparos) que sí aporta valor y es reutilizable.
- **Llamar directamente a una API de CRM ahora:** inviable sin credenciales y no verificable; además
  acoplaría el núcleo a un proveedor concreto en lugar de a la abstracción.
