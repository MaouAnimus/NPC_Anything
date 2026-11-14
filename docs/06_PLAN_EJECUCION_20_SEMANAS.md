# 06. Plan de ejecución y cronograma de 20 semanas

## Supuestos de planificación

Horizonte de referencia: **20 semanas** desde el inicio efectivo, con una persona técnica de **15–20 horas semanales** (capacidad indicativa total **300–400 horas**) y apoyo esporádico de diseño y QA. No se exige por principio una carta Gantt de 20 semanas: es una propuesta ajustable. Todo hito tiene evidencia, un gate y un plan de reducción de alcance. El desarrollo de integraciones para juegos cerrados no forma parte de este calendario.

## Cronograma ejecutivo

| Semanas | Fase | Actividades | Entregable y gate | Esfuerzo orientativo |
|---|---|---|---|---|
| 1–2 | F0 Fundaciones | Alcance, repositorio, versión de motor, mapa, baseline | G0: escena base y build reproducible | 25–35 h |
| 3–4 | F1 Percepción | Línea de visión, sonido simulado, pruebas de observación | G1: percepción sin omnisciencia | 25–40 h |
| 5–6 | F2 Memoria + decisión | Memoria TTL, utility selector, reglas y logging | G2: decisiones trazables y estables | 30–40 h |
| 7–8 | F3 Cobertura + navegación | Consulta de puntos, selección y replanteo | G3: cobertura robusta sin bloqueo | 30–40 h |
| 9–10 | F4 Roles | Asalto, flanqueador, apoyo, explorador | G4: diferencias observables por rol | 30–40 h |
| 11–12 | F5 Coordinación y aliado | Mensajes, reservas de cobertura, companion | G5: escuadrón y aliado funcionales | 30–40 h |
| 13–14 | F6 Dificultad y telemetría | Cuatro perfiles, runner, reason codes, reportes | G6: comparación A/B reproducible | 30–40 h |
| 15–16 | F7 Agente local N0 | Inspección read-only, inventario IA y reporte | G7: diagnóstico fundamentado | 30–40 h |
| 17–18 | F8 Evaluación | Stress tests, 8–12 playtests exploratorios, ajustes | G8: métricas + hallazgos documentados | 35–45 h |
| 19–20 | F9 Cierre MVP | Correcciones, demo, documentación, roadmap v0.2 | G9: release candidata + decisión | 25–40 h |

**Total aproximado:** 290–400 horas de ejecución. La incertidumbre es alta; considerar un colchón adicional o recortar roles/agente antes de comprometer fechas.

## Dependencias y ruta crítica

Mapa y baseline -> sensores -> memoria -> selector -> navegación/cobertura -> roles/coordinación -> escenario reproducible -> evaluación. El agente de ingeniería puede desarrollarse parcialmente en paralelo, pero el valor del producto exige primero un núcleo con comportamiento real verificable.

## Hitos y criterios para avanzar

| Gate | Condición de entrada a siguiente fase | Si no se cumple |
|---|---|---|
| G0 | Cualquier colaborador puede correr demo y baseline | Simplificar mapa/entorno |
| G1 | Test de oclusión y pista auditiva pasan | Corregir sensor antes de planificar |
| G2 | Misma entrada produce elección reproducible; sin bucles de acción | Reducir acciones y ajustar histéresis |
| G3 | Selección de cobertura alcanzable y fallback | Sustituir búsqueda dinámica por nodos predefinidos |
| G4 | Al menos 2 roles diferenciados por métricas | Posponer 2 roles opcionales |
| G5 | Mensajes con retardo y un aliado que no obstruye | Recortar órdenes avanzadas |
| G6 | Configs de dificultad, logs y baseline comparables | No iniciar estudio con jugadores |
| G7 | Agente identifica código de la demo con rutas y evidencia | Mantener reportes manuales asistidos |
| G8 | Resultados reproducibles y problemas priorizados | Declarar MVP exploratorio; no proclamar mejora |
| G9 | Guía, tests, release, riesgos y limitaciones actualizadas | No etiquetar como estable |

## Plan por sprint (10 sprints de 2 semanas)

**Sprint 01 (W1–2):** ADR-001 motor y stack; repositorio; mapa de arena; NPC baseline; primeros logs.  
**Sprint 02 (W3–4):** visual FOV/LOS; ruido; tests de falsa detección y oclusión.  
**Sprint 03 (W5–6):** memoria con confianza/TTL; acción elegible y utility selector; estabilidad e histéresis.  
**Sprint 04 (W7–8):** posiciones de cobertura; costo de ruta; reserva local; fallback.  
**Sprint 05 (W9–10):** perfiles de rol y señales; escenarios de especialización.  
**Sprint 06 (W11–12):** comunicación por canal, retardo, coordinación; compañero de apoyo.  
**Sprint 07 (W13–14):** perfiles de dificultad y runner A/B; instrumentación y reportes.
**Sprint 08 (W15–16):** agente read-only, herramientas locales, informe de puntos de integración.
**Sprint 09 (W17–18):** pruebas de estrés, panel básico, playtests exploratorios y correcciones.
**Sprint 10 (W19–20):** hardening, empaquetado, demo, reporte final, decisión v0.2.

## Gestión semanal

Ritmo recomendado: plan de sprint con máximo 3 objetivos verificables, revisión de tareas bloqueadas a mitad de sprint y demo al cierre. Mantener backlog con IDs, responsables, estados y tiempos invertidos. Congelar cambios de alcance durante las últimas dos semanas salvo bugs críticos.

## Roles / RACI simplificada

| Trabajo | Responsable principal | Colaborador opcional | Aprobador |
|---|---|---|---|
| Producto y criterios | Product owner | Diseñador de combate | Product owner |
| Arquitectura/runtime | Programador principal | Revisor técnico | Responsable técnico |
| Escenarios y UX | Diseñador de niveles | Programador | Product owner |
| Evaluación y QA | QA / programador | Usuarios voluntarios | Responsable técnico |
| Agente y seguridad | Programador de herramientas | Revisor de seguridad | Responsable técnico |
| Publicación | Responsable del proyecto | Colaborador documentación | Responsable del proyecto |

En un equipo de una persona, los roles pueden acumularse, pero la revisión de seguridad y las pruebas de usuarios conviene que involucren a terceros.

## Política de cambios de plazo

Todo cambio debe identificar RF afectado, incremento estimado, riesgo y decisión registrada. Primero reducir funciones **Could**, luego **Should**, nunca eliminar telemetría y fairness para cumplir artificialmente una fecha. La duración se redefine con datos después de G2.

## Backlog y seguimiento

Ver `07_BACKLOG.md` para tareas implementables y `15_PRIMER_SPRINT.md` para secuencia de arranque. Al cierre de cada sprint producir un reporte de una página: logrado, evidencia, problemas, métricas, decisiones y plan siguiente.
