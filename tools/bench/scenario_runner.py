#!/usr/bin/env python3
"""
tools/bench/scenario_runner.py
Ejecutor de escenarios experimentales y suites de integración para GodotArena.

Permite ejecutar Godot en modo --headless con semillas controladas,
capturar telemetría JSONL en runs/ y validar aserciones de integración.
"""

import argparse
import os
import shutil
import subprocess
import sys
import time
from pathlib import Path
from typing import List, Optional


def find_godot_binary(explicit_path: Optional[str] = None) -> Optional[Path]:
    """Localiza el binario de Godot (preferentemente versión .NET con soporte C#)."""
    if explicit_path:
        p = Path(explicit_path)
        if p.exists() and p.is_file():
            return p

    env_path = os.environ.get("GODOT_BIN")
    if env_path:
        p = Path(env_path)
        if p.exists() and p.is_file():
            return p

    in_path = shutil.which("godot") or shutil.which("godot-mono")
    if in_path:
        return Path(in_path)

    # Búsqueda en rutas estándar comunes
    candidates = [
        Path(os.path.expanduser("~")) / "Downloads" / "Godot_v4.3-stable_mono_win64" / "Godot_v4.3-stable_mono_win64_console.exe",
        Path(os.path.expanduser("~")) / "Downloads" / "Godot_v4.2.2-stable_mono_win64" / "Godot_v4.2.2-stable_mono_win64_console.exe",
        Path("C:/Program Files/Godot/godot.exe"),
    ]
    for c in candidates:
        if c.exists() and c.is_file():
            return c

    return None


def run_godot_scene(
    godot_bin: Path,
    project_path: Path,
    scene_path: str,
    duration_seconds: int = 15,
    seed_value: int = 42,
    extra_args: Optional[List[str]] = None,
) -> int:
    """Ejecuta una escena de Godot en modo headless durante una ventana de tiempo controlada."""
    cmd = [
        str(godot_bin),
        "--headless",
        "--path",
        str(project_path),
        "--scene",
        scene_path,
    ]

    if extra_args:
        cmd.extend(extra_args)

    print(f"[RUNNER] Ejecutando: {' '.join(cmd)}")
    print(f"[RUNNER] Semilla: {seed_value} | Tiempo límite: {duration_seconds}s")

    env = os.environ.copy()
    env["NPC_ANYTHING_SEED"] = str(seed_value)

    start_time = time.time()
    proc = subprocess.Popen(
        cmd,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        env=env,
    )

    try:
        stdout, stderr = proc.communicate(timeout=duration_seconds)
        exit_code = proc.returncode
    except subprocess.TimeoutExpired:
        print(f"[RUNNER] Tiempo límite de {duration_seconds}s alcanzado. Finalizando proceso.")
        proc.kill()
        stdout, stderr = proc.communicate()
        exit_code = 0  # Cierre por timeout controlado

    elapsed = round(time.time() - start_time, 2)
    print(f"[RUNNER] Ejecución finalizada en {elapsed}s (Exit code: {exit_code})")

    if stdout:
        print("--- Salida de Godot ---")
        for line in stdout.strip().splitlines()[-20:]:  # Últimas 20 líneas
            print(f"  {line}")

    if stderr:
        print("--- Errores reportados ---", file=sys.stderr)
        for line in stderr.strip().splitlines()[-10:]:
            print(f"  {line}", file=sys.stderr)

    return exit_code


def find_latest_run_log(runs_dir: Path) -> Optional[Path]:
    """Encuentra el archivo .jsonl más reciente en runs/."""
    if not runs_dir.exists():
        return None
    files = list(runs_dir.glob("*.jsonl"))
    if not files:
        return None
    files.sort(key=lambda f: f.stat().st_mtime, reverse=True)
    return files[0]


def main():
    parser = argparse.ArgumentParser(description="Ejecutor de escenarios empíricos en Godot.")
    parser.add_argument("--scene", default="res://scenes/Arena_NpcAnything.tscn", help="Escena a ejecutar")
    parser.add_argument("--seed", type=int, default=42, help="Semilla aleatoria")
    parser.add_argument("--duration", type=int, default=15, help="Duración máxima en segundos")
    parser.add_argument("--godot-bin", default=None, help="Ruta explícita al binario de Godot")
    parser.add_argument("--compare", action="store_true", help="Ejecuta tanto Baseline como NpcAnything y compara")

    args = parser.parse_args()

    project_root = Path(__file__).resolve().parents[2]
    godot_project_path = project_root / "demo" / "GodotArena"
    runs_dir = project_root / "runs"

    godot_bin = find_godot_binary(args.godot_bin)
    if not godot_bin:
        print("[ERROR] No se encontró el binario de Godot .NET.", file=sys.stderr)
        print("Indica la ruta mediante --godot-bin <path> o la variable de entorno GODOT_BIN.", file=sys.stderr)
        sys.exit(1)

    print(f"[RUNNER] Binario de Godot detectado: {godot_bin}")

    if args.compare:
        print("\n=== 1. Ejecutando Variante A (Baseline FSM) ===")
        run_godot_scene(
            godot_bin,
            godot_project_path,
            "res://scenes/Arena_Baseline.tscn",
            duration_seconds=args.duration,
            seed_value=args.seed,
        )
        log_baseline = find_latest_run_log(runs_dir)

        print("\n=== 2. Ejecutando Variante B (NpcAnything Core) ===")
        time.sleep(1)  # Separar timestamp de corrida
        run_godot_scene(
            godot_bin,
            godot_project_path,
            "res://scenes/Arena_NpcAnything.tscn",
            duration_seconds=args.duration,
            seed_value=args.seed,
        )
        log_core = find_latest_run_log(runs_dir)

        if log_baseline and log_core:
            print("\n=== 3. Comparación de Telemetría ===")
            compare_script = project_root / "tools" / "bench" / "compare_telemetry.py"
            subprocess.run([
                sys.executable,
                str(compare_script),
                str(log_baseline),
                str(log_core),
                "--label-a", "Baseline FSM",
                "--label-b", "NpcAnything Core",
            ])
    else:
        run_godot_scene(
            godot_bin,
            godot_project_path,
            args.scene,
            duration_seconds=args.duration,
            seed_value=args.seed,
        )
        latest_log = find_latest_run_log(runs_dir)
        if latest_log:
            print(f"[RUNNER] Último registro de telemetría: {latest_log}")


if __name__ == "__main__":
    main()
