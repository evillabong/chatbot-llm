# MIMO — Propuesta de valor y alcance

**Atención al cliente omnicanal, potenciada con IA generativa.**

MIMO es una plataforma para que las organizaciones centralicen y profesionalicen la atención a
sus clientes y ciudadanos: un asistente con IA responde al instante las consultas frecuentes y,
cuando hace falta una persona, entrega la conversación a tu equipo con todo el contexto, sin
fricción y desde una sola bandeja.

> Documento de presentación orientado al **modelo del producto**: su funcionalidad, su alcance y
> el valor que entrega. No incluye condiciones comerciales.

---

## 1. Resumen ejecutivo

Las organizaciones reciben consultas por muchos canales (web, redes sociales, mensajería) y por
distintos temas (soporte, información, ventas). Responder a tiempo, con calidad y sin perder el
hilo es costoso y difícil de escalar.

**MIMO unifica toda esa atención en un solo lugar** y combina dos fuerzas:

- **Un asistente conversacional con IA generativa (modelos de lenguaje / LLM)** que atiende 24/7,
  entiende en lenguaje natural y responde con la información oficial de tu organización, resolviendo
  lo repetitivo sin intervención humana.
- **Un equipo humano** que entra solo cuando aporta valor, con la conversación completa, enrutada
  al área correcta y con herramientas para resolver rápido.

El resultado: **respuestas más rápidas, equipos más productivos y clientes más satisfechos.**

---

## 2. El problema que resuelve

- La atención está **dispersa** en varios canales y herramientas; nadie tiene la foto completa.
- El equipo **repite** las mismas respuestas una y otra vez.
- Las consultas se **pierden, se enfrían o se duplican** sin un enrutamiento claro.
- Es difícil **medir** la calidad de la atención y la satisfacción del cliente.
- Crecer en volumen implica **crecer en costos** de personal de forma lineal.

---

## 3. La solución en una frase

> Un punto único de atención donde la IA resuelve lo masivo y el equipo humano resuelve lo
> importante, con enrutamiento inteligente, contexto completo y métricas de calidad.

### Cómo funciona (flujo de una conversación)

1. **El cliente escribe** desde cualquier canal habilitado.
2. **El asistente responde** usando la base de conocimiento de la organización.
3. Si la consulta lo requiere, **se escala a una persona** del área adecuada (soporte, ventas,
   etc.), con todo el historial.
4. **El agente atiende en vivo**, puede transferir a otro equipo o pedir apoyo interno, y **cierra**
   el caso.
5. **El cliente califica** la atención; la organización obtiene métricas para mejorar.

---

## 4. Funcionalidades

Vista general de los módulos del sistema y para qué sirve cada uno:

```text
MIMO
├─ Atención omnicanal                      # Recibe y responde por todos los canales desde una sola bandeja
│  ├─ Canales (web, redes, mensajería)     # El cliente escribe por el canal que prefiere
│  └─ Bandeja unificada                    # Todas las conversaciones en un único lugar
├─ Asistente con IA generativa (LLM)       # Responde al instante, 24/7, en lenguaje natural
│  └─ Base de conocimiento                 # Fuente oficial de las respuestas (responde con TU información)
├─ Consola de agentes                      # El puesto de trabajo del equipo
│  ├─ Conversación en tiempo real          # Chat en vivo con el cliente, con todo el historial
│  └─ Acciones de atención                 # Tomar, responder, transferir, resolver y cerrar
├─ Enrutamiento y colas                    # Cada consulta llega al área y a la persona correcta
│  ├─ Departamentos / áreas                # Soporte, ventas, envíos, etc.
│  ├─ Asignación manual o automática       # Reparte la carga entre los agentes disponibles
│  └─ Transferencias                       # Pasa el caso a otro equipo sin perder el contexto
├─ Tickets y seguimiento                   # Ciclo de vida del caso: estado, prioridad y trazabilidad
├─ Equipos y permisos                      # Define roles/departamentos y qué ve y hace cada uno
├─ Encuestas de satisfacción               # Mide la calidad y recoge comentarios del cliente
├─ Configuración de la organización        # Adapta la operación sin depender del proveedor
│  ├─ Horarios de atención                 # Cuándo atiende el equipo y mensajes fuera de horario
│  ├─ Reglas de asignación y encuestas     # Cómo se reparten los casos y cómo se evalúan
│  └─ Personalización del chat             # Marca, color y mensaje de bienvenida
├─ Ventas  (opcional)                      # Convierte consultas en oportunidades sin cambiar de herramienta
├─ Administración de la plataforma         # Operación centralizada para varias organizaciones
│  ├─ Organizaciones                       # Alta y gestión de cada cliente
│  ├─ Planes y capacidades                 # Qué funciones y volúmenes habilita cada plan
│  └─ Uso y seguimiento                    # Visibilidad del consumo y la actividad
└─ Mejora continua  (en evolución/premium) # El asistente mejora con el uso de la organización
   ├─ Sugerencia de conocimiento           # Propone nuevas respuestas a partir de lo ya resuelto
   └─ Derivación inteligente               # Aprende cuándo basta la IA y cuándo entra un funcionario
```

