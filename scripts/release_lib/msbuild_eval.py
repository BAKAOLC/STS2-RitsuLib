from __future__ import annotations

import subprocess
from pathlib import Path


def get_csproj_property(project_file: Path, property_name: str) -> str:
    project_file = project_file.resolve()
    if not project_file.is_file():
        msg = f"Project file not found: {project_file}"
        raise FileNotFoundError(msg)
    cwd = project_file.parent
    r = subprocess.run(
        [
            "dotnet",
            "msbuild",
            str(project_file),
            f"-getProperty:{property_name}",
            "-nologo",
        ],
        cwd=cwd,
        check=False,
        capture_output=True,
        text=True,
    )
    if r.returncode != 0:
        err = (r.stderr or r.stdout or "").strip()
        msg = f"dotnet msbuild -getProperty:{property_name} failed (exit {r.returncode})"
        if err:
            msg = f"{msg}: {err}"
        raise RuntimeError(msg)
    return r.stdout.strip()
