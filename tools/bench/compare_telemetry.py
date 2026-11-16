#!/usr/bin/env python3
"""
tools/bench/compare_telemetry.py
Analizador y comparador de telemetría JSONL entre Baseline FSM y NpcAnything.Core.

Métricas analizadas:
- Distribución de estados y acciones (Patrol, Pursue/Chase, Attack, Investigate/Search, TakeCover)
- Eficiencia y justicia de combate: disparos ejecutados vs bloqueados por oclusión o ángulo
- Entropía táctica (Shannon diversity) de transiciones de acción
- Detección estricta de violaciones de reglas de juego limpio (anti-omnisciencia / anti-wallhack)
"""

import argparse
import json
import math
import sys
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any, Dict, List, Optional


def load_jsonl(file_path: Path) -> List[Dict[str, Any]]:
    records = []
    with open(file_path, "r", encoding="utf-8") as f:
        for line_num, line in enumerate(f, 1):
            line = line.strip()
            if not line:
                continue
            try:
                records.append(json.loads(line))
            except json.JSONDecodeError as err:
                print(f"[WARN] Error parseando línea {line_num} en {file_path}: {err}", file=sys.stderr)
    return records


def analyze_run(records: List[Dict[str, Any]]) -> Dict[str, Any]:
    if not records:
        return {"error": "Registro vacío"}

    first_record = records[0]
    run_id = first_record.get("run_id", "unknown")
    scenario_id = first_record.get("scenario_id", "unknown")
    seed = first_record.get("seed", 0)

    max_sim_time = max(r.get("simulation_time_ms", 0) for r in records)

    action_counts = Counter()
    event_counts = Counter()
    blocked_reasons = Counter()
    total_attacks_executed = 0
    total_damage_dealt = 0.0
    wallhack_violations = 0

    transitions = []
    last_action = None

    for r in records:
        evt = r.get("event", "")
        action = r.get("action", "")
        reason = r.get("reason_code", "")
        score = float(r.get("score", 0.0))

        event_counts[evt] += 1

        if evt in ("action_changed", "state_changed"):
            action_counts[action] += 1
            if last_action and last_action != action:
                transitions.append((last_action, action))
            last_action = action

        if evt == "attack_executed":
            total_attacks_executed += 1
            total_damage_dealt += score

        if evt == "attack_blocked":
            blocked_reasons[reason] += 1

        # Auditoría de juego limpio
        if "CHEAT" in reason or "FROM_SOUND" in reason:
            wallhack_violations += 1

    # Cálculo de entropía de Shannon para diversidad táctica
    total_actions = sum(action_counts.values())
    entropy = 0.0
    if total_actions > 0:
        for count in action_counts.values():
            p = count / total_actions
            if p > 0:
                entropy -= p * math.log2(p)

    return {
        "run_id": run_id,
        "scenario_id": scenario_id,
        "seed": seed,
        "duration_ms": max_sim_time,
        "total_records": len(records),
        "event_counts": dict(event_counts),
        "action_counts": dict(action_counts),
        "unique_transitions": len(set(transitions)),
        "tactical_entropy": round(entropy, 3),
        "total_attacks_executed": total_attacks_executed,
        "total_damage_dealt": total_damage_dealt,
        "total_attacks_blocked": sum(blocked_reasons.values()),
        "blocked_reasons": dict(blocked_reasons),
        "wallhack_violations": wallhack_violations,
    }


