# Visión y diseño — Mejora continua del asistente (aprende con el uso)

- Estado: **Borrador / visión** (no implementado; no es una decisión cerrada)
- Fecha: 2026-06-17
- Audiencia: interna (técnica/producto)
- Relacionado: [ADR 0005 (gateway de IA)](adr/0005-gateway-de-ia-in-process-con-entitlements-y-cuotas-por-plan.md),
  [ADR 0008 (binding de tenant)](adr/0008-binding-de-tenant-al-principal-autenticado.md),
  [ADR 0009 (search_path)](adr/0009-search-path-por-interceptor-de-conexion.md),
  [ADR 0010 (cifrado de API keys)](adr/0010-cifrado-de-api-keys-de-conectores-de-ia.md),
  pendiente #22.

> Documento técnico interno. La versión orientada a venta vive en
> [propuesta-comercial.md](propuesta-comercial.md) (sección "Evolución").

## 1. Objetivo

Que el asistente **mejore con la operación diaria** en dos dimensiones:

1. **Respuestas más precisas** con el paso de las atenciones.
2. **Derivación IA↔funcionario más inteligente**: entender cuándo un caso puede resolverse solo con
   IA y cuándo conviene intervención humana.

**No es core.** Es una capacidad **adicional, premium y gobernada por plan**.

## 2. Principios de diseño

- **No reentrenar el LLM por defecto.** "Aprender con el uso" se implementa con **bucles alrededor
  del LLM** (recuperación + curación + clasificación), no con fine-tuning continuo. El fine-tuning
  es la última fase y es opcional.
- **Humano en el bucle (HITL).** Las mejoras se **proponen** y un `TenantAdmin` las **aprueba**;
  nada cambia solo.
- **Aislamiento por organización.** Todo el aprendizaje deriva de los datos del **propio tenant**;
  jamás se comparte ni se entrena entre tenants (coherente con ADR 0008/0009).
- **Gobernado por plan.** Intensidad y disponibilidad se habilitan vía `AiPlanPolicy`; el consumo
  cuenta contra cuotas (`AiUsageRecord`).
- **Medible.** Cada fase define métricas y se compara contra una línea base.
- **Aditivo.** Se construye sobre lo existente (gateway in-process, `pgvector`, orquestador) sin
  reestructurar el core.

## 3. Reencuadre técnico: "entrenar" ≠ "aprender con el uso"

| Enfoque | Qué es | Costo/Riesgo | Rol en MIMO |
|---|---|---|---|
| **RAG + crecimiento de conocimiento** | Mejorar el contexto recuperado y ampliar la base de conocimiento | Bajo | **Fase 1** (principal) |
| **Clasificación de derivación** | Aprender cuándo escalar a partir del histórico de resultados | Medio | **Fase 2** |
| **Fine-tuning del modelo** | Adaptar pesos del modelo al dominio del tenant | Alto | **Fase 3** (opcional, último) |

El error común es saltar a fine-tuning. El **mayor retorno con menor riesgo** está en las Fases 1 y 2.

## 4. Señales disponibles (inventario de datos)

La materia prima ya se captura hoy (esquema por tenant); **no requiere reestructurar el core**:

| Señal | Origen actual | Uso |
|---|---|---|
| Transcripciones completas | `Conversation` + `Message` (rol User/Assistant/System/Agent) | Fases 1, 2, 3 |
| ¿Resuelto por bot o por humano? | Ciclo de `Ticket.Status` (BotActive→Resolved sin escalar vs InQueue/Assigned→…) | Fase 2 (etiqueta) |
| Motivo de escalamiento | `Ticket.EscalationReason` | Fases 1, 2 |
| Reapertura (resolución fallida) | `Ticket.Status = Reopened` | Fase 2 (señal negativa) |
| Calidad percibida | `SatisfactionSurvey.Rating` + `Observations` | Fases 1, 2, 3 |
| Respuesta humana de referencia | `Message` con rol `Agent` | Fase 1 (candidato a conocimiento) |
| Conocimiento + embeddings | `Document.Embedding` (`pgvector`, dim 1536) | Fases 1, 2 |
| Consumo de IA | `AiUsageRecord` | Cuotas/gobernanza |

