# 10. Estrategia de integración por motor y por juego

## Decisión inicial: Godot 4.x + C#

La primera demo usa Godot 4.x **estable** y C# para facilitar un núcleo independiente del motor que posteriormente podría adaptarse a Unity. La versión exacta debe fijarse en ADR-001 tras verificar compatibilidad de .NET y herramientas; esta documentación no presume una versión instalada ni aprobada. Godot ofrece recursos de navegación y agentes, pero su `NavigationAgent3D` facilita consultas de ruta y no mueve por sí mismo al personaje: la integración debe ejecutar movimiento, animaciones y control por separado.

## Adaptador Godot propuesto

- Recoger entradas de percepción mediante raycasts/FOV/eventos de ruido propios.
- Usar `NavigationAgent3D` y nodos o puntos de cobertura predefinidos para rutas; recalcular con presupuesto.
- Convertir `Vector3` y estado del motor a DTOs del Core.
- Recibir `ActionDecision` y ejecutar move/take cover/investigate/retreat/hold.
- Informar inicio, fin, fallo y bloqueo de cada acción.
- Integrar señales de UI y animaciones para intenciones.

## Integración Unity (fase posterior)

Un adaptador Unity C# podría aplicar los mismos contratos e integrar `NavMeshAgent`, física, animaciones y componentes sensoriales propios. Unity ML-Agents es una opción de investigación para entrenamiento por refuerzo e imitación, pero **no es requisito** para el MVP y no convierte por sí solo a todos los NPC en inteligentes. Elegir un paquete/versiones compatibles antes de implementar.

## Integración Unreal (posterior)

En Unreal existen Behavior Trees, AI Perception y EQS; una integración no trivial requeriría interfaz nativa o puente con C++/Blueprint, por lo que no se promete compartir binarios C# directamente. El camino razonable es reutilizar el **modelo conceptual y contratos** (o portar Core) y mapear sensores, acciones y consultas en un adaptador explícito.

## Adaptación a juegos existentes

Cada juego se trata como investigación separada. El trabajo comienza con **triage**: nombre, plataforma, versión/build, motor, arquitectura, mecanismos de modding, licencia, antitrampas, tipo de modo offline/online, hooks de IA y visibilidad de código. Luego elegir uno de cuatro caminos:

1. **Código fuente disponible:** integrar SDK y pruebas mediante cambios normales.
2. **API de scripting/modding:** adaptar contratos a los hooks oficialmente permitidos.
3. **Mod comunitario autorizado y mantenible:** proponer cambios revisables en rama/copia propia.
4. **Ejecutable cerrado sin vía clara:** registrar como no viable o investigar legalmente los límites; no prometer modificación.

## Matriz de viabilidad por juego

| Criterio | Baja dificultad | Media | Alta / descartar |
|---|---|---|---|
| Código de gameplay | Fuente disponible | Scripts accesibles | Solo binario opaco |
| Permiso de modificación | Licencia clara | Condiciones ambiguas | Prohibición expresa |
| Hooks de IA | Documentados | Parciales | Inexistentes |
| Pruebas locales | Automáticas | Manuales reproducibles | No se pueden ejecutar |
| Dependencias de red | Offline | Co-op con permiso | Online competitivo |
| Seguridad | Sin antitrampas | Restricciones | Requeriría bypass |

## Artefactos por integración

`game_profile.yaml` (sin datos privados); evaluación de permisos; inventario de módulos; mapa de clases de IA; diseño de adaptador; escenarios de regresión; nota de compatibilidad por build; manifest de archivos propios; pasos de instalar/desinstalar; riesgos pendientes. Usar plantilla `plantillas/FICHA_JUEGO.md`.

## Lo que NO hace el adaptador

No crea assets artísticos, no reemplaza un motor de navegación completo, no interpreta arbitrariamente animaciones ni modifica IA desconocida sin acceso a interfaces. No reutiliza información de partidas de otros usuarios ni se integra con sistemas competitivos protegidos.
