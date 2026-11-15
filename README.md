# NPC Anything

**Versión:** 0.3.0 (Hito 03-C: Persecución Táctica, Adquisición FOV y Determinismo)  
**Fecha:** 15 de noviembre de 2026  
**Estado:** Hito 03-C implementado. Acción táctica Pursue en Core, validación FOV pre-rotación con adquisición progresiva, resolución estricta de objetivos por ID sin fallback ciego, hash determinista FNV-1a para acústica inter-proceso, sincronización multi-frame con NavigationServer3D y paridad simétrica de patrulla.  

---

## Qué es

NPC Anything es una plataforma desacoplada para dotar a videojuegos de un comportamiento inteligente, justo y reproducible en enemigos y compañeros. Se divide estrictamente en dos planos:

1. **Plano de Ejecución (Runtime en C#):** Toma de decisiones con percepción acotada (cono de visión y audio), memoria con degradación temporal (TTL), evaluación de utilidad pura con histéresis, reglas duras de juego limpio (anti-wallhacks) y coordinación de escuadrón. **Cero llamadas a LLMs en el bucle de juego/frame**.
2. **Plano de Ingeniería (Fuera del juego):** Herramientas asistidas por IA para inspeccionar proyectos, evaluar viabilidad y proponer parches en entornos aislados (`proposals/`).

---

## Estructura del Repositorio

```text
NPC_Anything/
├── .gitignore                   # Exclusiones de .NET, Godot, Python y cachés
├── NpcAnything.sln              # Solución completa .NET 8 (Core, GodotAdapter, Tests, Demo)
├── README.md                    # Estado del proyecto e instrucciones
├── AGENTS.md                    # Reglas obligatorias para asistentes de ingeniería
├── docs/                        # Paquete completo de especificaciones y diseño
│   ├── 00_EMPEZAR_AQUI.md a 18_GANTT_ESTRUCTURA.md
│   ├── plantillas/              # Plantillas de ADR, casos de prueba, sprint y PR
│   └── dossier/                 # Dossier técnico oficial (PDF y DOCX)
├── examples/                    # Perfiles ilustrativos (YAML, JSON, CSV)
├── src/
│   ├── NpcAnything.Core/        # Núcleo puro C# sin dependencias de motor (Dominio, Puertos, Memoria, Utilidad, Táctica)
│   └── NpcAnything.Godot/       # Adaptador para Godot 4.x (Sensores RayCast/Audio, Navegación MapGetPath, Motor físico)
├── demo/
│   └── GodotArena/              # Proyecto Godot 4.3 .NET con escenas experimentales aisladas
│       ├── scenes/
│       │   ├── ArenaEnvironment.tscn   # Geometría, iluminación y NavigationMesh compartida
│       │   ├── Arena_Baseline.tscn     # Variante A: Enemigo con FSM tradicional de referencia
│       │   └── Arena_NpcAnything.tscn  # Variante B: Enemigo controlado por NpcAnything.Core
│       └── scripts/                    # Controladores de jugador, baseline y entorno
├── tests/
│   └── Core.Tests/              # Suite de pruebas unitarias xUnit (31 tests automatizados)
├── tools/
│   ├── agent/                   # Asistente de inspección local (MCP)
│   └── bench/                   # Comparadores y runners de telemetría JSONL
└── proposals/                   # Sandbox aislado para diffs sugeridos
```

---

## Compilación y Ejecución de Pruebas

### 1. Compilar la Solución Completa (.NET 8)
```powershell
dotnet build NpcAnything.sln
```

### 2. Ejecutar la Suite de Pruebas Unitarias
```powershell
dotnet test NpcAnything.sln
```

### 3. Ejecutar las Escenas en Godot Engine (.NET)
Para ejecutar las variantes experimentales de forma aislada:
```powershell
$Godot = "RUTA_AL_GODOT_4_3_DOTNET.exe"

# Ejecutar Variante A (Baseline FSM de referencia):
& $Godot --path .\demo\GodotArena --scene "res://scenes/Arena_Baseline.tscn"

# Ejecutar Variante B (IA Avanzada NpcAnything.Core):
& $Godot --path .\demo\GodotArena --scene "res://scenes/Arena_NpcAnything.tscn"
```

---

## Principios Técnicos y Reglas de Oro

- **No Omnisciencia:** Ningún planificador recibe coordenadas que no hayan sido observadas físicamente.
- **Validación de Rutas:** Las coberturas se verifican mediante `NavigationServer3D.MapGetPath()`; no se seleccionan puntos desconectados.
- **Anti-Wallhack:** Los ataques físicos exigen comprobación de línea de visión directa en el instante de disparo; el sonido solo habilita investigación o aproximación.
- **Paridad Experimental:** El Baseline y el NPC Core comparten idénticas estadísticas de vida, daño, velocidad física y conos de percepción.
