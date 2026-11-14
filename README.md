# NPC Anything — paquete de documentación inicial

**Versión:** 0.1.0 (propuesta inicial)  
**Fecha:** 14 de noviembre de 2026  
**Estado:** diseño; aún no existe una implementación verificada  
**Nombre:** «NPC Anything» es provisional y requiere comprobar disponibilidad antes de publicarlo.

## Qué es

NPC Anything es una propuesta de plataforma para mejorar el comportamiento de enemigos y compañeros de videojuegos. Se compone de **(A) un núcleo de IA en tiempo de ejecución** que toma decisiones reproducibles con información limitada al conocimiento del NPC, y **(B) un agente de ingeniería asistido por IA** que inspecciona proyectos o juegos autorizados, recomienda integraciones, genera cambios revisables y ayuda a probarlos. La inspiración metodológica procede de [Co-op Anything](https://github.com/cranebeaker/coop-anything), no de una supuesta compatibilidad universal lista para usar.

## Decisión de alcance para el punto de partida

- **MVP:** juego de demostración propio, en Godot 4.x estable con .NET/C#, tres enemigos coordinados y un compañero de apoyo; sin llamadas a LLM durante el combate.
- **Prototipo del agente:** servicio local Python + herramientas estilo MCP, inicialmente de solo lectura; escribe propuestas o parches únicamente en un workspace de pruebas y con aprobación humana.
- **Más adelante:** adaptador para Unity, evaluación de juegos existentes con API de modding y, solo cuando exista autorización y viabilidad técnica, modificaciones acotadas.
- **No prometido:** compatibilidad con todos los juegos; soporte multijugador competitivo; inteligencia general; aprendizaje autónomo persistente; integración con ejecutables cerrados en el MVP.

## Ruta de lectura recomendada

1. `docs/00_EMPEZAR_AQUI.md` — primeras decisiones y ejecución de los próximos diez días.
2. `docs/01_ACTA_Y_PROPUESTA.md` — problema, justificación, visión, alcance y objetivos.
3. `docs/02_REQUISITOS_PRD.md` — requisitos, usuarios, historias y criterios de aceptación.
4. `docs/03_ARQUITECTURA.md` + `docs/04_DISENO_IA_NPC.md` — diseño técnico y modelo de comportamiento.
5. `docs/05_AGENTE_INGENIERIA.md` — agente inspirado en el método de Co-op Anything.
6. `docs/06_PLAN_EJECUCION_20_SEMANAS.md` + `docs/07_BACKLOG.md` — trabajo calendarizado y tareas.
7. `docs/08_VALIDACION_Y_QA.md` — método para demostrar mejora real.
8. `docs/09_RIESGOS_LEGAL_Y_SEGURIDAD.md` — riesgos, límites y controles.
9. Documentos 10–16 — interfaces, integraciones, decisiones, presupuesto, lanzamiento, guía de contribución y referencias.

## Entregables incluidos

Documentación de producto y técnica; cronograma orientativo de 20 semanas; backlog identificado por IDs; contratos de ejemplo; matrices de pruebas y riesgos; instrucciones del primer sprint; plantillas editables y archivos de configuración ilustrativos. **Los archivos de ejemplo son especificaciones/datos de partida, no una integración funcional.**

## Objetivo de validación del MVP

Demostrar con registros, escenas repetibles y una evaluación de jugadores que un grupo de NPC: percibe sin omnisciencia, recuerda pistas durante un tiempo limitado, ocupa coberturas, se coordina bajo restricciones, ajusta su conducta según roles y permite variar la dificultad principalmente por táctica y percepción. La condición de éxito exige además mantener la respuesta del juego dentro de un presupuesto medido de rendimiento y no empeorar la sensación de justicia.

## Fuentes de referencia

- https://github.com/cranebeaker/coop-anything
- https://github.com/cranebeaker/coop-anything/blob/main/AGENTS.md
- https://github.com/cranebeaker/coop-anything/blob/main/playbook/mcp.md
- https://docs.godotengine.org/en/stable/tutorials/navigation/
- https://github.com/Unity-Technologies/ml-agents
- https://dev.epicgames.com/documentation/en-us/unreal-engine/ai-perception-in-unreal-engine

**Nota:** todos los parámetros, presupuestos y umbrales que aparecen en este paquete son supuestos de planificación, no mediciones realizadas ni compromisos de producto.
