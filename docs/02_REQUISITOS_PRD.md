# 02. PRD — documento de requisitos del producto

## Personas y escenarios

- **Diseñadora de combate:** quiere ajustar valentía, cautela y trabajo en equipo sin tocar daño y HP.
- **Programador de gameplay:** necesita añadir lógica sin acoplarla a nodos concretos del motor.
- **Analista QA:** necesita reproducir decisiones con eventos, semillas y hashes de configuración.
- **Jugador:** necesita reconocer intención enemiga y mantener oportunidades de contrajuego.
- **Modder autorizado (posterior):** quiere saber si una integración es viable antes de modificar archivos.

## Casos de uso primarios

**CU-01 Investigación:** jugador deja de ser visible; un NPC registra última posición observada, inspecciona durante ventana limitada y deja de perseguir si no encuentra evidencia.  
**CU-02 Cobertura:** NPC bajo amenaza selecciona un punto alcanzable con protección y evita ocupar el mismo sitio que otro.  
**CU-03 Coordinación:** NPC A avisa «contacto» a B dentro de rango/canal; B recibe información con retraso y nivel de confianza.  
**CU-04 Roles:** asalto presiona, flanqueador busca ruta lateral, apoyo suprime y explorador detecta; el conjunto se puede reconfigurar.  
**CU-05 Aliado:** compañero sigue al jugador, avisa de amenazas conocidas y no cruza deliberadamente la línea de fuego.  
**CU-06 Dificultad:** cambiar dificultad modifica tolerancias y patrones tácticos, no necesariamente estadísticas.  
**CU-07 Trazabilidad:** desarrollador inspecciona qué estímulos y puntuaciones provocaron una acción.  
**CU-08 Agente de ingeniería:** operador solicita diagnóstico de proyecto, recibe un reporte fundamentado y aprueba o rechaza propuesta de cambio.

## Requisitos funcionales (RF)

| ID | Requisito | Prioridad | Verificación |
|---|---|---|---|
| RF-01 | Ingerir estímulos visuales con oclusión y distancia | Must | Prueba de visión bloqueada |
| RF-02 | Ingerir señales auditivas con posición aproximada e incertidumbre | Must | Escenario de ruido tras pared |
| RF-03 | Guardar, actualizar y expirar recuerdos por NPC | Must | Test de caducidad |
| RF-04 | Evaluar acciones elegibles con un sistema de utilidad determinista | Must | Test de elección con semilla |
| RF-05 | Ejecutar acciones a través de un contrato con el motor | Must | Mock adapter y escena |
| RF-06 | Buscar cobertura alcanzable con coste y riesgo | Must | Mapa con tres opciones |
| RF-07 | Definir perfiles de rol mediante configuración | Must | Trazas de roles divergentes |
| RF-08 | Compartir información solo mediante canal permitido | Must | Test de aislamiento de comunicación |
| RF-09 | Permitir retirada/rendición desactivable y señales de intención | Should | Escenario de inferioridad |
| RF-10 | Añadir un compañero de apoyo no intrusivo | Must | Escenario acompañante |
| RF-11 | Ofrecer dificultad configurable con HP y daño fijos | Must | Comparación de config |
| RF-12 | Registrar eventos de percepción, decisión y acción en JSONL | Must | Verificar schema y continuidad |
| RF-13 | Repetir escenarios con semillas y estados iniciales fijados | Must | Dos ejecuciones comparables |
| RF-14 | Exponer panel simple de depuración de estado y prioridades | Should | Inspección manual |
| RF-15 | Agente local identifica estructura y tecnologías de proyecto autorizado | Should | Informe con evidencia |
| RF-16 | Agente sugiere integración con diff revisable, sin autoaplicar | Should | Aprobación requerida |
| RF-17 | Exportar reporte de evaluación por escenario | Must | Archivo JSON/Markdown |
| RF-18 | Cargar perfil diferente sin recompilar núcleo | Should | Cambio configuración |

## Requisitos no funcionales (RNF)