A continuación, el detalle de cada módulo:

### 4.1 Atención omnicanal unificada
Recibe y responde conversaciones de múltiples canales (chat web embebible y mensajería/redes
sociales) desde **una sola bandeja**. El cliente usa el canal que prefiere; el equipo trabaja en
un único lugar.

### 4.2 Asistente conversacional con IA generativa (LLM)
El corazón de MIMO es un asistente basado en **modelos de lenguaje (LLM)**: entiende las consultas
en **lenguaje natural** —no menús rígidos ni palabras clave— y **genera** respuestas claras a
partir de la **información oficial** de la organización. Conversa con naturalidad, se adapta a cómo
pregunta cada persona, atiende de inmediato a cualquier hora y descarga al equipo de lo repetitivo.
Es la diferencia entre un bot tradicional de respuestas predefinidas y un **asistente que realmente
entiende y responde**.

### 4.3 Base de conocimiento (respuestas fundamentadas en tu información)
La organización publica sus políticas, procedimientos y respuestas (envíos, devoluciones,
horarios, preguntas frecuentes, etc.). El asistente **responde apoyándose en ese contenido, no
improvisa**: así las respuestas son **pertinentes, consistentes y verificables**, y se mantienen al
día con solo actualizar el conocimiento. El contenido puede marcarse como público o interno.

### 4.4 Consola de agentes (bandeja de trabajo)
Una vista tipo bandeja moderna donde cada agente ve:
- **Sus conversaciones** y las **del equipo**.
- La **conversación completa** con el cliente, en tiempo real.
- Acciones para **tomar**, **responder**, **transferir**, **resolver** y **cerrar**.

### 4.5 Enrutamiento, colas y transferencias
Las conversaciones se dirigen al **área o departamento correcto** (soporte, ventas, envíos…) y se
pueden **transferir** entre equipos conservando el contexto. La asignación puede ser manual o
**automática y balanceada** entre los agentes disponibles.

### 4.6 Gestión de equipos y permisos
Define **departamentos/roles** y asigna funcionarios. Cada rol ve y atiende lo que le corresponde;
los supervisores tienen visibilidad ampliada. La administración de personas y permisos es
autogestionable por cada organización.

### 4.7 Tickets y seguimiento
Cada caso que requiere atención humana se gestiona como un **ticket** con estado y prioridad,
desde que entra a la cola hasta que se resuelve y cierra, con trazabilidad del proceso.

### 4.8 Encuestas de satisfacción
Al cerrar la atención, el cliente puede **calificar** y dejar comentarios. La organización mide la
calidad y puede definir reglas (por ejemplo, reabrir casos con baja calificación).

### 4.9 Configuración por organización
Cada organización adapta su operación sin depender de terceros: mensajes de atención, **horarios
de servicio**, modo de asignación, reglas de encuestas y **personalización del chat** (mensaje de
bienvenida, identidad visual).

### 4.10 Ventas como capacidad opcional
El núcleo es la **atención al cliente**, pero muchas consultas terminan en una **oportunidad de
venta**. MIMO permite activar la venta como una capacidad adicional para gestionar esos casos sin
salir de la conversación.

### 4.11 Administración de la plataforma
Un panel de administración permite operar el servicio para varias organizaciones, gestionar sus
planes y dar seguimiento al uso de forma centralizada.

---

## 5. Beneficios

**Para la organización**
- Atención **24/7** sin multiplicar el equipo.
- Respuestas **consistentes** alineadas a sus políticas.
- **Visibilidad y métricas** de calidad y satisfacción.
- Operación **escalable**: más volumen no significa costos proporcionales.

**Para el equipo (agentes y supervisores)**
- Una sola bandeja, **sin saltar entre apps**.
- Contexto completo: **menos retrabajo**, atención más rápida.
- Enrutamiento y transferencias claras: **el caso llega a quien debe**.

**Para el cliente final**
- Respuesta **inmediata** a lo frecuente.
- Continuidad: **no tiene que repetir** su problema.
- Atención por **su canal preferido**.

