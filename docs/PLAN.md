Perfecto. Reescribo el documento completo con esa convención: documentación y comentarios en español, código (nombres de clases, métodos, objetos) en inglés.

---

# Documento de Arquitectura - CRM Omnicanal Multi-Tenant

## 1. Visión General

CRM omnicanal multi-tenant para atención ciudadana, con bot de IA, escalación a atención humana en vivo, transferencia entre departamentos, y gestión documental con búsqueda semántica.

---

## 2. Stack Tecnológico

| Capa | Tecnología |
|------|------------|
| API tenant-facing | .NET 10 - Minimal APIs (`Mimo.Api`) |
| API administración | .NET 10 - Minimal APIs (`Mimo.Admin.Api`, host separado) |
| Frontend Administrativo | Blazor WebAssembly |
| Comunicación en tiempo real | SignalR |
| Base de datos principal | PostgreSQL + pgvector |
| Caché efímera | `IMemoryCache` en proceso (sin Redis, ver ADR 0001) |
| Modelo de lenguaje | Conectores de IA configurables en BD, vía gateway de plataforma (DeepSeek inicial, ver ADR 0004 y 0005) |
| MCP Server | .NET (integrado en el backend) |
| Canales externos | Facebook Messenger, WhatsApp, Telegram, Instagram DM |
| Plugin web | WebChat embebible (Blazor WASM standalone) |

---

## 3. Actores del Sistema

### 3.1 SuperAdmin (Super Administrador)
- Dueño de la plataforma SaaS
- Crea y deshabilita tenants (entidades)
- Define planes y límites
- Monitoreo global
- No accede a datos internos de las entidades

### 3.2 TenantAdmin (Administrador de Entidad)
- Configura parámetros de comportamiento de su entidad
- Gestiona roles y funcionarios
- Carga y clasifica documentos de conocimiento
- Configura canales de comunicación
- Ve reportes y métricas
- Personaliza mensajes automáticos
- Ve todas las sesiones de su entidad

### 3.3 Agent (Funcionario)
- Ve cola de sesiones de su rol
- Atiende múltiples sesiones simultáneas
- Transfiere entre roles y funcionarios
- Chatea internamente con colegas
- Recibe notificaciones por correo
- Tiene alias visible al ciudadano

### 3.4 Customer (Ciudadano)
- Inicia conversación por cualquier canal
- Se autentica para acceso a información privada
- Solicita atención humana
- Espera en línea hasta ser atendido
- Ve alias del funcionario que lo atiende
- Califica atención (si está habilitado)

---

## 4. Premisas Fundamentales

1. **Visibilidad binaria de documentos:** Público (todos) y Privado (solo autenticados)
2. **La IA no decide qué información mostrar:** El sistema filtra por sesión antes de dar contexto al LLM
3. **Clasificación documental por roles/departamentos:** Cada documento se vincula a un rol
4. **Atención sincrónica:** El ciudadano espera en línea hasta ser atendido
5. **Transferencia entre roles y entre funcionarios:** Movilidad total de sesiones
6. **Todo parametrizable por entidad:** Nada está hardcodeado
7. **Arquitectura multi-tenant con aislamiento de datos**

---

## 5. Estructura del Proyecto

