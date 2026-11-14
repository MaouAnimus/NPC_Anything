# Suite de Pruebas y Validación (QA)

Estructura de validación definida en `docs/08_VALIDACION_Y_QA.md`:

- **`Core.Tests/`**:
  - Pruebas unitarias sobre `NpcAnything.Core`.
  - Validación de modelos deterministas, caducidad de memoria (TTL), atenuación de confianza, selección de utilidad pura, evitación de división por cero y fallback seguro cuando no hay acciones válidas.
  - No requiere motor gráfico para ejecutarse.

- **`Integration/`**:
  - Pruebas de integración de adaptadores y validación de escenarios reproducibles (`scenarios/` con semillas fijas).
  - Verificación de no-omnisciencia (comprobar que ningún dato fuera de la línea de visión o audición llega al planificador).
