"""
tools/agent/reporter.py
Exportador de reportes estructurados de auditoría y diagnóstico (BK-033, RF-15, RF-17).

Genera reportes en formatos JSON y Markdown distinguiendo estrictamente los niveles
de evidencia: VERIFIED, INFERRED y UNKNOWN.
"""

from __future__ import annotations

import datetime
import json
from pathlib import Path
from typing import Any, Dict, Optional, Tuple

from workspace_guard import WorkspaceGuard


def generate_markdown_report(inventory: Dict[str, Any], proposal_plan: Optional[str] = None) -> str:
    """Genera un informe Markdown técnico y formal con evidencias relativas y hashes."""
    engine = inventory.get("engine_detection", {})
    mod = inventory.get("modifiability_assessment", {})
    entrypoints = inventory.get("ai_entrypoints", [])
    behaviors = inventory.get("identified_behaviors", [])
    extensions = inventory.get("file_extensions", {})

    now_iso = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")

    lines = [
        "# Informe de Diagnóstico e Inventario Técnico de IA",
        "",
        f"> **Fecha de análisis:** {now_iso}  ",
        f"> **Entorno de ejecución:** {inventory.get('project_root', '.')}  ",
        f"> **Estado general:** Evaluado mediante agente de ingeniería local en modo lectura estricta.  ",
        "",
        "## 1. Detección de Tecnología y Motor",
        "",
        f"- **Motor identificado:** `{engine.get('engine', 'Unknown')}` ({engine.get('variant', 'N/A')})",
        f"- **Versión estimada:** `{engine.get('version_estimate', 'N/A')}`",
        f"- **Certeza / Confianza:** `{engine.get('confidence', 0.0) * 100:.0f}%`",
        f"- **Nivel de evidencia:** `[{engine.get('evidence_level', 'UNKNOWN')}]`",
        "",
        "### Evidencias de Detección:",
    ]

    for ev in engine.get("evidence", []):
        lines.append(f"- Archivo: `{ev.get('path')}` (SHA-256: `{ev.get('sha256')[:16]}...`) — {ev.get('note')}")

    lines.extend([
        "",
        "## 2. Inventario de Archivos y Código",
        "",
        f"- **Total de archivos inspeccionados:** {inventory.get('total_files', 0)}",
        "- **Distribución por extensión:**",
    ])

    for ext, count in sorted(extensions.items(), key=lambda x: x[1], reverse=True)[:10]:
        lines.append(f"  - `{ext}`: {count} archivos")

    lines.extend([
        "",
        "## 3. Puntos de Extensión y Control de IA (AI Entrypoints)",
        "",
        "| Archivo / Componente | Rol en IA | Símbolos Detectados | Nivel de Evidencia |",
        "|---|---|---|:---:|",
    ])

    if entrypoints:
        for ep in entrypoints[:15]:
            symbols_str = ", ".join(f"`{s}`" for s in ep.get("matched_symbols", [])[:3])
            lines.append(f"| `{ep.get('path')}` | {ep.get('role_in_ai')} | {symbols_str} | `[{ep.get('evidence_level')}]` |")
    else:
        lines.append("| *(No se detectaron entrypoints de IA explícitos)* | - | - | `[UNKNOWN]` |")

    lines.extend([
        "",
        "## 4. Acciones Tácticas y Comportamientos Reconocidos",
        "",
        "| Acción | Descripción | Implementado en | Nivel |",
        "|---|---|---|:---:|",
    ])

    if behaviors:
        for b in behaviors:
            lines.append(f"| **{b.get('action')}** | {b.get('description')} | `{b.get('implemented_in')}` | `[{b.get('evidence_level')}]` |")
    else:
        lines.append("| *(No se detectaron comportamientos catalogados)* | - | - | `[UNKNOWN]` |")

    lines.extend([
        "",
        "## 5. Evaluación de Modificabilidad y Acoplamiento",
        "",
        f"- **Estilo de Arquitectura:** `{mod.get('architecture_style')}` `[{mod.get('evidence_level')}]`",
        f"- **Aislamiento del Núcleo (Core):** {'Sí (Sin dependencias de motor)' if mod.get('core_engine_isolated') else 'No'}",
        f"- **Puertos y Adaptadores Detectados:** {'Sí' if mod.get('ports_and_adapters_detected') else 'No'}",
        f"- **Suites de Pruebas Automatizadas:** {'Disponibles' if mod.get('automated_tests_available') else 'No detectadas'}",
        f"- **Viabilidad de Integración:** **{mod.get('integration_viability')}**",
        f"- **Enfoque Recomendado:** {mod.get('recommended_approach')}",
        "",
        "## 6. Propuesta de Integración Reversible",
        "",
        proposal_plan or (
            "1. Mantener `NpcAnything.Core` desacoplado sin cambios.\n"
            "2. Probar mediante el fixture de transición headless `Arena_TransitionTest.tscn`.\n"
            "3. No escribir ni sobreescribir archivos fuera de `proposals/` sin aprobación humana."
        ),
        "",
        "---",
        "*Reporte generado automáticamente por NPC Anything Engineering Agent.*",
    ])

    return "\n".join(lines)


def export_report(
    inventory: Dict[str, Any],
    output_dir: Path,
    guard: WorkspaceGuard,
    filename_prefix: str = "project_audit",
    proposal_plan: Optional[str] = None,
) -> Tuple[Path, Path]:
    """
    Exporta el reporte en formatos JSON y Markdown dentro del sandbox de escritura autorizado.
    """
    safe_output_dir = guard.validate_write_path(output_dir)
    safe_output_dir.mkdir(parents=True, exist_ok=True)

    json_path = safe_output_dir / f"{filename_prefix}.json"
    md_path = safe_output_dir / f"{filename_prefix}.md"

    # 1. Guardar reporte JSON estructurado
    full_payload = {
        "schema_version": "1.0",
        "generated_at_utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "inventory": inventory,
        "proposal_plan": proposal_plan,
    }
    json_path.write_text(json.dumps(full_payload, indent=2, ensure_ascii=False), encoding="utf-8")

    # 2. Guardar reporte Markdown
    markdown_content = generate_markdown_report(inventory, proposal_plan)
    md_path.write_text(markdown_content, encoding="utf-8")

    return json_path, md_path
