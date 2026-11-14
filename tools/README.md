# Herramientas de Ingeniería y Benchmarking

- **`agent/`**:
  - Prototipo del agente de ingeniería local en Python (inspirado en la metodología de [Co-op Anything](https://github.com/cranebeaker/coop-anything)).
  - Implementa herramientas de inspección estricta de solo lectura (estilo MCP):
    - `inspect_project`
    - `detect_engine`
    - `find_ai_entrypoints`
    - `list_behaviors`
    - `assess_modifiability`
  - Generación de recomendaciones y diffs seguros en `proposals/` sin tocar archivos originales del usuario sin aprobación.

- **`bench/`**:
  - Runners de simulación, comparador de métricas JSONL y generador de informes de rendimiento (latencia de decisión ms, dispersión espacial, justicia percibida).
