# 07. Backlog inicial priorizado

## Convenciones

`P0`: necesario para MVP; `P1`: importante con posible reducción; `P2`: posterior. Estimaciones en **horas de trabajo focalizado**, sujetas a revisión después de la primera semana. Una tarea solo se marca completa cuando pasa DoD del PRD. Los IDs enlazan requisitos cuando corresponde.

| ID | Historia / entrega | Prio | Dependencia | Est. h | Evidencia de aceptación |
|---|---|---|---|---:|---|
| BK-001 | Crear repositorio, convenciones y CI de tests | P0 | — | 6 | Build y test desde checkout limpio |
| BK-002 | Elegir versión de Godot/.NET y documentarla | P0 | — | 3 | ADR aprobado |
| BK-003 | Arena con coberturas y 2 rutas | P0 | BK-002 | 10 | Mapa carga y se recorre |
| BK-004 | NPC baseline patrulla/detecta/ataca | P0 | BK-003 | 12 | Grabación y log de referencia |
| BK-005 | Define `Stimulus`, `MemoryFact`, `ActionDecision` | P0 | BK-001 | 5 | Contratos compilables y tests |
| BK-006 | Sensor visual LOS/FOV | P0 | BK-003 | 10 | Escenario tras pared no detecta |
| BK-007 | Sensor de ruido con incertidumbre | P0 | BK-003 | 7 | No revela posición exacta |
| BK-008 | Store de memoria con TTL/confianza | P0 | BK-005 | 9 | Tests expiran correctamente |
| BK-009 | Utility selector con acciones elegibles | P0 | BK-005 | 12 | Reproducción misma semilla |
| BK-010 | Histéresis/cooldown del selector | P0 | BK-009 | 6 | No oscillation en test |
| BK-011 | Adaptador Godot de lectura de mundo | P0 | BK-005,006 | 10 | Sin APIs Godot en Core |
| BK-012 | Adaptador Godot de acciones | P0 | BK-009 | 9 | Acción iniciada/terminada |
| BK-013 | Selector de puntos de cobertura | P0 | BK-011,012 | 14 | 3 coberturas puntuadas |
| BK-014 | Reserva y liberación de cobertura | P0 | BK-013 | 6 | No colisión de reservas |
| BK-015 | Timeout, cancelación y fallback | P0 | BK-012 | 7 | Acción bloqueada se recupera |
| BK-016 | Perfil rol asalto | P0 | BK-009 | 5 | Trazas de presión observables |
| BK-017 | Perfil rol flanqueador | P0 | BK-013 | 8 | Ruta lateral válida |
| BK-018 | Perfil rol apoyo | P1 | BK-013 | 6 | Apoyo sin fuego indiscriminado |
| BK-019 | Perfil rol explorador | P1 | BK-006 | 6 | Detección/aviso |
| BK-020 | Mensajes de escuadrón + TTL | P0 | BK-008 | 10 | Llega solo por canal válido |
| BK-021 | Control de retardo y alcance del mensaje | P0 | BK-020 | 7 | Desconectado no recibe |
| BK-022 | Compañero follow/hold/assist | P0 | BK-012,020 | 12 | No obstruye test de pasillo |
| BK-023 | Sistema de dificultad con perfiles | P0 | BK-009,008 | 9 | HP/daño idénticos |
| BK-024 | Logging JSONL con reason codes | P0 | BK-005 | 8 | Cero decisiones sin reason |
| BK-025 | Runner escenarios deterministas | P0 | BK-004,024 | 13 | 10 seeds con resultados exportables |
| BK-026 | Métricas automáticas y comparador | P0 | BK-025 | 12 | Reporte A/B con parámetros |
| BK-027 | Overlay de depuración | P1 | BK-024 | 9 | Visión + acción + confianza visibles |
| BK-028 | Test de conocimiento / anti-omnisciencia | P0 | BK-006,008 | 6 | Ningún acceso a world truth |
| BK-029 | Bench de p95 / uso CPU | P0 | BK-025 | 9 | Registro hardware y versión |
| BK-030 | Agente: inventario local de proyecto | P1 | BK-001 | 10 | Reporte con evidencia relativa |
| BK-031 | Agente: localizar puntos IA | P1 | BK-030 | 13 | Referencias a clases y rutas |
| BK-032 | Agente: modelo de permisos/allowlist | P0 | BK-030 | 8 | Test de escape de rutas |
| BK-033 | Agente: exportación de reporte Markdown/JSON | P1 | BK-030 | 6 | Distingue hecho/inferencia |
| BK-034 | Informe legal y elegibilidad de mod externo | P0 | — | 5 | Checklist revisado |
| BK-035 | Test ciego de percepción/juego con usuarios | P0 | BK-025 | 12 | Consentimiento y resultados anonimizados |
| BK-036 | Análisis de resultados y límites | P0 | BK-035,026 | 12 | No atribuye causalidad sin control |
| BK-037 | Video/demo de comportamiento (opcional) | P1 | BK-036 | 7 | Mismo escenario A/B |
| BK-038 | Guía de instalación y resolución de problemas | P0 | BK-001 | 7 | Tercero puede ejecutar |
| BK-039 | Informe final MVP y propuesta v0.2 | P0 | BK-036 | 8 | Decisión go/no-go |
| BK-040 | Integración Unity | P2 | BK-039 | Por estimar | SPIKE con PoC |
| BK-041 | Patches revisables en copia aislada | P2 | BK-031 | Por estimar | Diff + rollback |
| BK-042 | Entrenamiento RL de una conducta concreta | P2 | BK-039 | Por estimar | A/B demuestra valor incremental |

**Atención:** la suma de horas puntuales no sustituye una planificación ajustada por interrupciones, refactor, aprendizaje del motor ni pruebas. Los sprints agrupan actividades y pueden variar tras cada gate.

## Épicas

- **EP-01:** Infraestructura y baseline (001–004).
- **EP-02:** Sensores, memoria y decisión (005–012, 028).
- **EP-03:** Comportamiento táctico y roles (013–023).
- **EP-04:** Medición, QA y experiencia (024–029, 035–039).
- **EP-05:** Agente de ingeniería (030–034, 041).
- **EP-06:** Expansiones (040, 042).

## Lista de bloqueo a tratar en planificación

Versiones exactas del motor/SDK; disponibilidad real; assets y licencias para prototipo; equipo de QA; equipo para benchmarking; localización de dos perfiles de usuarios; definición de qué cuenta como cobertura y qué métrica demuestra coordinación útil.