---

## 6. Alcance

**Incluye**
- Atención omnicanal centralizada en una bandeja.
- Asistente con IA sobre la base de conocimiento de la organización.
- Consola de agentes con mensajería en tiempo real.
- Enrutamiento por departamento, colas, transferencias y tickets.
- Gestión de equipos, roles y permisos por organización.
- Encuestas de satisfacción y configuración operativa.
- Modelo multi-organización con datos **aislados** por cliente.
- Capacidad de ventas activable.

**Fuera de alcance (en esta propuesta)**
- Integraciones a medida con sistemas internos del cliente (se evalúan por separado).
- Migración de datos históricos desde otras herramientas.
- Personalizaciones específicas no contempladas en el modelo estándar.

---

## 7. Modelo del software

- **Servicio en la nube, multi-organización:** cada organización opera en su propio espacio, con
  sus datos **aislados y privados**, su equipo y su configuración.
- **Autogestión:** cada organización administra su conocimiento, su equipo y sus reglas de
  atención sin depender del proveedor para el día a día.
- **Planes por capacidades:** la propuesta se estructura en planes que habilitan distintos niveles
  de uso y funcionalidades (por ejemplo, canales disponibles, tamaño del equipo y capacidades
  opcionales como ventas). *Las condiciones comerciales se tratan por separado.*
- **Capacidad de IA gobernada por plan:** los **modelos de lenguaje (LLM)** disponibles y el
  **volumen de atención asistida** se definen según el plan de cada organización. Es una palanca
  clave del modelo: más capacidad de IA, mayor automatización de la atención.
- **Listo para crecer:** la organización puede empezar simple y ampliar capacidades a medida que
  evoluciona su operación.

---

## 8. ¿Para quién es?

Organizaciones que atienden a un volumen relevante de clientes o ciudadanos y quieren
profesionalizar y escalar esa atención. Por ejemplo:

- **Comercio y e‑commerce:** estado de pedidos, devoluciones, dudas de producto, ventas.
- **Servicios y suscripciones:** soporte, facturación, retención.
- **Sector público y educativo:** atención e información a la ciudadanía.
- **Salud, finanzas y servicios profesionales:** orientación y agendamiento.

---

## 9. Diferenciadores

- **IA generativa que responde con TU conocimiento:** modelos de lenguaje que entienden y conversan,
  pero fundamentados en la información de la organización —ni respuestas rígidas de menú ni
  contenido genérico inventado.
- **IA + humano, bien combinados:** la IA no reemplaza al equipo; lo potencia y le entrega los
  casos listos para resolver.
- **Una sola bandeja, de verdad omnicanal:** menos herramientas, más foco.
- **Pensado para equipos:** roles, enrutamiento, transferencias y colaboración interna.
- **Atención como ventaja competitiva:** con métricas para mejorar de forma continua.
- **La venta surge de la atención:** sin forzar, sin cambiar de herramienta.

---

## 10. Evolución: el asistente mejora con el uso *(capacidad en evolución)*

MIMO está pensado para **mejorar con la operación diaria**, no para quedarse estático. A partir de
las conversaciones resueltas, las calificaciones de satisfacción y las decisiones del equipo, la
plataforma evoluciona en dos frentes:

- **Respuestas más precisas:** identifica vacíos y preguntas frecuentes mal resueltas y **propone
  nuevo conocimiento** (con aprobación humana), de modo que el asistente responde cada vez mejor.
- **Derivación más inteligente:** aprende **cuándo una consulta puede resolverse solo con IA y
  cuándo conviene un funcionario**, afinando el momento del traspaso para no molestar al cliente ni
  saturar al equipo.

Principios de esta capacidad:

- **Por organización y privada:** cada cliente mejora con **sus propios datos**; el aprendizaje
  **nunca se mezcla** entre organizaciones. El asistente se vuelve, con el tiempo, "más tuyo".
- **Con supervisión humana:** las mejoras se proponen y se aprueban; no se cambian solas.
- **Gradual y por plan:** es un **plus** que se habilita según el plan, no el núcleo del servicio.

> Es una capacidad en evolución dentro del roadmap del producto; el núcleo de atención omnicanal con
> IA funciona desde el primer día.

## 11. Próximos pasos

1. **Demostración guiada** con un caso de negocio de ejemplo.
2. **Definición del alcance** para la organización (canales, departamentos, base de conocimiento).
3. **Puesta en marcha** y acompañamiento inicial.

> ¿Conversamos sobre cómo MIMO puede transformar la atención de tu organización?
