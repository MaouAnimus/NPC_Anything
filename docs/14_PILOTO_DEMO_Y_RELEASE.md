# 14. Plan de demostración, piloto y publicación

## Demo que debe mostrar el MVP

Duración recomendada 5–8 minutos. Usar un mismo mapa, misma arma, mismo daño, misma salud y semilla reproducible. 1) Presentar baseline detecta/ataca. 2) Mostrar oclusión: el enemigo pierde visión y busca una ubicación pasada. 3) Mostrar roles: asalto presiona mientras flanqueador busca ruta. 4) Mostrar mensaje de escuadrón e interrupción de canal. 5) Mostrar compañero asistiendo sin bloquear. 6) Cambiar dificultad sin modificar estadísticas. 7) Abrir overlay y logs para explicar una decisión.

## Evidencia mínima para cada capacidad

| Capacidad | Evidencia cuantitativa | Evidencia cualitativa |
|---|---|---|
| Percepción justa | Tests SC-01/02/03, 0 eventos de información prohibida | Jugador reconoce cuándo fue detectado |
| Cobertura | Coberturas válidas, fallos y recuperación | Movimiento se ve intencional |
| Coordinación | Mensajes enviados/recibidos por canal | Maniobra legible |
| Compañero | Bloqueos de ruta y avisos útiles | No roba protagonismo |
| Dificultad | Comparativa de pesos/retardos con stats fijas | Reto razonable y diferente |
| Herramientas | Informe con evidencia de proyecto demo | Desarrollador comprende integración |

## Piloto de usuario

Reclutar 8–12 personas para evaluación exploratoria. Mezclar jugadores acostumbrados a shooters y personas con menor experiencia si el género elegido lo permite. Definir consentimiento, anonimizar logs y no recoger datos sensibles. Presentar A/B en orden alternado; usar sesiones cortas y mismo tutorial. Documentar diferencias de habilidad para interpretar resultados, sin sobregeneralizar.

## Release checklist

- [ ] Build de Godot abre en un entorno nuevo siguiendo instrucciones.
- [ ] Se registra versión exacta, commit, sistema operativo y configuración.
- [ ] CI y todos los tests críticos pasan o quedan fallos explícitos bloqueantes.
- [ ] SC-01 a SC-12 tienen evidencia o explicación de no ejecución.
- [ ] No existen eventos de «omnisciencia» conocidos sin resolver.
- [ ] No se distribuyen assets, saves, claves o juegos de terceros.
- [ ] Se han auditado dependencias y avisos de licencia.
- [ ] Existe método de desactivar el SDK y restaurar baseline.
- [ ] Reporte de rendimiento incluye hardware y percentiles.
- [ ] Manual de demo, arquitectura y riesgos están actualizados.
- [ ] Bugs críticos cerrados o release cancelada.

## Informe final de la primera versión

Estructura: resumen ejecutivo, alcance real vs planeado, build y entorno, metodología de medición, tabla de resultados por escenario, pruebas con usuarios, costes y horas reales, limitaciones, decisiones técnicas, riesgos residuales y recomendación v0.2. Usar expresiones «medido», «observado», «estimado», «propuesto» de manera consistente.

## Decisión de siguiente etapa

- **Continuar como SDK:** si la demo demuestra valor y adaptador Godot es estable.
- **Desarrollar modding agent:** si N0 demuestra diagnóstico útil y hay juego abierto/moddable elegible.
- **Investigar RL o GOAP:** solo si existe un cuello de botella de decisiones demostrable.
- **Pivotar o detener:** si los jugadores perciben el sistema como menos justo o si rendimiento/integración son inviables dentro de recursos disponibles.