def format_markdown_comparison(stats_a: Dict[str, Any], stats_b: Dict[str, Any], label_a: str, label_b: str) -> str:
    lines = [
        f"### Comparación Experimental de Telemetría: {label_a} vs {label_b}",
        "",
        "| Métrica / Dimensión | " + label_a + " | " + label_b + " | Delta / Observación |",
        "|---|---:|---:|---|",
        f"| **Escenario / Semilla** | {stats_a.get('scenario_id')}:{stats_a.get('seed')} | {stats_b.get('scenario_id')}:{stats_b.get('seed')} | "
        + ("Paridad" if stats_a.get('seed') == stats_b.get('seed') else "Semillas distintas") + " |",
        f"| **Duración Simulación** | {stats_a.get('duration_ms', 0)} ms | {stats_b.get('duration_ms', 0)} ms | "
        f"Δ {stats_b.get('duration_ms', 0) - stats_a.get('duration_ms', 0)} ms |",
        f"| **Eventos Registrados** | {stats_a.get('total_records', 0)} | {stats_b.get('total_records', 0)} | "
        f"Δ {stats_b.get('total_records', 0) - stats_a.get('total_records', 0)} |",
        f"| **Ataques Ejecutados** | {stats_a.get('total_attacks_executed', 0)} | {stats_b.get('total_attacks_executed', 0)} | "
        f"Daño: {stats_a.get('total_damage_dealt', 0):.0f} vs {stats_b.get('total_damage_dealt', 0):.0f} |",
        f"| **Ataques Bloqueados (Oclusión/Ángulo)** | {stats_a.get('total_attacks_blocked', 0)} | {stats_b.get('total_attacks_blocked', 0)} | "
        f"Filtro físico activo |",
        f"| **Diversidad Táctica (Entropía H)** | {stats_a.get('tactical_entropy', 0.0)} bits | {stats_b.get('tactical_entropy', 0.0)} bits | "
        f"Mayor entropía = comportamiento menos monótono |",
        f"| **Transiciones Distintas** | {stats_a.get('unique_transitions', 0)} | {stats_b.get('unique_transitions', 0)} | "
        f"Variedad de decisiones |",
        f"| **Violaciones de Juego Limpio** | {stats_a.get('wallhack_violations', 0)} | {stats_b.get('wallhack_violations', 0)} | "
        + ("0 violaciones (VERIFICADO)" if stats_a.get('wallhack_violations', 0) == 0 and stats_b.get('wallhack_violations', 0) == 0 else "¡ALERTA WALLHACK!") + " |",
        "",
        "#### Distribución de Acciones y Estados:",
        "",
        f"- **{label_a}:** {stats_a.get('action_counts', {})}",
        f"- **{label_b}:** {stats_b.get('action_counts', {})}",
        "",
        "#### Motivos de Disparos Bloqueados:",
        "",
        f"- **{label_a}:** {stats_a.get('blocked_reasons', {})}",
        f"- **{label_b}:** {stats_b.get('blocked_reasons', {})}",
    ]
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description="Compara registros JSONL de telemetría entre variantes.")
    parser.add_argument("file_a", type=Path, help="Ruta al archivo JSONL de la variante A (ej. Baseline)")
    parser.add_argument("file_b", type=Path, nargs="?", default=None, help="Ruta al archivo JSONL de la variante B (ej. NpcAnything)")
    parser.add_argument("--label-a", default="Baseline FSM", help="Etiqueta para la variante A")
    parser.add_argument("--label-b", default="NpcAnything Core", help="Etiqueta para la variante B")
    parser.add_argument("--json", action="store_true", help="Imprime el resultado en formato JSON")

    args = parser.parse_args()

    if not args.file_a.exists():
        print(f"[ERROR] No existe el archivo: {args.file_a}", file=sys.stderr)
        sys.exit(1)

    records_a = load_jsonl(args.file_a)
    stats_a = analyze_run(records_a)

    if args.file_b is None:
        if args.json:
            print(json.dumps(stats_a, indent=2, ensure_ascii=False))
        else:
            print(f"### Análisis de Telemetría: {args.label_a}")
            print(json.dumps(stats_a, indent=2, ensure_ascii=False))
        return

    if not args.file_b.exists():
        print(f"[ERROR] No existe el archivo: {args.file_b}", file=sys.stderr)
        sys.exit(1)

    records_b = load_jsonl(args.file_b)
    stats_b = analyze_run(records_b)

    if args.json:
        result = {
            "variant_a": {"label": args.label_a, "stats": stats_a},
            "variant_b": {"label": args.label_b, "stats": stats_b},
        }
        print(json.dumps(result, indent=2, ensure_ascii=False))
    else:
        print(format_markdown_comparison(stats_a, stats_b, args.label_a, args.label_b))


if __name__ == "__main__":
    main()
