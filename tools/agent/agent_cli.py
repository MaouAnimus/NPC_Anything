#!/usr/bin/env python3
"""
tools/agent/agent_cli.py
Punto de entrada de línea de comandos para el agente de ingeniería local (EP-05, BK-030-BK-033).
"""

from __future__ import annotations

import argparse
from pathlib import Path
import sys

from inspector import inspect_project
from reporter import export_report, generate_markdown_report
from workspace_guard import WorkspaceGuard

REPO_ROOT = Path(__file__).resolve().parents[2]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Agente local de ingeniería para inspección y auditoría de IA.")
    parser.add_argument(
        "--workspace-dir",
        type=Path,
        default=REPO_ROOT,
        help="Directorio del proyecto a inspeccionar (debe estar en allowlist)",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=REPO_ROOT / "proposals",
        help="Directorio seguro de salida para propuestas y reportes",
    )
    parser.add_argument(
        "--prefix",
        default="project_audit",
        help="Prefijo para los archivos generados (.json y .md)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Imprime el informe en consola sin escribir en disco",
    )

    args = parser.parse_args(argv)

    try:
        guard = WorkspaceGuard(
            allowed_read_roots=[args.workspace_dir, REPO_ROOT],
            allowed_write_root=args.output_dir,
        )

        print(f"[AGENT] Inspeccionando proyecto en: {args.workspace_dir}")
        inventory = inspect_project(args.workspace_dir, guard)

        engine_name = inventory.get("engine_detection", {}).get("engine", "Unknown")
        print(f"[AGENT] Motor detectado: {engine_name} [{inventory.get('engine_detection', {}).get('evidence_level')}]")
        print(f"[AGENT] Total archivos analizados: {inventory.get('total_files')}")
        print(f"[AGENT] Entrypoints de IA localizados: {len(inventory.get('ai_entrypoints', []))}")
        print(f"[AGENT] Acciones tácticas catalogadas: {len(inventory.get('identified_behaviors', []))}")

        if args.dry_run:
            print("\n" + "=" * 60)
            print(generate_markdown_report(inventory))
            print("=" * 60)
            print("[AGENT] Dry-run finalizado exitosamente (sin escrituras en disco).")
            return 0

        json_out, md_out = export_report(inventory, args.output_dir, guard, filename_prefix=args.prefix)
        print(f"[AGENT] Reporte JSON exportado en: {json_out}")
        print(f"[AGENT] Reporte Markdown exportado en: {md_out}")
        return 0

    except PermissionError as perm_err:
        print(f"[SECURITY ERROR] {perm_err}", file=sys.stderr)
        return 2
    except Exception as exc:
        print(f"[AGENT ERROR] {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
