"""
tools/agent/test_agent.py
Suite de pruebas unitarias para el agente de ingeniería local y sandbox de seguridad (BK-030-BK-033, RNF-05).
"""

from pathlib import Path
import tempfile
import unittest

from inspector import detect_engine, find_ai_entrypoints, inspect_project, list_behaviors
from reporter import export_report, generate_markdown_report
from workspace_guard import WorkspaceGuard

REPO_ROOT = Path(__file__).resolve().parents[2]


class WorkspaceGuardSecurityTests(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp_dir.cleanup)
        self.workspace_root = Path(self.temp_dir.name) / "workspace"
        self.workspace_root.mkdir()
        self.proposals_dir = self.workspace_root / "proposals"
        self.proposals_dir.mkdir()

        self.guard = WorkspaceGuard(
            allowed_read_roots=[self.workspace_root],
            allowed_write_root=self.proposals_dir,
        )

    def test_allows_reading_inside_workspace(self):
        sample_file = self.workspace_root / "test.txt"
        sample_file.write_text("ok", encoding="utf-8")

        resolved = self.guard.validate_read_path(sample_file)
        self.assertEqual(sample_file.resolve(), resolved)

    def test_blocks_relative_path_traversal_escape(self):
        # Intento de fuga mediante secuencias '..' hacia fuera del workspace
        escaped_path = self.workspace_root / ".." / "outside.txt"

        with self.assertRaises(PermissionError):
            self.guard.validate_read_path(escaped_path)

    def test_blocks_absolute_path_outside_allowlist(self):
        # Intento de leer directorio del sistema
        system_path = Path("C:/Windows/System32/drivers/etc/hosts") if Path("C:/Windows").exists() else Path("/etc/passwd")

        with self.assertRaises(PermissionError):
            self.guard.validate_read_path(system_path)

    def test_allows_write_in_proposals_dir(self):
        valid_dest = self.proposals_dir / "report.json"
        resolved = self.guard.validate_write_path(valid_dest)
        self.assertEqual(valid_dest.resolve(), resolved)

    def test_blocks_write_outside_proposals_dir(self):
        # Intento de escribir directamente en la raíz del workspace o código fuente
        forbidden_dest = self.workspace_root / "malicious.patch"

        with self.assertRaises(PermissionError):
            self.guard.validate_write_path(forbidden_dest)


class AgentInspectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.guard = WorkspaceGuard(
            allowed_read_roots=[REPO_ROOT],
            allowed_write_root=REPO_ROOT / "proposals",
        )

    def test_detect_engine_identifies_godot_project(self):
        engine_info = detect_engine(REPO_ROOT / "demo" / "GodotArena", self.guard)

        self.assertEqual("Godot", engine_info["engine"])
        self.assertEqual("VERIFIED", engine_info["evidence_level"])
        self.assertGreaterEqual(engine_info["confidence"], 0.9)
        self.assertTrue(len(engine_info["evidence"]) > 0)

    def test_find_ai_entrypoints_identifies_core_components(self):
        entrypoints = find_ai_entrypoints(REPO_ROOT, self.guard)
        self.assertTrue(len(entrypoints) > 0)

        paths = [ep["path"] for ep in entrypoints]
        self.assertTrue(any("NpcBrainController" in p for p in paths), "Debe identificar NpcBrainController")
        self.assertTrue(any("ActionCandidateGenerator" in p or "Utility" in p for p in paths), "Debe identificar el Core Utility")

    def test_list_behaviors_identifies_tactical_actions(self):
        behaviors = list_behaviors(REPO_ROOT, self.guard)
        actions = [b["action"] for b in behaviors]

        self.assertIn("Patrol", actions)
        self.assertIn("Pursue", actions)
        self.assertIn("Attack", actions)
        self.assertIn("TakeCover", actions)
        self.assertIn("Flank", actions)
        self.assertIn("Follow", actions)

    def test_inspect_project_integrates_all_analyses(self):
        inventory = inspect_project(REPO_ROOT, self.guard)

        self.assertIn("total_files", inventory)
        self.assertGreater(inventory["total_files"], 10)
        self.assertIn("engine_detection", inventory)
        self.assertIn("ai_entrypoints", inventory)
        self.assertIn("identified_behaviors", inventory)
        self.assertIn("modifiability_assessment", inventory)

    def test_report_export_creates_valid_json_and_markdown(self):
        temp_dir = tempfile.TemporaryDirectory()
        self.addCleanup(temp_dir.cleanup)
        out_dir = Path(temp_dir.name)

        guard = WorkspaceGuard(
            allowed_read_roots=[REPO_ROOT],
            allowed_write_root=out_dir,
        )

        inventory = inspect_project(REPO_ROOT, guard)
        json_file, md_file = export_report(inventory, out_dir, guard, filename_prefix="test_audit")

        self.assertTrue(json_file.exists())
        self.assertTrue(md_file.exists())

        # Verificar contenido Markdown
        md_text = md_file.read_text(encoding="utf-8")
        self.assertIn("Informe de Diagnóstico e Inventario Técnico de IA", md_text)
        self.assertIn("[VERIFIED]", md_text)
        self.assertIn("Acciones Tácticas y Comportamientos", md_text)


if __name__ == "__main__":
    unittest.main()