```
MIMO/
├── src/
│   ├── Mimo.Core/                        # Modelos, interfaces, DTOs, enums
│   │   ├── Models/
│   │   │   ├── Tenant.cs                # Entidad/empresa del sistema
│   │   │   ├── Document.cs              # Documento de conocimiento
│   │   │   ├── Conversation.cs          # Conversación activa
│   │   │   ├── Message.cs               # Mensaje individual
│   │   │   ├── Ticket.cs                # Ticket/sesión de atención
│   │   │   ├── Agent.cs                 # Funcionario que atiende
│   │   │   ├── Role.cs                  # Rol/departamento
│   │   │   ├── TransferRecord.cs        # Registro de transferencia
│   │   │   ├── SatisfactionSurvey.cs    # Encuesta de satisfacción
│   │   │   └── TenantConfiguration.cs   # Parámetros configurables
│   │   ├── Enums/
│   │   │   ├── TicketStatus.cs          # Estados del ticket
│   │   │   ├── TicketPriority.cs        # Prioridades del ticket
│   │   │   ├── ConversationStatus.cs    # Estados de conversación
│   │   │   ├── VisibilityLevel.cs       # Niveles de visibilidad
│   │   │   ├── AssignmentMode.cs        # Modos de asignación
│   │   │   └── TimeoutAction.cs         # Acciones ante timeout
│   │   └── Interfaces/
│   │       ├── ITicketService.cs        # Servicio de tickets
│   │       ├── ITicketQueueService.cs   # Servicio de colas
│   │       ├── IAgentAssignmentService.cs   # Asignación de funcionarios
│   │       ├── IConversationOrchestrator.cs # Orquestador de conversaciones
│   │       ├── IVectorSearchService.cs  # Búsqueda vectorial en pgvector
│   │       ├── IChannelConnector.cs     # Conector de canales externos
│   │       ├── IInternalChatService.cs  # Chat interno entre funcionarios
│   │       ├── INotificationService.cs  # Notificaciones por correo
│   │       └── IMcpToolProvider.cs      # Proveedor de herramientas MCP
│   │
│   ├── Mimo.Infrastructure/              # Implementaciones concretas
│   │   ├── Data/
│   │   │   ├── CrmDbContext.cs          # Contexto de base de datos
│   │   │   └── Repositories/
│   │   │       ├── TenantRepository.cs
│   │   │       ├── DocumentRepository.cs
│   │   │       ├── ConversationRepository.cs
│   │   │       ├── TicketRepository.cs
│   │   │       └── AgentRepository.cs
│   │   ├── AI/
│   │   │   ├── VectorSearchService.cs       # Búsqueda semántica
│   │   │   ├── IntentClassifierService.cs   # Clasificador de intenciones
│   │   │   └── DeepSeekClient.cs            # Cliente para DeepSeek API
│   │   ├── Ticketing/
│   │   │   ├── TicketService.cs             # Lógica de tickets
│   │   │   ├── TicketQueueService.cs        # Gestión de colas
│   │   │   └── AgentAssignmentService.cs    # Asignación de funcionarios
│   │   ├── Orchestration/
│   │   │   └── ConversationOrchestrator.cs  # Orquestador principal
│   │   ├── InternalChat/
│   │   │   └── InternalChatService.cs       # Chat entre funcionarios
│   │   ├── Notifications/
│   │   │   └── EmailNotificationService.cs  # Notificaciones por correo
│   │   ├── MCP/
│   │   │   └── McpToolProvider.cs           # Herramientas MCP para el LLM
│   │   └── Connectors/
│   │       ├── MetaConnector.cs             # Facebook + Instagram
│   │       ├── TelegramConnector.cs         # Telegram
│   │       ├── WhatsAppConnector.cs         # WhatsApp Business
│   │       └── WebChatConnector.cs          # Plugin web propio
│   │
│   ├── Mimo.Api/                             # API tenant-facing (canales, chat, tickets, documentos)
│   │   ├── Program.cs                       # Punto de entrada
│   │   ├── Endpoints/
│   │   │   ├── WebhookEndpoints.cs          # Webhooks de canales externos
│   │   │   ├── AuthEndpoints.cs             # Autenticación de ciudadanos
│   │   │   ├── DocumentEndpoints.cs         # CRUD de documentos
│   │   │   ├── AgentEndpoints.cs            # Gestión de funcionarios
│   │   │   ├── RoleEndpoints.cs             # Gestión de roles
│   │   │   ├── TicketEndpoints.cs           # Gestión de tickets
│   │   │   ├── ConfigurationEndpoints.cs    # Parámetros configurables
│   │   │   ├── ReportEndpoints.cs           # Reportes y métricas
│   │   │   └── InternalChatEndpoints.cs     # Chat interno
│   │   ├── Hubs/
│   │   │   ├── ChatHub.cs                   # SignalR para chat con ciudadano
│   │   │   ├── TicketHub.cs                 # SignalR para panel de tickets
│   │   │   └── InternalChatHub.cs           # SignalR para chat interno
│   │   └── Middleware/
│   │       ├── TenantResolutionMiddleware.cs    # Resolución de tenant
│   │       └── AuthenticationMiddleware.cs      # Autenticación
│   │
│   ├── Mimo.Admin.Api/                       # API exclusiva de administración de plataforma
│   │   ├── Program.cs                       # Punto de entrada (puerto/host separado)
│   │   ├── Endpoints/
│   │   │   ├── TenantEndpoints.cs           # CRUD de tenants
│   │   │   ├── PlanEndpoints.cs             # Planes y límites
│   │   │   ├── BillingEndpoints.cs          # Facturación y suscripciones
│   │   │   └── MonitoringEndpoints.cs       # Monitoreo global de la plataforma
│   │   └── Middleware/
│   │       └── AdminAuthenticationMiddleware.cs  # Autenticación exclusiva de admin
│   │
│   ├── Mimo.Admin.Web/                  # Blazor WASM - Súper Admin
│   │   ├── Pages/
│   │   │   ├── Tenants/
│   │   │   │   ├── TenantList.razor         # Lista de tenants
│   │   │   │   ├── CreateTenant.razor       # Crear nuevo tenant
│   │   │   │   └── ConfigureTenant.razor    # Configurar límites y plan
│   │   │   ├── Billing/
│   │   │   │   ├── Plans.razor              # Definición de planes
│   │   │   │   └── Subscriptions.razor      # Suscripciones activas
│   │   │   └── Monitoring/
│   │   │       └── Dashboard.razor          # Monitoreo global
│   │   └── Services/
│   │       └── AdminApiClient.cs            # Cliente HTTP para Mimo.Admin.Api
│   │
│   ├── Mimo.Tenant.Web/                 # Blazor WASM - Admin de Entidad
│   │   ├── Pages/
│   │   │   ├── Dashboard/
│   │   │   │   └── Index.razor              # KPIs de la entidad
│   │   │   ├── Documents/
│   │   │   │   ├── DocumentList.razor       # Lista de documentos
│   │   │   │   ├── CreateDocument.razor     # Crear documento
│   │   │   │   └── Categories.razor         # Categorías documentales
│   │   │   ├── Agents/
│   │   │   │   ├── AgentList.razor          # Lista de funcionarios
│   │   │   │   ├── CreateAgent.razor        # Crear funcionario
│   │   │   │   └── RoleList.razor           # Gestión de roles
│   │   │   ├── Configuration/
│   │   │   │   ├── Parameters.razor         # Parámetros generales
│   │   │   │   ├── Schedule.razor           # Horarios de atención
│   │   │   │   ├── Channels.razor           # Configuración de canales
│   │   │   │   └── AutomaticMessages.razor  # Mensajes automáticos
│   │   │   ├── Monitoring/
│   │   │   │   ├── LiveSessions.razor       # Sesiones en vivo
│   │   │   │   └── History.razor            # Historial de sesiones
│   │   │   └── Reports/
│   │   │       ├── Satisfaction.razor       # Reportes de satisfacción
│   │   │       └── Performance.razor        # Rendimiento de funcionarios
│   │   └── Services/
│   │       └── TenantApiClient.cs           # Cliente HTTP para Mimo.Api
│   │
│   ├── Mimo.Agent.Web/                       # Blazor WASM - Panel Funcionario
│   │   ├── Pages/
│   │   │   ├── Panel/
│   │   │   │   └── Index.razor              # Panel principal
│   │   │   ├── Attention/
│   │   │   │   └── Session.razor            # Atención de sesión
│   │   │   └── History/
│   │   │       └── MySessions.razor         # Historial personal
│   │   ├── Components/
│   │   │   ├── TicketQueue.razor            # Cola de sesiones pendientes
│   │   │   ├── SessionChat.razor            # Chat con ciudadano
│   │   │   ├── TransferModal.razor          # Modal de transferencia
│   │   │   ├── InternalChat.razor           # Chat interno con colegas
│   │   │   └── AvailabilityIndicator.razor  # Indicador de disponibilidad
│   │   └── Services/
│   │       ├── TicketSignalRService.cs      # Conexión SignalR para tickets
│   │       ├── ChatSignalRService.cs        # Conexión SignalR para chat
│   │       └── InternalChatService.cs       # Servicio de chat interno
│   │
│   └── Mimo.WebChat/                         # Plugin WebChat embebible
│       ├── Components/
│       │   └── ChatWidget.razor             # Componente principal del chat
│       ├── Services/
│       │   └── ChatSignalRService.cs        # Conexión SignalR
│       └── wwwroot/
│           ├── chat-widget.js               # Script de inicialización
│           └── chat-widget.css              # Estilos del widget
│
└── docker-compose.yml                       # PostgreSQL + pgvector (sin Redis, ver ADR 0001)
```

