"""
tools/agent/inspector.py
Módulo de inspección estricta de proyectos y detección de puntos de extensión de IA (BK-030, BK-031).

Herramientas:
- inspect_project: Inventario de archivos, tecnologías y estructura.
- detect_engine: Detección de motor (Godot, Unity, Custom) con evidencias y versión estimada.
- find_ai_entrypoints: Localización de controladores, sensores, motores y máquinas de estados.
- list_behaviors: Extracción de acciones y comportamientos identificados en scripts.
- assess_modifiability: Evaluación de acoplamiento, contratos y viabilidad de integración.
"""

from __future__ import annotations

import hashlib
import os
from pathlib import Path
import re
from typing import Any, Dict, List, Optional

from workspace_guard import WorkspaceGuard


def compute_file_sha256(path: Path) -> str:
    """Calcula el hash SHA-256 de un archivo para evidencia verificable."""
    sha = hashlib.sha256()
    with open(path, "rb") as f:
        while chunk := f.read(65536):
            sha.update(chunk)
    return sha.hexdigest()


def detect_engine(root: Path, guard: WorkspaceGuard) -> Dict[str, Any]:
    """
    Detecta el motor utilizado en el directorio raíz mediante evidencias observables en disco.
    Retorna motor, versión estimada, nivel de certeza y evidencias.
    """
    safe_root = guard.validate_read_path(root)

    # 1. Búsqueda de proyecto Godot
    godot_project_file = safe_root / "project.godot"
    if not godot_project_file.exists():
        # Buscar en subcarpetas de primer nivel
        for sub in safe_root.iterdir():
            if sub.is_dir() and (sub / "project.godot").exists():
                godot_project_file = sub / "project.godot"
                break

    if godot_project_file.exists():
        content = godot_project_file.read_text(encoding="utf-8", errors="replace")
        version_estimate = "4.x"
        if "config/features=PackedStringArray(\"4." in content:
            m = re.search(r"\"(4\.[0-9]+)\"", content)
            if m:
                version_estimate = m.group(1)

        is_csharp = any(safe_root.glob("**/*.csproj"))
        tech_variant = "Godot (.NET / C#)" if is_csharp else "Godot (GDScript)"

        return {
            "engine": "Godot",
            "variant": tech_variant,
            "version_estimate": version_estimate,
            "evidence_level": "VERIFIED",
            "confidence": 1.0,
            "evidence": [
                {
                    "path": str(godot_project_file.relative_to(safe_root)),
                    "sha256": compute_file_sha256(godot_project_file),
                    "note": f"project.godot detected with features: {version_estimate}",
                }
            ],
        }

    # 2. Búsqueda de proyecto Unity
    unity_version_file = safe_root / "ProjectSettings" / "ProjectVersion.txt"
    if unity_version_file.exists():
        content = unity_version_file.read_text(encoding="utf-8", errors="replace")
        m = re.search(r"m_EditorVersion:\s*([^\s]+)", content)
        ver = m.group(1) if m else "Unknown"
        return {
            "engine": "Unity",
            "variant": "Unity C#",
            "version_estimate": ver,
            "evidence_level": "VERIFIED",
            "confidence": 1.0,
            "evidence": [
                {
                    "path": str(unity_version_file.relative_to(safe_root)),
                    "sha256": compute_file_sha256(unity_version_file),
                    "note": f"ProjectVersion.txt detected ({ver})",
                }
            ],
        }

    # 3. Fallback: Desconocido o biblioteca pura C#/.NET
    csharp_projects = list(safe_root.glob("**/*.csproj"))
    if csharp_projects:
        return {
            "engine": "Standalone .NET / C#",
            "variant": ".NET 8.0+",
            "version_estimate": "8.0",
            "evidence_level": "VERIFIED",
            "confidence": 0.9,
            "evidence": [
                {
                    "path": str(csharp_projects[0].relative_to(safe_root)),
                    "sha256": compute_file_sha256(csharp_projects[0]),
                    "note": "C# project without direct engine configuration",
                }
            ],
        }

    return {
        "engine": "Unknown",
        "variant": "Generic",
        "version_estimate": None,
        "evidence_level": "UNKNOWN",
        "confidence": 0.0,
        "evidence": [],
    }


