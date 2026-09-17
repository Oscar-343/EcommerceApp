---
name: project-memory
description: Gestiona la memoria persistente y estructurada de los proyectos. Utilízala para recordar decisiones importantes, arquitectura, convenciones, estado, problemas conocidos y tareas pendientes entre sesiones. No almacenes conversaciones completas ni información trivial.
---

# Project Memory

## Propósito

Esta habilidad permite mantener una memoria persistente y estructurada del
proyecto para que la información importante sobreviva entre sesiones.

La memoria debe complementar el contexto de la conversación, nunca sustituirlo.

No se debe almacenar una conversación completa ni registrar cada acción realizada.

---

## Ubicación

Cuando esta habilidad se utilice dentro de un proyecto, busca una carpeta:

`.agent/`

Dentro de ella pueden existir:

- `MEMORY.md`
- `DECISIONS.md`
- `PROJECT_STATE.md`
- `TASKS.md`

Si la carpeta o alguno de estos archivos no existe, no los crees
automáticamente solamente por haber cargado esta habilidad.

Créelos cuando exista una necesidad real de persistir información.

---

# 1. PRINCIPIO FUNDAMENTAL

La memoria debe responder a esta pregunta:

> "¿Esta información seguirá siendo útil en una sesión futura?"

Si la respuesta es no, probablemente no debe guardarse.

Una buena memoria contiene conocimiento útil y persistente.

Una mala memoria contiene un historial de acciones.

---

# 2. QUÉ DEBE GUARDARSE

La memoria puede contener:

- Decisiones arquitectónicas.
- Tecnologías utilizadas.
- Estructura importante del proyecto.
- Convenciones de programación.
- Reglas específicas del proyecto.
- Problemas conocidos.
- Soluciones importantes a problemas recurrentes.
- Estado importante del proyecto.
- Tareas pendientes relevantes.
- Restricciones técnicas.
- Dependencias importantes.
- Preferencias persistentes del usuario relacionadas con el proyecto.
- Decisiones que afecten futuras implementaciones.

---

# 3. QUÉ NO DEBE GUARDARSE

No guardes:

- Conversaciones completas.
- Mensajes del usuario completos.
- Respuestas completas del agente.
- Acciones triviales.
- Archivos creados que no tengan relevancia futura.
- Errores temporales ya solucionados y sin importancia futura.
- Resultados normales de compilación.
- Información que pueda obtenerse fácilmente inspeccionando el proyecto.
- Información duplicada.
- Datos sensibles innecesarios.
- Contraseñas.
- API keys.
- Tokens.
- Secretos.
- Credenciales.

No conviertas la memoria en un registro de actividad.

---

# 4. CUÁNDO LEER LA MEMORIA

Al comenzar una tarea dentro de un proyecto:

1. Determina si existe memoria del proyecto.
2. Consulta la memoria relevante para la tarea.
3. No leas necesariamente todos los archivos si no es necesario.
4. Utiliza la información encontrada para evitar decisiones contradictorias.

Si la tarea afecta arquitectura, consulta especialmente:

`DECISIONS.md`

Si la tarea depende del estado actual:

`PROJECT_STATE.md`

Si existen tareas pendientes relacionadas:

`TASKS.md`

Si necesitas conocer convenciones o información general:

`MEMORY.md`

---

# 5. CUÁNDO ACTUALIZAR LA MEMORIA

Actualiza la memoria cuando durante el trabajo ocurra algo
que pueda afectar futuras sesiones.

Ejemplos:

- Se toma una decisión arquitectónica.
- Se cambia una tecnología.
- Se establece una nueva convención.
- Se descubre una restricción importante.
- Se encuentra una solución relevante a un problema complejo.
- Cambia significativamente el estado del proyecto.
- Se crea una tarea pendiente importante.
- El usuario establece una preferencia permanente para ese proyecto.

No actualices la memoria por cada modificación.

---

# 6. CLASIFICACIÓN

Antes de guardar información, determina dónde corresponde.

## MEMORY.md

Utilízalo para conocimiento general y persistente del proyecto.

Ejemplos:

- Tecnologías principales.
- Arquitectura general.
- Convenciones.
- Reglas importantes.
- Información estructural que sea difícil de inferir.

---

## DECISIONS.md

Utilízalo para decisiones técnicas o arquitectónicas importantes.

Cada decisión debe indicar:

- Fecha.
- Decisión.
- Motivo.
- Consecuencia cuando sea relevante.

Formato recomendado:

```text
## YYYY-MM-DD — Título de la decisión

### Decisión

Descripción de la decisión.

### Motivo

Por qué se tomó.

### Consecuencia

Qué implica para el proyecto.
````

Cuando una decisión anterior sea reemplazada, actualiza o marca
la decisión anterior como reemplazada. No mantengas decisiones
contradictorias como si ambas siguieran vigentes.

---

## PROJECT_STATE.md

Utilízalo para representar el estado actual relevante del proyecto.

Puede incluir:

* Módulos terminados.
* Módulos en desarrollo.
* Problemas conocidos.
* Integraciones pendientes.
* Estado de pruebas.
* Bloqueos importantes.

No lo conviertas en un historial detallado.

Debe representar principalmente el estado ACTUAL.

---

## TASKS.md

Utilízalo para tareas pendientes importantes.

Formato recomendado:

```text
## Pendientes