**Señales a añadir (baratas):** *score* de recuperación semántica por consulta y un *flag*
`knowledge_gap` cuando la recuperación queda por debajo de un umbral o el caso escala por falta de
información. Es lo único que conviene instrumentar pronto para alimentar la Fase 1.
**✅ Implementado** (corte 1, [ADR 0023](adr/0023-instrumentacion-de-vacios-de-conocimiento.md)):
`KnowledgeQuerySignal` por consulta + `GET /knowledge/gaps`.

---

## 5. Fase 1 — Crecimiento de conocimiento asistido (RAG que mejora)

**Objetivo:** respuestas más precisas cerrando vacíos de conocimiento.

**Cómo funciona:**
1. **Detección de vacíos:** consultas con baja similitud de recuperación o casos escalados por
   "falta de información"; se agrupan preguntas frecuentes mal resueltas (clustering por embeddings
   con `pgvector`).
2. **Generación de borradores:** un proceso usa el `IAiGatewayService`/`ILlmClient` para **resumir**
   la conversación resuelta o la respuesta del agente en un **documento de conocimiento candidato**.
3. **Curación (HITL):** los candidatos entran a una **bandeja de sugerencias** en `Mimo.App`; el
   `TenantAdmin` aprueba/edita → se publica como `Document` (con su embedding) → mejora el RAG.

**Componentes/cambios:**
- Instrumentar *retrieval score* + *flag* `knowledge_gap` (ver §4).
- **Worker batch por tenant** (estilo `IHostedService`) que analiza conversaciones recientes y
  genera sugerencias.
- **UI de "Sugerencias de conocimiento"** (aprobar/editar/descartar) en el admin de tenant.
- Reutiliza `IVectorSearchService` (embeddings/clustering) y el gateway (resumen).

**Métricas:** cobertura de conocimiento, tasa de auto-resolución, CSAT, reducción de escalamientos
por "falta de info".

**Riesgos:** sugerencias incorrectas o ruidosas → mitigado con HITL + umbrales de
frecuencia/similitud. **Esfuerzo:** medio. **ROI:** el más alto.

---

## 6. Fase 2 — Derivación inteligente (clasificador de escalamiento)

**Objetivo:** decidir mejor cuándo basta la IA y cuándo entra un funcionario, y afinar el momento
del traspaso.

**Cómo funciona:**
- **Etiquetas derivadas del histórico:** `resuelto_por_bot` / `escalado_resuelto` / `reabierto` /
  `baja_calificación` (a partir de `Ticket` + `SatisfactionSurvey`).
- **Features por consulta:** similitud máxima de recuperación, categoría/intención, sentimiento,
  longitud del intercambio, canal, horario, reincidencia del cliente.
- **Modelo de decisión:** empezar simple con **LLM-as-judge + reglas/umbrales** (estima confianza
  de resolución) y, con suficiente volumen, migrar a un **clasificador ligero entrenado** offline
  por tenant. **No** es reentrenar el LLM.
- **Política configurable por tenant:** umbrales (sesgo conservador a escalar), respeto de horarios
  de atención, *override* humano siempre disponible.

**Integración:** el `ConversationOrchestrator` consulta la confianza **antes** de responder o
escalar; registra el resultado para medir aciertos (¿el escalamiento fue realmente necesario?).

**Métricas:** tasa de auto-resolución, % de escalamientos correctos, reaperturas, tiempo a primera
respuesta, CSAT.

**Riesgos:** **sobre-escalar** (satura al equipo) o **sub-escalar** (molesta al cliente) →
mitigado con umbrales conservadores, monitoreo y *override*. **Esfuerzo:** medio-alto.

---

## 7. Fase 3 — Modelo especializado por organización (fine-tuning, opcional)

**Objetivo:** un modelo afinado al dominio y al tono de la organización, cuando RAG ya no alcanza.

**Cómo funciona:**
- **Dataset curado** de conversaciones de **alta calidad** (CSAT alto, aprobadas), **anonimizado**
  (sin PII).
- **Adaptación** del modelo base (p. ej. *adapters*/LoRA o fine-tuning del proveedor), **por
  tenant**, versionada y evaluada **A/B contra el baseline RAG**.
