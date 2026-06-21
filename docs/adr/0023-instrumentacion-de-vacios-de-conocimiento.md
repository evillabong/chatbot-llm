# 0023. Instrumentación de señales de recuperación y vacíos de conocimiento

- Estado: Aceptado
- Fecha: 2026-06-20
- Decisores: evill

## Contexto

La [visión de mejora continua](../vision-mejora-continua.md) (pendiente #22) describe que el asistente
debe "aprender con el uso" mediante **bucles alrededor del LLM** (recuperación + curación +
clasificación), no fine-tuning. Su Fase 1 (crecimiento de conocimiento asistido) depende de poder
**detectar vacíos**: consultas que la base de conocimiento del tenant no cubre bien.

El diseño (§4 y §13) señala que lo **único que conviene instrumentar pronto** es el *score* de
recuperación semántica por consulta y un *flag* `knowledge_gap`. Hasta ahora el orquestador descartaba
la distancia coseno: `IVectorSearchService.SearchAsync` devolvía solo los documentos, sin exponer qué
tan buena fue la coincidencia.

## Decisión

- **Exponer la señal de recuperación.** Nuevo `IVectorSearchService.SearchScoredAsync` que, además de
  los documentos, devuelve la **mejor similitud** (`1 - distancia coseno`) y el número de coincidencias
  (`VectorSearchResult`). `SearchAsync` se conserva como envoltura para los consumidores existentes.
- **Decisión pura y probable.** `KnowledgeGapEvaluator.IsGap(topSimilarity, matchCount, threshold)`
  marca vacío cuando no hay coincidencias, no hay similitud, o la mejor similitud queda por debajo del
  umbral (`DefaultSimilarityThreshold = 0.75`). Sin dependencias, unit-testeable.
- **Captura en el orquestador, best-effort.** Tras la búsqueda en el camino LLM, `ConversationOrchestrator`
  registra una `KnowledgeQuerySignal` (consulta recortada, similitud, nº de coincidencias, flag). Un fallo
  al registrar **nunca** interrumpe la atención. **Solo** se registra cuando la búsqueda se ejecutó: si
  el LLM/embeddings no están disponibles y la búsqueda se degrada, **no** se registra una señal falsa
  (no es un vacío real de conocimiento). El camino de **flujo guiado** (#27, determinista, sin RAG) no
  genera señales.
- **Persistencia por tenant.** Tabla `knowledge_query_signals` en el **esquema del tenant** (ADR 0009),
  con índices por `(knowledge_gap, created_at)` y por `conversation_id`. Aislada por organización; nunca
  cross-tenant.
- **Lectura para curación.** `GET /knowledge/gaps` (política **TenantAdmin**) lista los vacíos más
  recientes (`limit` acotado 1–200). Es la base de la futura bandeja de sugerencias de conocimiento.

## Consecuencias

### Positivas

- Habilita la Fase 1 de mejora continua sin reestructurar el core: la captura ya alimenta la detección
  de vacíos. **Verificado E2E:** migración aplicada a los esquemas de tenant en el arranque;
  `GET /knowledge/gaps` devuelve solo filas con `knowledge_gap = true` (TenantAdmin 200, sin token 401,
  `limit` respetado); con el LLM caído la búsqueda se degrada y **no** se registra señal falsa.
  Unit tests del evaluador de umbral.
- Reutiliza `pgvector` y el gateway de IA; el contrato del chat no cambia.

### Negativas / Costos

- El umbral (0.75) es un **valor inicial fijo**; conviene calibrarlo por tenant/modelo con datos reales.
- La captura **requiere embeddings activos**: en entornos sin credenciales de LLM no se generan señales
  (comportamiento intencional, no un error).
- Falta lo que sigue de la Fase 1: **worker batch** de agrupamiento de consultas, **generación de
  borradores** (resumen vía gateway) y la **UI de curación** (aprobar/editar/descartar → `Document`).
  Se registran como cortes siguientes del #22.

## Alternativas consideradas

- **Guardar la distancia en el `Message.Metadata`** en vez de una tabla propia: descartado; dificulta
  consultar/agrupar vacíos y mezcla telemetría de mejora continua con el historial de chat.
- **Calcular el vacío solo desde el motivo de escalamiento**: insuficiente; muchos vacíos no escalan y
  la similitud es una señal más temprana y barata.