---

## 6. Flujo de Vida de una Sesión

```
INICIO
  │
  ▼
[BotActive] Ciudadano interactúa con el bot
  │
  ├──► Solicita atención humana ──► [InQueue] Espera asignación
  │                                      │
  │                                      ├──► Funcionario toma ──► [Assigned]
  │                                      ├──► Auto-asignación ──► [Assigned]
  │                                      └──► Timeout ──► [Unattended] ──► FIN
  │
  └──► Funcionario interviene ──► [Intervention] ──► [InProgress]
                                         │
                                [Assigned] ──► [InProgress]
                                                   │
                                                   ├──► Resuelve ──► [Resolved]
                                                   ├──► Transfiere mismo rol ──► [InQueue]
                                                   ├──► Transfiere otro rol ──► [InQueue]
                                                   └──► Devuelve a cola ──► [InQueue]
                                                        │
                                                  [Resolved]
                                                        │
                                                   Encuesta habilitada?
                                                        │
                                                ┌───────┴───────┐
                                                ▼               ▼
                                           [Survey]        [Closed] ──► FIN
                                                │
                                        ┌───────┴───────┐
                                        ▼               ▼
                                   Calificación    Calificación
                                   ≥ umbral        < umbral
                                        │               │
                                        ▼               ▼
                                   [Closed]       [Reopened] ──► [InQueue]
                                        │
                                        ▼
                                       FIN
```

