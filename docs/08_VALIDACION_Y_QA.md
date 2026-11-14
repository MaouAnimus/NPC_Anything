# 08. Plan de validación, QA y experimentos

## Propósito

Probar tres cosas por separado: **corrección técnica**, **rendimiento** y **calidad percibida**. Una IA que derrota más al jugador no es, por sí sola, una IA mejor. La prueba A/B requiere mapear los mismos recursos y constantes de combate entre variantes.

## Unidades de análisis

- **Tick de decisión:** latencia de selección, acciones candidatas y motivo.
- **Encuentro:** flanqueos válidos, coberturas alcanzadas, mensajes útiles, eventos de pérdida de rastro, bloqueos y daño recibido/causado como variable contextual.
- **Sesión:** satisfacción, fairness, legibilidad, sorpresa no frustrante y utilidad del compañero.

## Matriz de escenarios obligatorios

| ID | Escenario | Estímulo controlado | Aserción esperada |
|---|---|---|---|
| SC-01 | Pared opaca | Jugador detrás de pared | No detecta por visión |
| SC-02 | Última posición vista | Jugador sale de FOV | Recuerda ubicación estimada y expira |
| SC-03 | Ruido tras obstáculo | Ruido lejos y ocluido | Investiga región aproximada |
| SC-04 | Tres coberturas | Una bloqueada, dos libres | Elige una alcanzable |
| SC-05 | Cobertura compartida | Dos NPC eligen mismo punto | Reserva evita doble ocupación |
| SC-06 | Flanqueo | Frente protegido y lateral libre | Flanqueador intenta ruta elegible |
| SC-07 | Corte de comunicaciones | A ve, B está aislado | B no adquiere información de A |
| SC-08 | Escuadrón reducido | Un aliado queda fuera de combate | Los demás reevalúan el plan |
| SC-09 | Compañero en pasillo | Jugador transita espacio estrecho | Compañero no obstruye persistentemente |
| SC-10 | Cambio dificultad | Mismo daño, HP, equipamiento y seed | Tácticas cambian según perfil |
| SC-11 | Camino inaccesible | Se bloquea ruta en ejecución | Timeout y fallback, sin loop infinito |
| SC-12 | Veinte NPC | Stress sobre hardware fijado | Registrar p50/p95/p99 y frametime |

## Diseño experimental A/B

**A:** IA básica de referencia (patrulla/detecta/ataca) con los mismos sensores máximos permitidos, daño, armas, vida, mapa y cobertura. **B:** IA enriquecida. Emparejar versiones, semilla, posición inicial, NPCs y comportamiento del jugador simulado cuando sea posible. Ejecutar inicialmente 30 seeds por escenario técnico (propuesta a ajustar) y publicar distribución, no solo promedio. Registrar diferencias no controladas. En sesiones humanas, balancear orden A/B y ocultar la variante al jugador; 8–12 participantes sirven como *exploración*, no como prueba estadística concluyente.

## Métricas cuantitativas

| Métrica | Definición | Interpretación |
|---|---|---|
| `valid_cover_ratio` | Coberturas alcanzadas / intentos elegibles | Robustez del sistema |
| `invalid_knowledge_events` | Decisiones basadas en conocimiento prohibido | Objetivo: 0 |
| `coordination_success_ratio` | Maniobras completadas / maniobras válidas iniciadas | Utilidad de coordinación |
| `tactical_diversity` | Distribución de acciones útiles por contexto | Evita patrón repetitivo; no maximizar sin sentido |
| `decision_latency_p95_ms` | p95 del tiempo de selector por tick | Rendimiento |
| `npc_frame_cost_ms` | Costo total agregado de IA por frame/tick | Rendimiento |
| `stuck_seconds` | Tiempo con acción de movimiento sin progreso | Robustez navegación |
| `fairness_rating` | Escala 1–7 tras sesión | Justicia percibida |
| `immersion_rating` | Escala 1–7 tras sesión | Inmersión percibida |
| `companion_usefulness` | Escala 1–7 tras sesión | Ayuda sin sobreintervención |

`coordination_success_ratio` requiere reglas por escenario; un NPC puede decidir correctamente **no** flanquear si el riesgo supera el beneficio. El número de flanqueos nunca debe ser el único KPI.

## Eventos a registrar (JSONL)

`session_start`, `stimulus_received`, `memory_updated`, `candidate_scored`, `action_selected`, `action_completed`, `action_failed`, `message_sent`, `message_received`, `memory_expired`, `session_end`. Incluir run id, seed, build, perf profile, versión, contexto permitido, tiempo del juego, causa y resultado. Limitar sampling para no afectar FPS ni registrar contenido personal.

## Protocolo de benchmarking

1. Fijar hardware, SO, versión del motor, build, resolución, calidad gráfica y cantidad de NPC.
2. Calentar escena con varias ejecuciones; distinguir warm-up de medición.
3. Ejecutar al menos 1.000 ticks de planificación por perfil cuando se mida latencia.
4. Reportar p50/p95/p99, desviación y fallos; comparar baseline.
5. Repetir tras optimizaciones, no aceptar cifras tomadas con distintas condiciones.

## Cuestionario breve para participantes

Después de cada variante, puntuar 1 (nada) a 7 (mucho): (1) «Entendí por qué me detectaron»; (2) «Las tácticas parecieron coherentes»; (3) «Tuve oportunidades razonables de respuesta»; (4) «El enfrentamiento se sintió repetitivo»; (5) «El compañero me ayudó sin quitarme control». Preguntas abiertas: «¿Qué comportamiento notaste?», «¿En qué momento sentiste injusticia?». Añadir consentimiento informado y no guardar datos de identificación innecesarios.

## Bugs y severidad

- **Bloqueante:** crash, fuga de datos, escritura externa, loop infinito, omnisciencia verificable.
- **Alta:** NPC queda atascado, companion bloquea objetivo, decisiones sin motivo, regresión de FPS severa.
- **Media:** comportamiento incoherente ocasional, señal confusa, métrica incompleta.
- **Baja:** defectos visuales de overlay, documentación o pequeños ajustes.

## Criterio de salida de MVP

No deben quedar bugs bloqueantes; todas las pruebas SC-01 a SC-12 deben tener resultado documentado, incluyendo fallos conocidos; benchmark reproducible; evidencia del estudio exploratorio; decisión explícita sobre H1–H4 como apoyada/no apoyada/no evaluada. Publicar solo conclusiones sustentadas por datos.
