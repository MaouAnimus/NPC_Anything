# 12. Viabilidad técnica, económica y organizacional

## Viabilidad técnica

**Alta para prototipo controlado:** percepción visual básica, memoria temporal, Utility AI, roles y navegación son problemas implementables con herramientas de motores actuales. **Media** para coordinación creíble y dificultad que sea realmente divertida: depende de iteración y diseño de niveles. **Baja/no determinada** para adaptar automáticamente cualquier juego sin APIs: habría ingeniería inversa, incompatibilidades, permisos y prueba por build.

## Alternativas arquitectónicas

| Alternativa | Pros | Contras | Decisión propuesta |
|---|---|---|---|
| Script único por NPC | Rápido para demo mínima | Poco reusable, difícil probar | Solo baseline |
| FSM + Utility AI + reglas | Predictible, medible, flexible | Requiere diseño de scoring | **Elegida MVP** |
| Behavior Tree + Utility | Legible para diseñador | Integración por motor | Posible por adaptador |
| GOAP multiobjetivo | Planificación flexible | Complejidad y depuración | Posponer |
| LLM por NPC en vivo | Lenguaje y flexibilidad | Latencia, costo, incertidumbre | No usar en combate |
| RL/imitación | Potencial aprendizaje | Datos, recompensa, tiempo y riesgo de sobreajuste | Investigar tras baseline |

## Modelo de esfuerzo

Suponiendo una persona a 15–20 horas/semana por 20 semanas: **300–400 h** disponibles. Si solo hay 8 h/semana, reducir el proyecto a 160 h de alcance: baseline, percepción, memoria, un rol táctico y telemetría; agente y playtest quedarían para después. El cronograma no equivale a compromiso contractual.

## Gastos directos orientativos (sin salarios)

| Concepto | Estimación inicial | Comentarios |
|---|---:|---|
| Godot y código abierto | 0 USD de licencia | Revisar dependencias incorporadas |
| Repositorio privado/público básico | 0 USD posible | Según proveedor y límites |
| Arte provisional | 0–100 USD | Assets compatibles o primitivas |
| Dominio y hosting opcional | 0–100 USD/año | No necesario MVP local |
| API LLM opcional para agente | 0–100+ USD | Depende del uso; se puede desactivar |
| Pruebas con usuarios | 0–200 USD | Incentivos/traslados, si aplica |
| Hardware | No cuantificado | Utilizar equipo propio y documentarlo |

Las cifras son **bandas ilustrativas**, no precios de mercado verificados ni cotizaciones. Excluyen trabajo humano, impuestos, mantenimiento y contingencia. Un presupuesto formal requiere precios locales y lista real de proveedores.

## Costo de oportunidad

Si el trabajo se valora a una tarifa interna `r` USD/h y se dedican `h` horas, el costo de esfuerzo es `r × h`. Ejemplo **hipotético**: a 20 USD/h durante 350 horas = 7.000 USD, además de gastos directos. No se afirma que exista contratación ni esa tarifa sea una referencia vigente.

## Riesgos de ejecución y decisión de inversión

La inversión se divide en tramos para no construir el agente de modding antes de demostrar que el núcleo de IA genera valor. Gates recomendados: W2 (baseline), W6 (decisión estable), W12 (tácticas y aliado), W14 (métricas), W18 (validación), W20 (release). Si H1/H2 no se sostienen, rediseñar el sistema de comportamiento antes de ampliar motor/es.

## Retorno potencial, no validado

Podría reducir tiempo de prototipado de NPC y mejorar iteraciones de diseño si ofrece documentación, perfiles y pruebas reutilizables. Se requiere medir tiempo de integración frente a desarrollo manual en al menos dos proyectos de prueba antes de alegar productividad. No se presenta proyección de ingresos ni afirmación de demanda validada.
