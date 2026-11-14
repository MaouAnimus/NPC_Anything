# 01. Acta de constitución y propuesta completa

## Ficha

| Campo | Definición inicial |
|---|---|
| Nombre | NPC Anything (provisional) |
| Versión del acta | 0.1 |
| Tipo | I+D aplicada / software para desarrollo de videojuegos |
| Estado | Propuesto; a validar |
| Patrocinio | Por definir |
| Responsable técnico | Por definir |
| Inicio de referencia | 14-11-2026 |
| Ciclo de trabajo | 20 semanas orientativas |

## Planteamiento del problema

En diferentes géneros se percibe una diferencia entre enemigos funcionales y enemigos creíbles. Un sistema simple puede reaccionar a la detección del jugador, seguir una ruta y atacar, pero repetir patrones, abandonar búsquedas de forma brusca o utilizar ventajas poco transparentes. Por otro lado, acompañantes que se interponen, no comunican riesgos o resuelven conflictos sin intervención del jugador afectan la inmersión. La causa no es únicamente falta de modelos de IA: importa también la integración con sensores, navegación, objetivos, animaciones, telemetría, diseño de dificultad y evaluación sistemática.

No se afirma que todos los juegos adolezcan de este problema; se plantea una hipótesis para validar en un escenario representativo y con entrevistas. Tampoco se asume que modelos generativos sean una solución eficiente para decisiones por fotograma.

## Visión

«Permitir que los equipos de videojuegos diseñen, prueben e integren comportamientos de NPC más coherentes, tácticos y justos, con herramientas modulares y un agente de ingeniería que reduzca el trabajo repetitivo de análisis e integración».

## Propuesta de valor

**Para desarrolladores:** un núcleo de comportamiento reutilizable, configurable y observable, con controles explícitos de dificultad y métricas.  
**Para modders autorizados:** un proceso de evaluación técnica por juego y un agente que documente puntos de integración y cambios propuestos; no una promesa de mod universal.  
**Para jugadores:** enemigos que pueden investigar, coordinarse, fallar de forma creíble, retirarse y responder a tácticas, y compañeros útiles que no arrebatan la agencia del jugador.

## Objetivo general

Diseñar, implementar y validar un prototipo de plataforma de inteligencia artificial modular para NPC enemigos y aliados, incorporando percepción contextual, memoria temporal, toma de decisiones por utilidad, coordinación limitada y herramientas de evaluación automatizada, junto con un agente de ingeniería que asista la integración de sistemas de IA en juegos o proyectos autorizados.

## Objetivos específicos

1. Identificar y documentar un baseline de IA convencional y sus limitaciones observables.
2. Definir contratos independientes del motor para percepción, memoria, decisión, acciones, configuración y telemetría.
3. Producir perfiles de al menos tres roles enemigos y uno aliado que generen decisiones observables diferentes.
4. Implementar dificultad mediante políticas y tolerancias, manteniendo estadísticas de combate fijas durante los experimentos.
5. Construir escenarios de prueba deterministas, métricas automáticas y evaluación cualitativa de justicia e inmersión.
6. Diseñar un agente de desarrollo con herramientas limitadas, trazabilidad, validaciones y revisión humana.
7. Evaluar viabilidad técnica, permisos y coste de al menos una integración futura fuera de la demo.

## Preguntas de investigación e hipótesis

- **H1:** con iguales estadísticas, la variante avanzada produce una mayor variedad de acciones tácticas útiles que la referencia.
- **H2:** jugadores que desconozcan la variante perciben coordinación y coherencia superiores, sin penalización sustantiva en justicia.
- **H3:** las pruebas repetibles detectan regresiones tácticas y de rendimiento antes de pruebas humanas.
- **H4:** un agente con playbook y contexto reduce pasos manuales en el análisis de una integración autorizada; solo se considerará validada si se mide tiempo y error frente a procedimiento manual.

## Beneficiarios y partes interesadas

| Actor | Interés | Necesidad principal |
|---|---|---|
| Jugador | Enfrentamientos divertidos | Desafío, señales claras, opciones de respuesta |
| Diseñador de juego | Ajuste de conducta | Parámetros interpretables y pruebas rápidas |
| Programador de gameplay | Integración | Interfaces estables y depuración |
| QA | Reproducibilidad | Semillas, logs, escenarios y umbrales |
| Modder con permisos | Adaptación | Diagnóstico de motor, riesgos y guía paso a paso |
| Responsable del proyecto | Viabilidad | Alcance, hitos, costes y evidencia de valor |

## Alcance del MVP

**Incluye:** demo 3D pequeña, navegación, visión/ruido, memoria por NPC, selector Utility AI, sistema de roles, coordinación restringida a comunicación plausible, compañero de apoyo, perfiles de dificultad, registros de decisión, baseline comparable y un prototipo del agente de análisis de proyecto local.  
**Excluye:** producción de arte, voces generativas, mundo abierto, aprendizaje por refuerzo en producción, servicio cloud, modificación de juegos comerciales, matchmaking, juego competitivo, bypass de anti-cheat y soporte simultáneo para múltiples motores.

## Entregables

D1: repositorio base y guía reproducible; D2: baseline y mapa; D3: SDK de comportamiento; D4: roles, coordinación y dificultad; D5: suite de pruebas y dashboard/reportes; D6: agente local de análisis; D7: evaluación de usuarios; D8: informe de viabilidad y propuesta de siguiente versión.

## Criterios de éxito preliminares (objetivos, no resultados)

- Se diferencian al menos tres roles enemigos y un compañero mediante eventos y trazas.
- **Cero decisiones basadas en posición exacta del jugador** cuando esa posición no es conocida por sensores/memoria válida.
- Cobertura, investigación, apoyo y retirada operan en escenarios definidos sin bloqueo de navegación.
- Las comparaciones mantienen daño, HP y armamento constantes; se guardan configuración y semilla.
- En el benchmark fijo de desarrollo, el presupuesto agregado de planificación de 20 NPC será inicialmente p95 ≤ 3 ms por tick de planificación; recalibrar tras medir.
- Prueba ciega exploratoria de 8–12 participantes que informe sobre inmersión, justicia, previsibilidad y utilidad de compañeros; sin afirmar significancia estadística.

## Restricciones

Equipo y presupuesto por confirmar; uso de herramientas compatibles con el sistema operativo disponible; prioridad a software abierto o gratuito; cualquier integración externa requiere permiso de uso y cumplimiento de licencias. El agente no ejecutará acciones destructivas por defecto.

## Aprobación del proyecto

**Condición de aprobación:** aceptar el alcance y las decisiones del documento 00; asignar responsable de producto y responsable técnico; definir disponibilidad semanal real; registrar riesgos y fecha del primer gate. Dejar por escrito cambios de alcance mediante ADR o RFC.
