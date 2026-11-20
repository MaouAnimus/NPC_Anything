import json
from pathlib import Path
import tempfile
import unittest

from run_scenarios import ValidationError, parse_args, validate_jsonl


class ValidateJsonlTests(unittest.TestCase):
    def write_events(self, events):
        temporary_directory = tempfile.TemporaryDirectory()
        self.addCleanup(temporary_directory.cleanup)
        path = Path(temporary_directory.name) / "events.jsonl"
        path.write_text(
            "".join(json.dumps(event) + "\n" for event in events),
            encoding="utf-8",
        )
        return path

    def event(self, event_type, action, npc_id="enemy_core_01"):
        return {
            "schema_version": "0.1",
            "run_id": "run_test",
            "scenario_id": "INT-01",
            "seed": 42,
            "simulation_time_ms": 10,
            "npc_id": npc_id,
            "event": event_type,
            "action": action,
            "score": 1.0,
            "reason_code": "TEST",
        }

    def test_accepts_runtime_transition_for_one_npc(self):
        path = self.write_events(
            [
                self.event("arena_started", "Initialize", "system"),
                self.event("action_changed", "Patrol"),
                self.event("action_changed", "Pursue"),
                self.event("attack_executed", "Attack"),
                self.event("action_changed", "Attack"),
            ]
        )

        traces = validate_jsonl(path, "INT-01", 42)

        self.assertEqual(["Patrol", "Pursue", "Attack"], traces["enemy_core_01"])

    def test_rejects_out_of_order_or_interrupted_transition(self):
        path = self.write_events(
            [
                self.event("action_changed", "Patrol"),
                self.event("action_changed", "Pursue"),
                self.event("action_changed", "Investigate"),
                self.event("action_changed", "Attack"),
            ]
        )

        with self.assertRaisesRegex(ValidationError, "no NPC completed"):
            validate_jsonl(path, "INT-01", 42)

    def test_does_not_combine_transitions_from_different_npcs(self):
        path = self.write_events(
            [
                self.event("action_changed", "Patrol", "enemy_a"),
                self.event("action_changed", "Pursue", "enemy_a"),
                self.event("action_changed", "Attack", "enemy_b"),
            ]
        )

        with self.assertRaisesRegex(ValidationError, "no NPC completed"):
            validate_jsonl(path, "INT-01", 42)

    def test_rejects_metadata_mismatch(self):
        event = self.event("action_changed", "Patrol")
        event["seed"] = 43
        path = self.write_events([event])

        with self.assertRaisesRegex(ValidationError, "seed 43"):
            validate_jsonl(path, "INT-01", 42)

    def test_rejects_malformed_jsonl(self):
        path = self.write_events([self.event("action_changed", "Patrol")])
        path.write_text("{invalid}\n", encoding="utf-8")

        with self.assertRaisesRegex(ValidationError, "invalid JSON"):
            validate_jsonl(path, "INT-01", 42)

    def test_accepts_matching_difficulty(self):
        events = [
            self.event("action_changed", "Patrol"),
            self.event("action_changed", "Pursue"),
            self.event("action_changed", "Attack"),
        ]
        for evt in events:
            evt["difficulty"] = "hard"

        path = self.write_events(events)
        traces = validate_jsonl(path, "INT-01", 42, expected_difficulty="hard")
        self.assertEqual(["Patrol", "Pursue", "Attack"], traces["enemy_core_01"])

    def test_rejects_difficulty_mismatch(self):
        events = [
            self.event("action_changed", "Patrol"),
            self.event("action_changed", "Pursue"),
            self.event("action_changed", "Attack"),
        ]
        for evt in events:
            evt["difficulty"] = "easy"

        path = self.write_events(events)
        with self.assertRaisesRegex(ValidationError, "difficulty 'easy' does not match 'expert'"):
            validate_jsonl(path, "INT-01", 42, expected_difficulty="expert")

    def test_parse_args_handles_difficulty(self):
        args_default = parse_args([])
        self.assertEqual("normal", args_default.difficulty)

        args_custom = parse_args(["--difficulty", "expert"])
        self.assertEqual("expert", args_custom.difficulty)


if __name__ == "__main__":
    unittest.main()
