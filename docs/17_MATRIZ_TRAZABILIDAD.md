# 17. Matriz de trazabilidad (requisitos -> backlog -> pruebas)

| Requisito | Backlog vinculado | Prueba / evidencia |
|---|---|---|
| RF-01 Visión | BK-006, BK-011, BK-028 | SC-01, SC-02 |
| RF-02 Sonido | BK-007 | SC-03 |
| RF-03 Memoria | BK-008 | SC-02 |
| RF-04 Utility AI | BK-009, BK-010 | Unit test selector, SC-06 |
| RF-05 Acciones por contrato | BK-011, BK-012, BK-015 | SC-11 |
| RF-06 Cobertura | BK-013, BK-014 | SC-04, SC-05 |
| RF-07 Roles | BK-016–019 | SC-06, SC-08 |
| RF-08 Comunicación | BK-020, BK-021 | SC-07 |
| RF-09 Retirada | BK-015, BK-023 | SC-08; pendiente caso dedicado |
| RF-10 Compañero | BK-022 | SC-09 |
| RF-11 Dificultad | BK-023 | SC-10 |
| RF-12 Telemetría | BK-024 | Auditoría JSONL |
| RF-13 Determinismo | BK-025 | Runner 30 seeds |
| RF-14 Debug overlay | BK-027 | Demostración visual |
| RF-15 Análisis de proyecto | BK-030, BK-031 | Informe sobre demo propia |
| RF-16 Diffs revisables | BK-041 (P2) | Fuera de MVP; test posterior |
| RF-17 Reportes | BK-026 | Reporte SC-01 a SC-12 |
| RF-18 Perfil recargable | BK-023 | Test de configuración |
| RNF-01 Rendimiento | BK-029 | SC-12 |
| RNF-04 Observabilidad | BK-024 | Verificación reason code |
| RNF-05 Seguridad | BK-032 | Escape de workspace |
| RNF-09 Fairness | BK-028 | SC-01, SC-07, SC-10 |

**Huecos identificados:** crear pruebas dedicadas para retirada (RF-09), recuperación de configuración inválida (RNF-08) y carga dinámica de perfiles (RF-18). La matriz debe evolucionar con el repositorio y distinguir pruebas diseñadas de ejecutadas.
