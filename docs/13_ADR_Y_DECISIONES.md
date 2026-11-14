# 13. Registro de decisiones de arquitectura (ADR) y gobernanza

## ADR-001 — Motor inicial

**Estado:** propuesto.  
**Decisión:** usar Godot 4.x estable con C#/.NET para demo; fijar versión exacta en primera semana.  
**Justificación:** prototipo abierto y adaptable, navegación integrada y núcleo C# reutilizable conceptualmente en Unity.  
**Consecuencias:** integración Godot .NET, pruebas específicas de versión, Unreal requerirá portabilidad adicional.  
**Alternativas:** Unity-first, Unreal-first, Godot GDScript.  
**Revisión:** al cerrar W2.

## ADR-002 — Separación runtime/agente

**Estado:** propuesto.  
**Decisión:** agente de ingeniería fuera del bucle de combate; runtime ejecuta decisiones locales deterministas.  
**Justificación:** latencia, previsibilidad, coste y depuración.  
**Consecuencias:** el agente produce integración y pruebas, no comandos de cada fotograma.  
**Revisión:** antes de evaluar LLM runtime.

## ADR-003 — Utility AI sobre acciones elegibles

**Estado:** propuesto.  
**Decisión:** FSM de alto nivel + Utility AI con reglas duras, histéresis y cooldowns.  
**Justificación:** equilibrio entre flexibilidad y explicabilidad.  
**Consecuencias:** se requiere ajustar funciones y registrar puntuaciones.  
**Revisión:** después de G4.

## ADR-004 — Justicia como restricción

**Estado:** propuesto.  
**Decisión:** planificador solo consume percepción, memoria y mensajes autorizados.  
**Justificación:** evita «wallhack» e información instantánea.  
**Consecuencias:** interfaces de observación explícitas y test de conocimiento.  
**Revisión:** continua.

## ADR-005 — Cero modificación directa de instalaciones

**Estado:** propuesto.  
**Decisión:** cualquier análisis o parche futuro se ejecuta en workspace de prueba; escrituras exigen aprobación.  
**Justificación:** integridad de saves, permisos, licencias y seguridad.  
**Consecuencias:** overhead de copias, manifest y rollback.  
**Revisión:** previo a herramientas N2.

## Proceso de cambio

Abrir issue RFC con problema, evidencia, alternativas, impacto en RF/RNF y riesgos; proponer ADR si afecta arquitectura o política; obtener revisión técnica; implementar con pruebas y documentación; no reescribir decisiones pasadas: marcar reemplazo con nuevo ADR. Las decisiones sin evidencia se marcan **provisionales**.

## Política de ramas

`main` protegida; ramas `feat/BK-###-descripcion`, `fix/BK-###-descripcion`, `docs/...`; PR con contexto, requisitos, pruebas ejecutadas, registros de rendimiento si aplica y checklist de privacidad. No mezclar cambios masivos de comportamiento con cambios de escena experimental en el mismo PR.

## Matriz de definiciones

- **Hecho:** observado en test o repositorio con evidencia.
- **Hipótesis:** resultado deseado por validar.
- **Decisión:** elección acordada, con motivo y tradeoffs.
- **Desconocido:** falta de datos, no sustituir con suposición.
- **No aplicable:** fuera de alcance por decisión explícita.

## Revisión de calidad documental

Cada documento debe llevar versión, fecha y responsable cuando se active el repo. Los enlaces se verifican antes de release. Las métricas se actualizan solo con resultados instrumentados. Cambios de requisitos se reflejan en backlog y matriz de pruebas.
