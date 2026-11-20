"""
tools/agent/workspace_guard.py
Modelo de permisos y sandbox de seguridad para el agente de ingeniería local (BK-032, RNF-05).

Garantías:
1. Allowlist estricta de rutas de lectura autorizadas.
2. Bloqueo total de path traversal (secuencias '..' y symlinks fuera del workspace).
3. Sandbox de escritura: solo se permite escribir dentro de 'proposals/'.
4. Redacción de rutas personales y datos sensibles en logs.
"""

from __future__ import annotations

import os
from pathlib import Path
import re
from typing import Iterable, Optional


class WorkspaceGuard:
    """Controlador de límites de seguridad y autorización del workspace."""

    def __init__(
        self,
        allowed_read_roots: Iterable[Path | str],
        allowed_write_root: Optional[Path | str] = None,
    ) -> None:
        self.allowed_read_roots = [
            Path(p).resolve().absolute() for p in allowed_read_roots
        ]
        self.allowed_write_root = (
            Path(allowed_write_root).resolve().absolute()
            if allowed_write_root
            else None
        )

    def is_path_within(self, candidate: Path, root: Path) -> bool:
        """Verifica si candidate está estrictamente contenido dentro de root sin escapar."""
        try:
            candidate_canon = candidate.resolve().absolute()
            root_canon = root.resolve().absolute()
            # En Python 3.9+, candidate_canon.is_relative_to(root_canon)
            candidate_canon.relative_to(root_canon)
            return True
        except (ValueError, RuntimeError):
            return False

    def validate_read_path(self, raw_path: Path | str) -> Path:
        """
        Valida que la ruta solicitada sea de lectura permitida y no escape del sandbox.
        Lanza PermissionError ante cualquier intento de fuga de ruta (path traversal).
        """
        target = Path(raw_path)
        if not target.is_absolute():
            # Interpretar relativo al primer root autorizado
            if not self.allowed_read_roots:
                raise PermissionError("No hay directorios de lectura autorizados configurados.")
            target = (self.allowed_read_roots[0] / target)

        resolved = target.resolve().absolute()

        # Comprobar si está dentro de alguna de las raíces autorizadas
        is_allowed = any(self.is_path_within(resolved, root) for root in self.allowed_read_roots)
        if not is_allowed:
            raise PermissionError(
                f"[GUARD] Acceso denegado: La ruta '{raw_path}' ({resolved}) está fuera del workspace permitido."
            )

        return resolved

    def validate_write_path(self, raw_path: Path | str) -> Path:
        """
        Valida que la ruta de destino esté estrictamente dentro de la carpeta autorizada de propuestas.
        Lanza PermissionError si se intenta escribir fuera de proposals/.
        """
        if not self.allowed_write_root:
            raise PermissionError(
                "[GUARD] Operación de escritura prohibida: Sandbox en modo estricto de solo lectura."
            )

        target = Path(raw_path)
        if not target.is_absolute():
            target = (self.allowed_write_root / target)

        resolved = target.resolve().absolute()

        if not self.is_path_within(resolved, self.allowed_write_root):
            raise PermissionError(
                f"[GUARD] Escritura denegada: La ruta '{raw_path}' está fuera de '{self.allowed_write_root}'. "
                "Los agentes solo tienen permiso de escritura en 'proposals/'."
            )

        return resolved

    @staticmethod
    def sanitize_path_string(text: str, project_root: Optional[Path] = None) -> str:
        """
        Redacta rutas absolutas del usuario y reemplaza rutas locales por relativas al proyecto.
        """
        user_home = str(Path.home().resolve())
        sanitized = text.replace(user_home, "~")

        if project_root:
            root_str = str(project_root.resolve())
            sanitized = sanitized.replace(root_str, ".")

        # Redactar posibles tokens y secretos en el texto
        sanitized = re.sub(r"(token|secret|password|key)\s*[:=]\s*[^\s,]+", r"\1: [REDACTED]", sanitized, flags=re.I)
        return sanitized