def find_ai_entrypoints(root: Path, guard: WorkspaceGuard) -> List[Dict[str, Any]]:
    """
    Localiza clases, componentes y escenas donde se gestiona la toma de decisiones o control de NPC.
    """
    safe_root = guard.validate_read_path(root)
    entrypoints = []

    ai_keywords = [
        "NpcBrainController",
        "NpcMotorController",
        "CharacterBody3D",
        "RayCast3D",
        "NavigationAgent3D",
        "StateMachine",
        "UtilitySelector",
        "ActionCandidateGenerator",
        "MemoryStore",
        "SquadMessageBus",
    ]

    for file_path in safe_root.rglob("*"):
        if not file_path.is_file():
            continue
        if file_path.suffix.lower() not in (".cs", ".gd", ".tscn", ".py"):
            continue

        try:
            content = file_path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue

        matched_keywords = [kw for kw in ai_keywords if kw in content]
        if matched_keywords:
            rel_path = str(file_path.relative_to(safe_root))
            entrypoints.append({
                "path": rel_path,
                "sha256": compute_file_sha256(file_path),
                "matched_symbols": matched_keywords,
                "role_in_ai": "Decision/Brain" if "Brain" in content or "Utility" in content else "Motor/Perception",
                "evidence_level": "VERIFIED",
            })

    return entrypoints


def list_behaviors(root: Path, guard: WorkspaceGuard) -> List[Dict[str, Any]]:
    """
    Extrae las acciones tácticas y estados operativos implementados en los scripts del proyecto.
    """
    safe_root = guard.validate_read_path(root)
    behaviors = []
    recognized_actions = {
        "Patrol": "Patrulla activa entre puntos o waypoints",
        "Pursue": "Persecución frontal con sprint y aproximación táctica",
        "Attack": "Disparo directo bajo línea de visión confirmada",
        "TakeCover": "Búsqueda y reserva de posición protegida ante amenaza",
        "Flank": "Desplazamiento a ángulos laterales (30°-120°) respecto al objetivo",
        "Investigate": "Inspección de pistas acústicas o última posición conocida",
        "Follow": "Seguimiento y escolta cercana sin bloqueo de retícula",
        "Hold": "Mantener posición en guardia o cobertura",
    }

    found_actions = set()

    for file_path in safe_root.rglob("*.cs"):
        try:
            content = file_path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue

        for action_name, description in recognized_actions.items():
            if f"ActionKind.{action_name}" in content or f"\"{action_name}\"" in content:
                if action_name not in found_actions:
                    found_actions.add(action_name)
                    behaviors.append({
                        "action": action_name,
                        "description": description,
                        "implemented_in": str(file_path.relative_to(safe_root)),
                        "evidence_level": "VERIFIED",
                    })

    return behaviors


def assess_modifiability(root: Path, guard: WorkspaceGuard) -> Dict[str, Any]:
    """
    Evalúa la modificabilidad arquitectónica y desacoplamiento para integrar NpcAnything.Core.
    """
    safe_root = guard.validate_read_path(root)
    has_ports = any(safe_root.glob("**/Ports/*.cs")) or any(safe_root.glob("**/*Port.cs"))
    has_isolated_core = (safe_root / "src" / "NpcAnything.Core").exists()
    has_tests = any(safe_root.glob("tests/**/*.cs")) or any(safe_root.glob("tools/bench/*.py"))

    coupling_status = "Decoupled (Clean Hexagonal Architecture)" if has_ports and has_isolated_core else "Tight Engine Coupling"

    return {
        "architecture_style": coupling_status,
        "ports_and_adapters_detected": has_ports,
        "core_engine_isolated": has_isolated_core,
        "automated_tests_available": has_tests,
        "integration_viability": "HIGH" if (has_ports and has_isolated_core) else "MEDIUM",
        "recommended_approach": "Conectar NpcBrainController mediante los puertos IWorldSensorPort, ISpatialQueryPort e IActionExecutorPort sin modificar NpcAnything.Core.",
        "evidence_level": "VERIFIED" if has_ports else "INFERRED",
    }


def inspect_project(root: Path, guard: WorkspaceGuard) -> Dict[str, Any]:
    """
    Realiza el inventario integral de solo lectura de un proyecto autorizado (BK-030).
    """
    safe_root = guard.validate_read_path(root)
    engine_info = detect_engine(safe_root, guard)
    ai_entrypoints = find_ai_entrypoints(safe_root, guard)
    behaviors = list_behaviors(safe_root, guard)
    modifiability = assess_modifiability(safe_root, guard)

    extensions_count: Dict[str, int] = {}
    total_files = 0

    for file_path in safe_root.rglob("*"):
        if file_path.is_file():
            total_files += 1
            ext = file_path.suffix.lower() or "(no-ext)"
            extensions_count[ext] = extensions_count.get(ext, 0) + 1

    return {
        "project_root": WorkspaceGuard.sanitize_path_string(str(safe_root)),
        "total_files": total_files,
        "file_extensions": extensions_count,
        "engine_detection": engine_info,
        "ai_entrypoints": ai_entrypoints,
        "identified_behaviors": behaviors,
        "modifiability_assessment": modifiability,
    }
