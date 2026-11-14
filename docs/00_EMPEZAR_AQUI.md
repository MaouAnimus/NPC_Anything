# 00. Empezar aquí: guía operativa

## Resumen para quien inicia el proyecto

**Necesidad:** en muchos videojuegos, la oposición de enemigos puede sentirse mecánica y la dificultad puede depender excesivamente de estadísticas. También existen compañeros poco autónomos o que entorpecen al jugador.  
**Solución candidata:** construir una capa de decisión de NPC basada en percepción limitada, memoria temporal, objetivos de rol, coordinación y evaluación de utilidad; después, automatizar con un agente la integración y las pruebas en proyectos autorizados.  
**Prueba de valor:** comparar la misma escena con la IA de referencia y la IA propuesta, manteniendo constantes daño, puntos de vida, mapa, armamento y condiciones iniciales.

## Supuestos de arranque

| Dimensión | Supuesto para planificar | Revisión necesaria |
|---|---|---|
| Público | Desarrolladores independientes y modders con permisos | Entrevistas con 5–8 interesados |
| Motor de la demo | Godot 4.x estable con C#/.NET | Comprobar versión y soporte en equipos de prueba |
| Entorno | Windows o Linux en PC de desarrollo | Documentar sistema y hardware |
| Equipo | Una persona técnica, 15–20 h/semana | Ajustar alcance al tiempo real |
| Duración | 20 semanas, no requisito obligatorio | Validar después del Sprint 1 |
| Presupuesto | Preferencia por soluciones locales y gratuitas | Decidir equipo, hosting y API opcional |
| Juegos externos | Excluidos del MVP de comportamiento | Evaluar con checklist de viabilidad y licencias |

## Tres decisiones que se deben registrar antes de programar

1. **Motor:** confirmar Godot C# o sustituirlo, manteniendo el núcleo desacoplado.
2. **Escenario:** elegir un único mapa compacto con cobertura, dos rutas de flanqueo y obstáculos; ninguna campaña ni arte definitivo.
3. **Métrica principal:** acordar que el resultado no es «gana más veces», sino comportamiento diferenciado, justo y reproducible.

## Primeras diez jornadas de trabajo (propuesta, no calendario fijo)

| Jornada | Acción concreta | Evidencia de finalización |
|---|---|---|
| 1 | Crear repositorio, `docs/`, `src/`, `tests/`, `demo/`, `tools/`; acordar licencia | README, estructura y proyecto inicial |
| 2 | Instalar versión estable del motor, SDK .NET compatible y herramientas de calidad | Proyecto abre y ejecuta una escena vacía |
| 3 | Diseñar el mapa con cobertura y posiciones de prueba | Escena jugable sin IA avanzada |
| 4 | Programar NPC de referencia: patrulla, detectar, perseguir, atacar | Grabación de comportamiento base |
| 5 | Añadir eventos de percepción con línea de visión y sonido simulado | Logs `seen`, `heard`, `lost` |
| 6 | Crear estructura de memoria temporal por NPC | Registro de conocimiento y caducidad |
| 7 | Definir acciones y evaluador de utilidad en un ensamblado independiente | Pruebas unitarias de decisión |
| 8 | Implementar selección de cobertura y navegación básica | NPC llega a cobertura alcanzable |
| 9 | Crear cuatro escenarios con semilla fija y exportación JSONL | Resultados repetibles y comparables |
| 10 | Revisar PRD, ADR y backlog según hallazgos | Acta de Sprint 1 y decisión de continuar |

## Qué se considera «inicio completo»

- Repositorio versionado y ejecutable por otra persona con instrucciones.
- Una escena de prueba y una referencia básica, aun sin IA inteligente.
- Registro de semilla, versión, configuración, hardware y métricas.
- Primeras pruebas automáticas del núcleo.
- Lista explícita de elementos pendientes y sus responsables.

## Orden de construcción y principios

Primero se crea un **baseline verificable**, luego se agregan capacidades de IA una por una. Nunca se atribuye una mejora a la «IA» si simultáneamente cambian daño, salud, velocidad o visibilidad. Los resultados experimentales se guardan con identificadores y evidencia. Los LLM quedan fuera del bucle crítico de gameplay.

## Próxima revisión

Al cierre de la semana 2 debe existir una demo básica y un informe de viabilidad con decisión **seguir / simplificar / cambiar motor**. Consultar `06_PLAN_EJECUCION_20_SEMANAS.md` y `08_VALIDACION_Y_QA.md`.
