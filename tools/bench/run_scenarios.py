#!/usr/bin/env python3
"""Run deterministic headless Godot transition checks and validate their JSONL traces."""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SCENE = "res://scenes/Arena_TransitionTest.tscn"
DEFAULT_SCENARIOS = tuple(f"INT-{number:02}" for number in range(1, 8))
EXPECTED_TRANSITIONS = ("Patrol", "Pursue", "Attack")


class ValidationError(ValueError):
    """Raised when a Godot telemetry file does not satisfy the runtime contract."""


def validate_jsonl(path: Path, scenario_id: str, seed: int) -> dict[str, list[str]]:
    """Validate metadata and require the expected adjacent action transitions per NPC."""
    events: list[dict[str, Any]] = []
    try:
        with path.open(encoding="utf-8") as telemetry_file:
            for line_number, line in enumerate(telemetry_file, start=1):
                if not line.strip():
                    raise ValidationError(f"{path}:{line_number}: blank JSONL record")
                try:
                    event = json.loads(line)
                except json.JSONDecodeError as exc:
                    raise ValidationError(f"{path}:{line_number}: invalid JSON: {exc.msg}") from exc
                if not isinstance(event, dict):
                    raise ValidationError(f"{path}:{line_number}: expected a JSON object")
                string_fields = (
                    "run_id",
                    "scenario_id",
                    "npc_id",
                    "event",
                    "action",
                    "reason_code",
                )
                for field in (
                    "schema_version",
                    *string_fields,
                    "seed",
                    "simulation_time_ms",
                    "score",
                ):
                    if field not in event:
                        raise ValidationError(f"{path}:{line_number}: missing '{field}'")
                if event["schema_version"] != "0.1":
                    raise ValidationError(
                        f"{path}:{line_number}: unsupported schema_version "
                        f"{event['schema_version']!r}"
                    )
                for field in string_fields:
                    if not isinstance(event[field], str):
                        raise ValidationError(
                            f"{path}:{line_number}: '{field}' must be a string"
                        )
                if not isinstance(event["seed"], int) or isinstance(event["seed"], bool):
                    raise ValidationError(f"{path}:{line_number}: 'seed' must be an integer")
                if (
                    not isinstance(event["simulation_time_ms"], int)
                    or isinstance(event["simulation_time_ms"], bool)
                    or event["simulation_time_ms"] < 0
                ):
                    raise ValidationError(
                        f"{path}:{line_number}: 'simulation_time_ms' must be a "
                        "non-negative integer"
                    )
                if not isinstance(event["score"], (int, float)) or isinstance(
                    event["score"], bool
                ):
                    raise ValidationError(f"{path}:{line_number}: 'score' must be numeric")
                if event["scenario_id"] != scenario_id:
                    raise ValidationError(
                        f"{path}:{line_number}: scenario_id {event['scenario_id']!r} "
                        f"does not match {scenario_id!r}"
                    )
                if event["seed"] != seed:
                    raise ValidationError(
                        f"{path}:{line_number}: seed {event['seed']!r} does not match {seed}"
                    )
                events.append(event)
    except OSError as exc:
        raise ValidationError(f"cannot read telemetry file {path}: {exc}") from exc

    if not events:
        raise ValidationError(f"{path}: telemetry contains no records")

    run_ids = {event["run_id"] for event in events}
    if len(run_ids) != 1:
        raise ValidationError(f"{path}: telemetry contains multiple run IDs")

    traces: dict[str, list[str]] = {}
    for event in events:
        if event["event"] == "action_changed":
            traces.setdefault(event["npc_id"], []).append(event["action"])

    matching_npcs = [
        npc_id
        for npc_id, actions in traces.items()
        if any(
            tuple(actions[index : index + len(EXPECTED_TRANSITIONS)]) == EXPECTED_TRANSITIONS
            for index in range(len(actions) - len(EXPECTED_TRANSITIONS) + 1)
        )
    ]
    if not matching_npcs:
        detail = "; ".join(f"{npc_id}: {actions}" for npc_id, actions in sorted(traces.items()))
        raise ValidationError(
            f"{path}: no NPC completed {' -> '.join(EXPECTED_TRANSITIONS)}; "
            f"observed action_changed traces: {detail or 'none'}"
        )

    return traces