---

## 7. Parámetros Configurables por Entidad

### 7.1 Atención al Ciudadano (`CustomerAttention`)
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `MaxQueueWaitTimeMinutes` | int? | Tiempo máximo en cola. null = infinito |
| `QueueWaitMessage` | string | Texto que ve el ciudadano mientras espera |
| `TransferMessage` | string | Texto que ve el ciudadano al ser transferido |
| `InactivityTimeoutMinutes` | int? | Tiempo sin mensajes antes de cerrar. null = nunca |

### 7.2 Asignación de Sesiones (`SessionAssignment`)
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `AssignmentMode` | enum | `Manual` o `AutomaticBalanced` |
| `NotifyPendingByEmail` | bool | Enviar correo a funcionarios con sesiones pendientes |
| `NotificationFrequencyMinutes` | int | Cada cuánto se envía notificación |
| `MaxConcurrentSessionsPerAgent` | int | Límite de sesiones simultáneas por funcionario |

### 7.3 Transferencias (`Transfers`)
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `AllowPartialTransfer` | bool | Permite transferir solo parte de una conversación |
| `RequireCustomerConfirmation` | bool | Pedir confirmación al ciudadano antes de transferir |
| `EnableInternalChat` | bool | Habilitar chat entre funcionarios |
| `MaxTransfersPerSession` | int? | Límite de transferencias. null = sin límite |

### 7.4 Intervención de Funcionarios (`AgentIntervention`)
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `AllowIntervention` | bool | Permite a funcionarios unirse a conversaciones con bot |
| `CanViewBotConversations` | bool | Ver conversaciones activas con bot |
| `CanViewOtherRolesSessions` | bool | Ver sesiones de roles diferentes al propio |
| `InterventionSuggestionThreshold` | int | N° de mensajes sin resolver antes de sugerir intervención |

### 7.5 Encuesta de Satisfacción (`SatisfactionSurvey`)
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `SurveyEnabled` | bool | Mostrar encuesta al finalizar |
| `SurveyRequired` | bool | Obligar a calificar para cerrar |
| `AllowObservations` | bool | Permitir comentarios adicionales |
| `ReopenOnLowRating` | bool | Reabrir sesión si calificación es baja |
| `ReopenRatingThreshold` | int (1-5) | Nota mínima para no reabrir |

### 7.6 Sesiones No Atendidas (`UnattendedSessions`)
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `UnattendedTimeoutMinutes` | int? | Tiempo sin que nadie tome la sesión |
| `TimeoutAction` | enum | `MarkAsUnattended`, `TransferToAnotherRole`, `NotifyAdmin` |
| `TimeoutCustomerMessage` | string | Mensaje al ciudadano cuando hay timeout |
| `AutoRetry` | bool | Reintentar asignación automáticamente |

