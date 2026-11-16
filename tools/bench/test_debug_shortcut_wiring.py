from pathlib import Path
import unittest


REPO_ROOT = Path(__file__).resolve().parents[2]


class DebugShortcutWiringTests(unittest.TestCase):
    def test_shortcuts_are_opt_in_and_only_enabled_in_npc_arena(self):
        player_script = (REPO_ROOT / "demo/GodotArena/scripts/Player.cs").read_text(
            encoding="utf-8"
        )
        player_scene = (REPO_ROOT / "demo/GodotArena/scenes/Player.tscn").read_text(
            encoding="utf-8"
        )
        npc_scene = (REPO_ROOT / "demo/GodotArena/scenes/Arena_NpcAnything.tscn").read_text(
            encoding="utf-8"
        )
        baseline_scene = (REPO_ROOT / "demo/GodotArena/scenes/Arena_Baseline.tscn").read_text(
            encoding="utf-8"
        )

        self.assertIn("EnableDebugPositionShortcuts { get; set; } = false", player_script)
        self.assertNotIn("EnableDebugPositionShortcuts", player_scene)
        self.assertIn("EnableDebugPositionShortcuts = true", npc_scene)
        self.assertNotIn("EnableDebugPositionShortcuts", baseline_scene)


if __name__ == "__main__":
    unittest.main()
