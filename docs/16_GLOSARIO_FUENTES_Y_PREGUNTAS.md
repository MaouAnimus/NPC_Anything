# 16. Glosario, bibliografía y decisiones abiertas

## Glosario

- **NPC:** personaje no jugador controlado por el sistema.
- **Behavior Tree:** estructura jerárquica que decide qué tareas ejecutar.
- **FSM:** máquina de estados finitos para transiciones de comportamiento.
- **Utility AI:** elección de acciones según valores de utilidad dependientes de contexto.
- **GOAP:** planificación orientada a objetivos a partir de acciones y precondiciones.
- **Blackboard:** memoria de trabajo con hechos y estados útiles para el NPC.
- **TTL:** tiempo hasta la expiración de un hecho o mensaje.
- **FOV/LOS:** campo de visión / línea de visión.
- **EQS:** sistema de consultas del entorno de Unreal.
- **LLM:** modelo de lenguaje grande; aquí se reserva principalmente para tareas de ingeniería offline.
- **MCP:** protocolo para exponer herramientas y recursos a agentes externos.
- **ADR:** registro de decisión arquitectónica y su justificación.
- **Baseline:** comportamiento inicial contra el cual comparar versiones.
- **Seed:** semilla que controla aspectos aleatorios para reproducir escenarios.
- **P95:** percentil 95 de una distribución de tiempos.
- **Fairness:** percepción y reglas de justicia; el NPC usa información plausible y ofrece contrajuego.
- **Modding:** modificación permitida de un videojuego conforme a mecanismos y licencias.

## Referencias técnicas consultadas (acceso 14-11-2026)

1. Co-op Anything, repositorio y README: https://github.com/cranebeaker/coop-anything
2. Co-op Anything, instrucciones para agentes: https://github.com/cranebeaker/coop-anything/blob/main/AGENTS.md
3. Co-op Anything, herramientas MCP: https://github.com/cranebeaker/coop-anything/blob/main/playbook/mcp.md
4. Godot, navegación (versión estable): https://docs.godotengine.org/en/stable/tutorials/navigation/
5. Godot, NavigationAgents: https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_using_navigationagents.html
6. Unity ML-Agents, repositorio: https://github.com/Unity-Technologies/ml-agents
7. Unity ML-Agents, documentación: https://unity-technologies.github.io/ml-agents/ML-Agents-Toolkit-Documentation/
8. Unreal Engine, AI Perception: https://dev.epicgames.com/documentation/en-us/unreal-engine/ai-perception-in-unreal-engine
9. Unreal Engine, Environment Query System: https://dev.epicgames.com/documentation/en-us/unreal-engine/environment-query-system-in-unreal-engine

Estas fuentes fundamentan capacidades de motores o el método de trabajo de Co-op Anything; **no prueban** que la solución propuesta ya exista ni validan nuestras métricas.

## Preguntas abiertas que no bloquean la documentación

1. ¿Qué género exacto tendrá la arena (shooter táctico, supervivencia, sigilo)? La propuesta usa shooter táctico como escenario de referencia.
2. ¿Se priorizará Windows o Linux? Se recomienda probar ambos si hay recursos, pero comenzar por el equipo disponible.
3. ¿Godot C# es adecuado según experiencia del equipo? Confirmarlo en G0.
4. ¿Qué perfil de desarrollador/modder se beneficiaría primero? Validar con entrevistas.
5. ¿Qué significa realmente «mejor» para la audiencia? Recoger datos de inmersión, justicia y legibilidad.
6. ¿Se cuenta con voluntarios y tiempo para pruebas? Reclutar antes de W12.
7. ¿Cuál sería el primer juego existente elegible? No elegir antes de validar permisos, hooks y la demo.
8. ¿Qué licencia tendrá el código propio y cómo se atribuirán dependencias? Decidir con inventario.

## Revisión documental prevista

Versionar documentos importantes con PR. Registrar supuestos confirmados, métricas medidas y ADR sustituidos. No convertir objetivos en hechos sin evidencia. Al publicar resultados, incluir reproducibilidad, condiciones y limitaciones.