### 7.7 Horarios de Atención (`BusinessHours`)
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `BusinessHoursEnabled` | bool | Activar restricción por horario |
| `StartTime` | TimeOnly | Hora de inicio de atención |
| `EndTime` | TimeOnly | Hora de fin de atención |
| `WorkingDays` | int[] | Días de atención (1=Lunes, 7=Domingo) |
| `OutOfHoursMessage` | string | Mensaje cuando está fuera de horario |

### 7.8 Personalización de Canales (`ChannelCustomization`)
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| WebChat: `PrimaryColor` | string | Color principal del widget |
| WebChat: `LogoUrl` | string | URL del logo |
| WebChat: `WelcomeMessage` | string | Texto inicial del chat |
| Facebook: `PageId` | string | ID de la página de Facebook |
| WhatsApp: `PhoneNumber` | string | Número de WhatsApp Business |
| Telegram: `BotToken` | string | Token del bot de Telegram |

---

## 8. Modelo de Datos Conceptual

### 8.1 Tablas Principales

**Tenant** (Entidad/Empresa)
- `Id`
- `Name` - Nombre de la entidad
- `Slug` - Identificador único
- `IsActive` - Estado
- `Plan` - Plan asignado
- `Configuration` - JSON con todos los parámetros (Sección 7)
- `CreatedAt`

**Agent** (Funcionario)
- `Id`
- `TenantId`
- `Email`
- `FullName` - Nombre real
- `Alias` - Visible al ciudadano
- `IsActive`
- `MaxConcurrentSessions`
- `CreatedAt`

**Role** (Rol/Departamento)
- `Id`
- `TenantId`
- `Name` - Financiero, Legal, Atención General, etc.
- `Description`
- `PriorityLevel`
- `CanViewAllTickets` - Si es admin/supervisor
- `CreatedAt`

**AgentRole** (pivote Funcionario-Rol)
- `AgentId`
- `RoleId`

**Document** (Documento de conocimiento)
- `Id`
- `TenantId`
- `Title`
- `Content`
- `Visibility` - Public o Private
- `CategoryId`
- `RelatedRoleId` - Rol al que pertenece el conocimiento
- `Tags` - Array de palabras clave
- `PriorityLevel`
- `Embedding` - Vector para búsqueda semántica
- `IsActive`
- `CreatedBy` - Funcionario que lo creó
- `CreatedAt`

**DocumentCategory** (Categoría documental)
- `Id`
- `TenantId`
- `Name`
- `ParentCategoryId` - Para jerarquía

**Conversation** (Conversación)
- `Id`
- `TenantId`
- `ExternalUserId` - ID del ciudadano en el canal
- `ExternalUserName` - Nombre del ciudadano
- `Channel` - WebChat, Facebook, WhatsApp, Telegram
- `Status` - BotActive, InQueue, Assigned, InProgress, Resolved, Closed
- `IsAuthenticated` - Si el ciudadano se autenticó
- `CustomerEmail`
- `CustomerPhone`
- `CreatedAt`
- `LastMessageAt`
- `ResolvedAt`

**Message** (Mensaje)
- `Id`
- `ConversationId`
- `Role` - User, Assistant, System, Agent
- `SenderId` - Si es funcionario, su ID
- `Content`
- `Metadata` - JSON con intenciones, docs usados, etc.
- `CreatedAt`

**Ticket** (Sesión/Ticket de atención)
- `Id`
- `ConversationId`
- `AssignedRoleId` - Rol responsable
- `AssignedAgentId` - Funcionario asignado
- `Priority` - Low, Normal, High, Urgent
- `Status` - Open, Assigned, InProgress, WaitingClient, Resolved, Closed
- `InternalNotes`
- `EscalationReason`
- `CreatedAt`
- `AssignedAt`
- `FirstResponseAt`
- `ResolvedAt`

**TransferRecord** (Registro de transferencia)
- `Id`
- `TicketId`
- `FromRoleId`
- `FromAgentId`
- `ToRoleId`
- `ToAgentId`
- `TransferredBy` - Quién ejecutó la transferencia
- `Reason` - Motivo declarado
- `IsPartial` - Si fue transferencia parcial
- `ContextNote` - Nota de contexto
- `CreatedAt`

