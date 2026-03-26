from __future__ import annotations

import os
import shutil
import subprocess
import tempfile
from pathlib import Path


def run_pack(
    ritsulib_root: Path,
    *,
    configuration: str,
    skip_build: bool,
    artifacts_dir: Path,
) -> Path:
    artifacts_dir.mkdir(parents=True, exist_ok=True)
    csproj = ritsulib_root / "STS2-RitsuLib.csproj"
    args = [
        "dotnet",
        "pack",
        str(csproj),
        "-c",
        configuration,
        "-o",
        str(artifacts_dir),
        "/p:ContinuousIntegrationBuild=false",
    ]
    if skip_build:
        args.append("--no-build")

    subprocess.run(args, cwd=ritsulib_root, check=True)

    nupkgs = sorted(
        artifacts_dir.glob("*.nupkg"),
        key=lambda p: p.stat().st_mtime,
        reverse=True,
    )
    nupkgs = [p for p in nupkgs if not p.name.endswith(".snupkg")]
    if not nupkgs:
        msg = f"No .nupkg generated under {artifacts_dir}"
        raise RuntimeError(msg)
    return nupkgs[0]


def run_push(package: Path, *, source: str, api_key: str) -> None:
    subprocess.run(
        [
            "dotnet",
            "nuget",
            "push",
            str(package),
            "--source",
            source,
            "--api-key",
            api_key,
            "--skip-duplicate",
        ],
        cwd=package.parent,
        check=True,
    )


def publish_nuget(
    ritsulib_root: Path,
    *,
    configuration: str,
    source: str,
    api_key: str | None,
    skip_build: bool,
) -> Path:
    artifacts_dir = ritsulib_root / "artifacts" / "nuget"
    package = run_pack(
        ritsulib_root,
        configuration=configuration,
        skip_build=skip_build,
        artifacts_dir=artifacts_dir,
    )
    key = api_key or os.environ.get("NUGET_API_KEY")
    if not key or not key.strip():
        msg = "NuGet API key missing. Pass --api-key or set NUGET_API_KEY."
        raise RuntimeError(msg)
    run_push(package, source=source, api_key=key.strip())
    return package


def verify_pack_in_tempdir(
    ritsulib_root: Path,
    *,
    configuration: str,
    skip_build: bool,
) -> str:
    tmp = Path(tempfile.mkdtemp(prefix="ritsulib-nuget-"))
    try:
        pkg = run_pack(
            ritsulib_root,
            configuration=configuration,
            skip_build=skip_build,
            artifacts_dir=tmp,
        )
        return pkg.name
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
