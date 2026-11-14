# 09. Gestión de riesgos, seguridad, ética y aspectos legales

## Registro de riesgos

Escala cualitativa: probabilidad (B/M/A) e impacto (B/M/A). Prioridad alta si cualquiera es alta con daño significativo. Revisar al cierre de cada sprint.

| ID | Riesgo | Prob. | Impacto | Mitigación | Señal / contingencia |
|---|---|---|---|---|---|
| R-01 | Alcance de «todos los juegos» inviable | A | A | MVP en demo propia | Recortar integraciones externas |
| R-02 | Dependencia excesiva de LLM en runtime | M | A | Selector determinista, LLM offline | Deshabilitar llamadas en gameplay |
| R-03 | IA aparentemente hace trampas | A | A | Información delimitada y test anti-omnisciencia | Bloquear release si se confirma |
| R-04 | NPC empeora rendimiento | M | A | Scheduling, caches, profiling | Reducir frecuencia y consultas |
| R-05 | Pathfinding se atasca | A | M | Timeouts, ruta alternativa, pruebas | Fallback seguro |
| R-06 | Compañero quita agencia al jugador | M | A | Políticas de no intervención | Ajustar órdenes/prioridades |
| R-07 | Sobreajuste a una arena | A | M | Múltiples seeds y variaciones | Añadir escenario nuevo |
| R-08 | Resultados A/B sin control | M | A | Congelar stats/armas/mapa | Rehacer experimento |
| R-09 | Motor o biblioteca cambia API | M | M | Pin de versiones y ADR | Actualización controlada |
| R-10 | Acceso indebido del agente a archivos | M | A | Allowlist, sandbox y aprobaciones | Deshabilitar herramientas de escritura |
| R-11 | Prompt injection desde archivos del juego | M | A | Tratar texto externo como datos | Rechazar instrucciones externas |
| R-12 | Licencia no permite distribución | M | A | Checklist jurídico por proyecto | No publicar mod; solo guía permitida |
| R-13 | Uso contra anti-cheat / partidas ajenas | M | A | Política de exclusión y controles | No integrar ni recomendar bypass |
| R-14 | Falta de disponibilidad del equipo | A | M | Alcance MoSCoW y gates | Aplazar roles/agente |
| R-15 | Ausencia de usuarios para pruebas | M | M | Reclutar temprano | Documentar limitación cualitativa |
| R-16 | Exposición de rutas/tokens/logs | M | A | Redacción y análisis local | Eliminar artefactos sensibles |
| R-17 | Métricas incentivan táctica absurda | M | M | Métricas múltiples y revisión humana | Redefinir KPI |
| R-18 | Conflictos de propiedad intelectual | M | A | No redistribuir assets, respetar licencias | Suspender publicación |

## Políticas para el agente

- Trabajar solo con proyectos propios, abiertos o expresamente autorizados.
- Mantener copias de prueba separadas de instalaciones y partidas originales.
- Respetar términos del juego, motores y mod loaders.
- No intervenir en videojuegos multijugador competitivos ni sortear antitrampas.
- Pedir aprobación explícita antes de instalar herramientas, ejecutar builds de terceros, aplicar parches o publicar.
- Evitar subir códigos, saves, archivos del juego y datos privados a servicios externos sin autorización.
- Incorporar auditoría de herramientas: quién pidió operación, cuál fue el permiso, qué archivos fueron leídos y escritos, qué pruebas se hicieron.

## Seguridad de herramientas MCP

Cada herramienta declara esquema estricto, límites de tamaño, timeout, rutas permitidas y resultados estructurados. Validar todas las entradas de ruta; eliminar rutas simbólicas que salgan del workspace. No exponer `exec_shell` en MVP. Para tool outputs, distinguir texto de terceros de instrucciones del usuario. Registrar efectos secundarios y solicitudes de aprobación. El estado inicial es read-only.

## Estrategia de licenciamiento

No elegir licencia definitiva sin identificar dependencias, recursos visuales, snippets y metas comerciales. Una licencia permisiva como MIT/Apache-2.0 puede ser candidata para **código propio**, con atribuciones que correspondan, pero no afecta licencias de motores, assets ni juegos. Si se reutiliza código de Co-op Anything se debe revisar su licencia y conservar avisos aplicables; la inspiración metodológica no implica uso de código. Usar `THIRD_PARTY_NOTICES.md` si se incorporan librerías con obligaciones.

## Seguridad y privacidad de pruebas con jugadores

Obtener consentimiento para sesiones/grabaciones, pedir participación voluntaria, explicar la finalidad, minimizar datos y usar IDs seudónimos. No guardar chats personales, ubicaciones ni cuentas. Permitir que una persona solicite retirar sus datos de prueba cuando corresponda.

## Condiciones de no publicación

No liberar parche ni instalador si modifica archivos originales sin backup; no está documentado el rollback; viola la licencia; requiere evadir anti-cheat; contiene archivos propietarios; hay vulnerabilidad de permisos no resuelta; o el reporte de QA afirma resultados no ejecutados.

## Plan de contingencia

Si surgen problemas de viabilidad externa, mantener el proyecto como SDK para desarrolladores. Si el agente aumenta significativamente riesgo/costo, aplazarlo y completar primero núcleo + evaluador. Si un LLM no agrega mejoras verificadas, no integrarlo en runtime.