**SatisfactionSurvey** (Encuesta de satisfacción)
- `Id`
- `TicketId`
- `Rating` - 1 a 5
- `Observations` - Comentarios adicionales
- `RecordedAt`

**InternalChatMessage** (Chat interno entre funcionarios)
- `Id`
- `TenantId`
- `FromAgentId`
- `ToAgentId`
- `RelatedTicketId` - Opcional, si es sobre un ticket específico
- `Content`
- `CreatedAt`

---

## 9. Aislamiento Multi-Tenant

### Estrategia elegida: Esquemas separados por tenant

- Una base de datos PostgreSQL
- Cada tenant tiene su propio esquema (`tenant_{slug}`)
- El esquema `public` contiene la tabla de tenants y configuración global
- Al recibir una petición, el middleware resuelve el tenant y establece el `search_path`

### Resolución de tenant:
- **Webhook de redes sociales:** Mapeo por configuración del canal
- **WebChat:** El script de embed incluye el identificador del tenant
- **Dashboard administrativo:** El JWT del funcionario incluye el `TenantId`
- **API pública:** Header `X-Tenant-Id` o subdominio

---

## 10. Conectores de Canales Externos

### Interfaz común: `IChannelConnector`

Métodos:
- `NormalizeIncomingMessageAsync` - Convierte payload externo a modelo interno
- `SendMessageAsync` - Envía respuesta al canal
- `ValidateSignature` - Verifica autenticidad del webhook

### Conectores a implementar:
- **MetaConnector:** Facebook Messenger + Instagram DM (Meta Graph API)
- **TelegramConnector:** Telegram Bot API
- **WhatsAppConnector:** WhatsApp Business API (vía proveedor o Meta Cloud API)
- **WebChatConnector:** WebChat propio con SignalR

---

## 11. MCP Server - Herramientas para el LLM

El Model Context Protocol permite que el LLM invoque herramientas del sistema:

### Herramientas definidas:

| Herramienta | Descripción |
|-------------|-------------|
| `search_knowledge_base` | Busca documentos relevantes en pgvector |
| `get_document_by_id` | Obtiene un documento específico |
| `classify_intent` | Clasifica la intención del mensaje |
| `check_business_hours` | Verifica si está en horario de atención |
| `request_human_agent` | Crea un ticket para atención humana |
| `check_queue_status` | Consulta estado de la cola de atención |

---

## 11.bis. Gateway de IA multi-tenant

Todas las consultas al LLM de los tenants pasan por un **gateway de plataforma
in-process** dentro de `Mimo.Api` (`IAiGatewayService`). Decisión registrada en
[ADR 0005](adr/0005-gateway-de-ia-in-process-con-entitlements-y-cuotas-por-plan.md);
la configuración de proveedores en BD, en [ADR 0004](adr/0004-configuracion-de-conectores-de-ia-en-base-de-datos.md).

Responsabilidades del gateway, por cada operación (chat/embedding) y `tenantId`:

1. **Aislamiento de credenciales:** las claves del proveedor viven en `ai_connectors`
   (esquema `public`, JSONB) y nunca se exponen al tenant.
2. **Opacidad del modelo:** el tenant no sabe qué proveedor/modelo se usa; los usuarios
   solo hablan con `Mimo.Api`.
3. **Entitlement por plan:** el plan del tenant (`ai_plan_policies.allowed_providers`)
   determina qué modelos puede usar.
4. **Cuota por plan:** solicitudes y/o tokens mensuales (`monthly_request_quota`,
   `monthly_token_quota`; `0` = ilimitado).
5. **Medición de uso:** cada llamada registra consumo en `ai_usage_records` (append-only),
   base de las estadísticas del SuperAdmin.

Tablas (esquema `public`, gestionadas por el SuperAdmin):

| Tabla | Propósito |
|-------|-----------|
| `ai_connectors` | Proveedores de IA configurables; uno activo a la vez; config en JSONB |
| `ai_plan_policies` | Modelos permitidos y cuota por código de plan |
| `ai_usage_records` | Consumo por tenant (proveedor, modelo, operación, tokens) |