def resolve_godot(requested: str | None) -> str:
    if requested:
        candidate = Path(requested).expanduser()
        if candidate.is_file():
            return str(candidate.resolve())
        resolved = shutil.which(requested)
        if resolved:
            return resolved
        raise FileNotFoundError(f"Godot executable not found: {requested}")

    for name in ("godot", "godot4", "Godot_v4.3-stable_mono_win64.exe"):
        resolved = shutil.which(name)
        if resolved:
            return resolved
    raise FileNotFoundError(
        "Godot .NET executable not found on PATH; pass --godot or set GODOT_BIN."
    )


def run_scenario(
    godot: str,
    scene: str,
    scenario_id: str,
    seed: int,
    frame_limit: int,
    timeout_seconds: float,
    output_path: Path,
) -> None:
    command = [
        godot,
        "--headless",
        "--path",
        str(REPO_ROOT / "demo" / "GodotArena"),
        "--scene",
        scene,
        "--quit-after",
        str(frame_limit),
        "--",
        "--scenario-id",
        scenario_id,
        "--seed",
        str(seed),
        "--telemetry-path",
        str(output_path),
    ]
    try:
        result = subprocess.run(
            command,
            cwd=REPO_ROOT,
            capture_output=True,
            text=True,
            timeout=timeout_seconds,
            check=False,
        )
    except subprocess.TimeoutExpired as exc:
        raise RuntimeError(f"Godot exceeded the {timeout_seconds:g}s timeout") from exc
    except OSError as exc:
        raise RuntimeError(f"could not start Godot: {exc}") from exc

    if result.returncode != 0:
        output = (result.stdout + "\n" + result.stderr).strip().splitlines()
        tail = "\n".join(output[-20:])
        raise RuntimeError(f"Godot exited with code {result.returncode}.\n{tail}")

    traces = validate_jsonl(output_path, scenario_id, seed)
    print(
        f"PASS {scenario_id} seed={seed}: "
        + "; ".join(f"{npc_id}={actions}" for npc_id, actions in sorted(traces.items()))
    )


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--godot", default=os.environ.get("GODOT_BIN"))
    parser.add_argument("--scene", default=DEFAULT_SCENE)
    parser.add_argument("--scenario-ids", nargs="+", choices=DEFAULT_SCENARIOS, default=DEFAULT_SCENARIOS)
    parser.add_argument("--base-seed", type=int, default=42)
    parser.add_argument("--frames", type=int, default=600, help="Godot frames before automatic quit")
    parser.add_argument("--timeout-seconds", type=float, default=120.0)
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=REPO_ROOT / "runs" / "int-scenarios",
        help="directory for retained JSONL outputs",
    )
    args = parser.parse_args(argv)
    if args.base_seed < 0 or args.base_seed + len(args.scenario_ids) - 1 > 2**31 - 1:
        parser.error("--base-seed and scenario count must produce non-negative 32-bit seeds")
    if args.frames < 1:
        parser.error("--frames must be positive")
    if args.timeout_seconds <= 0:
        parser.error("--timeout-seconds must be positive")
    if len(set(args.scenario_ids)) != len(args.scenario_ids):
        parser.error("--scenario-ids must not contain duplicates")
    return args


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        godot = resolve_godot(args.godot)
    except FileNotFoundError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2

    args.output_dir.mkdir(parents=True, exist_ok=True)
    run_dir = Path(tempfile.mkdtemp(prefix="m03c_", dir=args.output_dir))
    print(f"Godot: {godot}")
    print(f"JSONL output: {run_dir}")

    failures = 0
    for index, scenario_id in enumerate(args.scenario_ids):
        seed = args.base_seed + index
        output_path = run_dir / f"{scenario_id}_seed-{seed}.jsonl"
        try:
            run_scenario(
                godot,
                args.scene,
                scenario_id,
                seed,
                args.frames,
                args.timeout_seconds,
                output_path,
            )
        except (RuntimeError, ValidationError) as exc:
            failures += 1
            print(f"FAIL {scenario_id} seed={seed}: {exc}", file=sys.stderr)

    print(f"Result: {len(args.scenario_ids) - failures}/{len(args.scenario_ids)} passed")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