| ID | Requisito | Objetivo inicial | Método |
|---|---|---|---|
| RNF-01 | Rendimiento | p95 de selección agregada de 20 NPC ≤ 3 ms/tick en benchmark fijado | Profiler y 1.000 muestras |
| RNF-02 | Juego fluido | No regresión de FPS/frametime material frente a baseline | Perfil fijo y comparación |
| RNF-03 | Reproducibilidad | Misma semilla y mismo estado generan misma elección pura | Tests unitarios |
| RNF-04 | Observabilidad | 100% de decisiones estratégicas tienen reason code | Auditoría de trazas |
| RNF-05 | Seguridad | Sin escritura externa al workspace por defecto | Test de permisos |
| RNF-06 | Privacidad | No recopilar datos personales de jugadores | Revisión de logs |
| RNF-07 | Modularidad | Core sin dependencias del motor | Build aislado |
| RNF-08 | Recuperación | Config inválida falla con mensaje claro y usa fallback seguro | Test de schema |
| RNF-09 | Justicia | El NPC no consume datos omniscientes fuera de interfaz válida | Instrumentación de acceso |
| RNF-10 | Mantenibilidad | Tests y documentación por cada módulo público | Checklist CI |

**Nota:** los números de RNF son umbrales de hipótesis sujetos a benchmark real; no equivalen a SLA.

## Políticas de experiencia de juego

1. El NPC puede fallar, pero sus fallas han de resultar explicables y configurables.
2. Un enemigo jamás «adivina» la coordenada real del jugador sin señal explícita.
3. Las maniobras peligrosas se anuncian mediante señales legibles (voz, gesto, audio o movimiento).
4. Las dificultades superiores mejoran coordinación, disciplina y uso de información, no confieren reflejos instantáneos ni visión a través de paredes.
5. El aliado protege la agencia del jugador: no inicia combates indiscriminados ni realiza objetivos que requieren decisión del usuario.

## Historias de usuario con criterios de aceptación

**US-01:** Como diseñador quiero configurar agresividad de rol para observar cambios de táctica. **Dado** el mismo escenario y semilla, **cuando** aumento la preferencia de presión dentro de los límites admitidos, **entonces** el registro debe mostrar más decisiones de presión en condiciones elegibles; no se exige variación si la acción es ilegal.

**US-02:** Como jugador quiero que un enemigo pierda mi rastro cuando me escondo correctamente. **Dado** una última visión válida, **cuando** se supera TTL sin nuevas pistas, **entonces** la información exacta caduca y el NPC vuelve a búsqueda general/patrulla.

**US-03:** Como QA quiero reproducir una maniobra. **Dado** versión, configuración, escenario, semilla y estado inicial, **cuando** ejecuto el test, **entonces** los eventos lógicos producidos son equivalentes dentro de tolerancias definidas.

**US-04:** Como desarrollador quiero usar el agente de análisis sin exponer mis proyectos. **Dado** un workspace local autorizado, **cuando** solicito un diagnóstico, **entonces** el agente lee solo rutas permitidas, redacta rutas privadas del informe y no modifica archivos.

**US-05:** Como jugador quiero un compañero útil. **Dado** un enemigo detectado fuera de mi campo visual, **cuando** el aliado dispone de percepción válida, **entonces** puede comunicar la amenaza, mantener distancia de seguridad y evitar bloquear mi ruta.

## Priorización MoSCoW

- **Must:** baseline, percepción, memoria, Utility AI, cobertura, dos roles diferenciados, coordinación controlada, aliado mínimo, telemetría, pruebas.
- **Should:** cuatro roles completos, panel visual de depuración, agente local con reportes y diffs propuestos.
- **Could:** GOAP avanzado, sistema moral complejo, adaptación al estilo de jugador, entrenamiento RL, diálogo dinámico.
- **Won't (v0.1):** soporte universal y aplicación automática a ejecutables comerciales.

## Definición de terminado (DoD) transversal

Código revisado, pruebas automáticas relevantes en verde, demo de escenario, telemetría, documentación actualizada, evidencia de performance cuando aplica, sin regresión de fairness y referencia al requisito RF/RNF correspondiente.