Administración (SuperAdmin) en `Mimo.Admin.Api`: `/plans` (catálogo), `/ai/connectors`
(conectores, activación exclusiva), `/ai/plan-policies` (modelos permitidos y cuotas por plan)
y `/ai/usage/summary` (estadísticas de uso por tenant).

---

## 12. Plan de Implementación por Fases

### Fase 1: Fundación Multi-Tenant + WebChat + Bot Básico
- Base de datos con esquemas por tenant
- CRUD de tenants (súper admin)
- WebChat embebible con identificador de tenant
- Bot con búsqueda en pgvector (público/privado)
- Autenticación de ciudadanos (magic link)
- Endpoints de documentos por tenant
- Carga de documentos con clasificación por rol

### Fase 2: Dashboard de Funcionario con Atención en Vivo
- Autenticación de funcionarios
- Panel de conversaciones en vivo (SignalR)
- Colas de espera por rol
- Intervención en conversaciones
- Transferencia entre roles y funcionarios
- Chat interno entre funcionarios
- Parámetros configurables por tenant

### Fase 3: Conectores de Redes Sociales
- Telegram (el más simple para probar webhooks)
- Facebook Messenger + Instagram DM
- WhatsApp Business
- Cada canal configurable por tenant

### Fase 4: Sistema de Satisfacción y Reportes
- Encuestas post-atención
- Dashboard de métricas
- Reportes exportables
- Reapertura automática por baja calificación

### Fase 5: Súper Admin Avanzado
- Facturación y planes
- Límites por tenant (usuarios, chats simultáneos, almacenamiento)
- Monitoreo centralizado
- Alertas y notificaciones

---

## 13. Convenciones de Código

### Idioma
- **Documentación y comentarios:** Español
- **Nombres de clases, métodos, propiedades, variables:** Inglés
- **Nombres de tablas y columnas:** Inglés

### Ejemplos:

```csharp
// Clase en inglés, comentario en español
/// <summary>
/// Representa una sesión de atención al ciudadano
/// </summary>
public class Ticket
{
    // Identificador único del ticket
    public Guid Id { get; set; }
    
    // Estado actual del ticket
    public TicketStatus Status { get; set; }
    
    // Funcionario asignado a la atención
    public Guid? AssignedAgentId { get; set; }
}
```

```sql
-- Tabla de tickets de atención
CREATE TABLE tickets (
    id UUID PRIMARY KEY,
    conversation_id UUID NOT NULL,
    status VARCHAR(30) NOT NULL,
    assigned_agent_id UUID
);

-- Comentario en español para documentar
-- Índice para búsqueda rápida por estado y funcionario
CREATE INDEX idx_tickets_status_agent ON tickets(status, assigned_agent_id);
```

---

## 14. Flujo de Transferencia entre Roles

```
Funcionario actual inicia transferencia
  │
  ├──► Define motivo (nota de contexto)
  │
  ├──► Selecciona destino
  │    ├── Mismo rol, otro funcionario
  │    ├── Mismo rol, devolver a cola
  │    ├── Otro rol, a cola
  │    └── Otro rol, funcionario específico
  │
  ├──► ¿Chat interno habilitado?
  │    └── Puede enviar mensaje interno al destinatario
  │
  ├──► ¿Confirmación del ciudadano requerida?
  │    ├── Sí: Se notifica al ciudadano y se espera confirmación
  │    └── No: Transferencia directa
  │
  ├──► Sistema registra TransferRecord
  │
  └──► Sesión aparece en nuevo destino
       │
       ├──► Si es cola: Disponible para todo el rol
       └──► Si es funcionario específico: Se le asigna directamente
```

---

## 15. Notificaciones por Correo a Funcionarios

### Condiciones de envío:
- `NotifyPendingByEmail = true`
- Hay sesiones en cola del rol del funcionario
- La sesión lleva al menos `NotificationFrequencyMinutes` en cola

### Contenido del correo:
- Cantidad de sesiones pendientes en su rol
- Tiempo promedio de espera de la cola
- Link directo al panel de atención
- Opción de "Tomar siguiente sesión"

### Frecuencia:
- Configurable (por defecto cada 5 minutos)
- No se envía si el funcionario está activo en el panel
- Se envía notificación de cese cuando la cola llega a cero

---

Este documento es la referencia canónica del proyecto. Cualquier decisión arquitectónica debe consultarse y actualizarse aquí.