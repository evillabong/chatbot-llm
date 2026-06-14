# 0006. Convención de endpoints sin parámetros en la ruta

- Estado: Aceptado
- Fecha: 2026-06-14
- Decisores: evill

## Contexto

Los endpoints usaban parámetros de ruta (`/{id:guid}`, `/{roleId}`, `/{channel}`, etc.).
Se busca una convención uniforme y predecible para la ubicación de los parámetros en
todas las APIs (`Mimo.Api` y `Mimo.Admin.Api`).

## Decisión

Los endpoints **no llevan parámetros en la ruta**. Cada parámetro se ubica según su tipo:

- **Parámetros simples** (id, filtros, paginación) → **query string**.
- **Objetos complejos** → **body**.
- **Parámetros de seguridad** (tokens, firmas) → **header**.

La ruta solo contiene segmentos literales que identifican el recurso y la acción.
Para no colisionar con el listado (`GET /recurso`), la obtención de un único elemento
usa un segmento literal de acción: `GET /recurso/detail?id=`. Las acciones sobre un
recurso son segmentos literales con el id por query: `POST /tickets/claim?id=`,
`POST /documents/reindex?id=`, etc.

Ejemplos de la conversión aplicada:

| Antes | Después |
|---|---|
| `GET /agents/{id}` | `GET /agents/detail?id=` |
| `PUT /agents/{id}` | `PUT /agents?id=` (cuerpo en body) |
| `DELETE /agents/{id}` | `DELETE /agents?id=` |
| `GET /tickets/queue/{roleId}` | `GET /tickets/queue?roleId=` |
| `POST /tickets/{id}/claim` | `POST /tickets/claim?id=` |
| `POST /conversations/{id}/messages` | `POST /conversations/messages?id=` (cuerpo en body) |
| `POST /conversations/{id}/survey` | `POST /conversations/survey?conversationId=` |
| `POST /webhooks/{channel}/incoming` | `POST /webhooks/incoming?channel=` |

En Minimal APIs, al quitar el token de la plantilla de ruta, los parámetros simples se
enlazan automáticamente desde la query y los tipos complejos desde el body; no se
requieren atributos `[FromRoute]`.

## Consecuencias

### Positivas

- Ubicación de parámetros uniforme y predecible en todo el proyecto.
- La firma de seguridad ya viaja por header (p. ej. `X-Hub-Signature-256`), alineada con la regla.

### Negativas / Costos

- Se pierde la validación de formato del constraint de ruta (`:guid`); el binding de query
  devuelve 400 ante un valor inválido, lo que es aceptable.
- URLs menos "RESTful" (acciones como `/detail`, `/claim`); es una decisión consciente de
  consistencia interna.
- Los webhooks externos deben configurarse con el canal por query (`?channel=`).

## Alternativas consideradas

- **Mantener parámetros de ruta (REST clásico)**: descartado por la preferencia explícita
  de uniformidad en la ubicación de parámetros.
