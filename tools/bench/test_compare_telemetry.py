import json
from pathlib import Path
import tempfile
import unittest

from compare_telemetry import analyze_run, format_markdown_comparison


class CompareTelemetryTests(unittest.TestCase):
    def make_record(self, event, action="Patrol", difficulty="normal", score=1.0, reason="TEST"):
        return {
            "schema_version": "0.1",
            "run_id": "run_bench_01",
            "scenario_id": "SC-02",
            "seed": 42,
            "difficulty": difficulty,
            "simulation_time_ms": 1500,
            "npc_id": "enemy_core_01",
            "event": event,
            "action": action,
            "score": score,
            "reason_code": reason,
        }

    def test_analyze_run_extracts_difficulty_and_metrics(self):
        records = [
            self.make_record("arena_started", action="Initialize", difficulty="hard"),
            self.make_record("action_changed", action="Patrol", difficulty="hard"),
            self.make_record("action_changed", action="Pursue", difficulty="hard"),
            self.make_record("attack_executed", action="Attack", difficulty="hard", score=10.0),
        ]

        stats = analyze_run(records)

        self.assertEqual("hard", stats["difficulty"])
        self.assertEqual("SC-02", stats["scenario_id"])
        self.assertEqual(42, stats["seed"])
        self.assertEqual(1, stats["total_attacks_executed"])
        self.assertEqual(10.0, stats["total_damage_dealt"])
        self.assertEqual(0, stats["wallhack_violations"])

    def test_format_markdown_comparison_includes_difficulty_row(self):
        records_a = [self.make_record("action_changed", action="Patrol", difficulty="easy")]
        records_b = [self.make_record("action_changed", action="Pursue", difficulty="expert")]

        stats_a = analyze_run(records_a)
        stats_b = analyze_run(records_b)

        markdown = format_markdown_comparison(stats_a, stats_b, "Easy Run", "Expert Run")

        self.assertIn("Dificultad", markdown)
        self.assertIn("easy", markdown)
        self.assertIn("expert", markdown)


if __name__ == "__main__":
    unittest.main()