- **Integración vía gateway:** el conector del tenant apunta al modelo afinado; el gateway sigue
  aplicando límites por plan (ADR 0005).

**Cuándo:** solo con **volumen alto + ROI claro** y cuando Fases 1–2 ya no muevan la aguja.

**Riesgos:** costo, *drift*, **privacidad** (PII → anonimización obligatoria), mantenimiento y
posible *lock-in* del proveedor. **Esfuerzo:** alto. **Es el último recurso, no el primero.**

---

## 8. Arquitectura (cómo encaja)

- **Gateway de IA in-process** (ADR 0005): ya media el acceso al LLM por tenant/plan; es el punto
  natural para enchufar (a) recuperación mejorada, (b) servicio de decisión de escalamiento,
  (c) selección del modelo (base vs afinado).
- **`pgvector`**: sustrato de recuperación y de *clustering* de preguntas; ya en uso para
  `Document.Embedding`.
- **Procesos batch por tenant**: análisis de conversaciones, generación de sugerencias y datasets;
  desacoplados del camino de petición en vivo.
- **Orquestador**: `ConversationOrchestrator` consume el servicio de decisión de escalamiento.

## 9. Aislamiento multi-tenant y privacidad

- Todo el aprendizaje (embeddings, sugerencias, features, datasets, modelos afinados) **deriva de y
  vive en el ámbito del propio tenant** (esquema por tenant; ADR 0009). **Nunca cross-tenant.**
- **PII:** anonimizar/seudonimizar antes de cualquier exportación o fine-tuning (Fase 3).
- Es además **argumento de venta**: "el asistente se vuelve tuyo" sin filtrar datos entre clientes.

## 10. Gobernanza por plan

- `AiPlanPolicy` habilita la capacidad y su **intensidad** (frecuencia de análisis, si hay
  fine-tuning, volumen de sugerencias).
- El consumo (resúmenes, evaluaciones) cuenta contra **cuotas** vía `AiUsageRecord`.

## 11. Métricas y evaluación

Definir **línea base** y seguir en el tiempo, **por tenant**:
- % de **auto-resolución** (sin funcionario).
- **CSAT** y % de calificaciones bajas.
- **Reaperturas** de tickets.
- **Cobertura de conocimiento** (consultas con buena recuperación).
- **Tiempo a primera respuesta** y a resolución.
- % de **escalamientos evitables** (Fase 2).

## 12. Riesgos transversales

| Riesgo | Mitigación |
|---|---|
| Respuestas incorrectas / alucinación | RAG fundamentado + HITL + citar fuente de conocimiento |
| *Drift* (el modelo/decisión se degrada) | Monitoreo de métricas + re-evaluación periódica |
| Privacidad / PII | Aislamiento por tenant + anonimización antes de entrenar |
| Costo de IA | Gobernanza por plan + cuotas + procesos batch |
| Sobre/sub-escalamiento | Umbrales conservadores + override humano + telemetría |

## 13. Dependencias / qué falta hoy

- ✅ **Hecho (corte 1, [ADR 0023](adr/0023-instrumentacion-de-vacios-de-conocimiento.md)):** instrumentar
  *retrieval score* + *flag* `knowledge_gap`. El orquestador registra una `KnowledgeQuerySignal` por
  consulta (similitud + nº de coincidencias + flag) en el esquema del tenant; `GET /knowledge/gaps`
  (TenantAdmin) lista los vacíos recientes. Verificado E2E.
- Worker batch por tenant + bandeja de curación de sugerencias (Fase 1) — **siguiente corte**.
- Pipeline de *features* y etiquetas para el clasificador (Fase 2).
- Pipeline de anonimización + exportación de dataset (Fase 3).

> Ninguna de estas dependencias bloquea el core. La **captura base ya existe**; el resto se
> construye cuando la capacidad se priorice como plus premium.

## 14. Decisiones abiertas

- Fase 2: ¿arrancar con **LLM-as-judge** o ir directo a **clasificador entrenado**?
- Fase 3: ¿fine-tuning propio vs del proveedor? ¿criterio de "volumen suficiente"?
- ¿Frecuencia de los procesos batch (diaria/semanal) y su impacto en cuotas?
- Anonimización: ¿automática (detección de PII) vs revisión manual?