- [ ] Tarea pendiente

## En progreso

- [ ] Tarea actualmente en desarrollo

## Completadas relevantes

- [x] Tarea importante completada
```

No registres tareas triviales.

---

# 7. ACTUALIZACIÓN AUTOMÁTICA

Puedes actualizar la memoria automáticamente cuando identifiques
información claramente importante y persistente.

No necesitas pedir permiso para guardar información normal de proyecto
cuando esta cumpla los criterios de memoria.

Sin embargo:

* No guardes información trivial.
* No guardes secretos.
* No guardes información sensible innecesaria.
* No registres conversaciones completas.
* No dupliques información existente.

Si no estás seguro de si algo merece memoria, es preferible no guardarlo
antes que llenar la memoria con información de poco valor.

---

# 8. CONSISTENCIA

La memoria debe representar el estado actual.

Antes de agregar información:

1. Comprueba si ya existe.
2. Si existe y sigue siendo válida, no la dupliques.
3. Si cambió, actualiza la información anterior.
4. Si una decisión fue reemplazada, indícalo claramente.
5. Evita contradicciones.

La información más reciente y explícitamente confirmada por el usuario
tiene prioridad sobre información antigua.

---

# 9. NO INVENTAR MEMORIA

Nunca inventes información para completar la memoria.

Si una decisión no fue tomada, no la registres como tomada.

Si desconoces una parte del proyecto, indícalo.

No conviertas una suposición en una decisión documentada.

Diferencia siempre entre:

* Hecho confirmado.
* Inferencia.
* Propuesta.
* Decisión aprobada.

Solo las decisiones realmente aprobadas deben registrarse como decisiones.

---

# 10. RELACIÓN CON AGENTS.md

`AGENTS.md` define cómo debe comportarse el agente.

La memoria define información específica que el agente debe recordar.

No copies las reglas generales de `AGENTS.md` dentro de la memoria.

Ejemplo:

Correcto:

```text
AGENTS.md
→ El agente debe utilizar código limpio.

MEMORY.md
→ El proyecto utiliza ASP.NET Core MVC.
```

Incorrecto:

```text
MEMORY.md
→ El agente debe utilizar código limpio.
→ El agente debe hablar español.
→ El agente debe pedir permiso para commits.
```

Esas son reglas globales y pertenecen a `AGENTS.md`.

---

# 11. VERIFICACIÓN

Antes de confiar en información almacenada en memoria:

* Comprueba que no contradiga el código actual.
* Si el proyecto ha cambiado, actualiza la memoria.
* El código y configuración actuales tienen prioridad sobre una memoria
  antigua cuando exista una contradicción.

La memoria ayuda a comprender el proyecto, pero no reemplaza la
inspección del código.

---

# 12. OBJETIVO

El objetivo de esta habilidad es que el agente pueda volver a un proyecto
después de días o semanas y recuperar rápidamente:

* Qué es el proyecto.
* Cómo está construido.
* Qué decisiones se tomaron.
* Qué problemas existen.
* Qué queda pendiente.
* Qué convenciones debe respetar.

La memoria debe ahorrar contexto y evitar repetir decisiones,
no convertirse en un segundo proyecto paralelo.

````
# 13. INICIALIZACIÓN DE MEMORIA

Cuando el usuario solicite explícitamente inicializar la memoria del
proyecto, por ejemplo:

"Inicializa la memoria del proyecto."

debes:

1. Comprobar si existe la carpeta `.agent/`.
2. Si no existe, crearla.
3. Comprobar cuáles de los siguientes archivos existen:
   - `.agent/MEMORY.md`
   - `.agent/DECISIONS.md`
   - `.agent/PROJECT_STATE.md`
   - `.agent/TASKS.md`
4. Crear únicamente los archivos que no existan.
5. No eliminar ni sobrescribir información existente.
6. No inventar información del proyecto.

Los archivos nuevos deben tener una estructura inicial sencilla.

### MEMORY.md

Debe contener:

```md
# Project Memory

## Información general

Pendiente de definir.
14. PROTOCOLO DE MANTENIMIENTO

Cuando una tarea aprobada produzca información importante y persistente,
determina si debe actualizarse la memoria.

Antes de modificar la memoria:

Comprueba si la información ya existe.
Comprueba si contradice una decisión anterior.
Actualiza la información existente cuando corresponda.
Evita duplicados.
Mantén la memoria breve y estructurada.

Cuando una decisión técnica importante sea reemplazada:

conserva el registro histórico de la decisión anterior;
márcala como reemplazada;
registra la nueva decisión como vigente.

Cuando el usuario indique explícitamente que una información debe
recordarse para futuras sesiones del proyecto, esa información debe
considerarse candidata prioritaria para la memoria, siempre que no sea
un secreto o información sensible innecesaria.

15. COMPROBACIÓN ANTES DE TERMINAR UNA TAREA

En tareas medianas o grandes, antes de finalizar:

Determina si se produjo alguna decisión, cambio arquitectónico,
restricción, problema conocido o tarea pendiente que sea relevante
para futuras sesiones.
Si existe información relevante, actualiza la memoria correspondiente.
No registres cambios triviales.
Informa brevemente al usuario si la memoria fue actualizada.

La memoria debe mantenerse pequeña, útil y coherente con el proyecto.