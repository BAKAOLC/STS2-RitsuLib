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
    compat_target: str,
) -> Path:
    artifacts_dir.mkdir(parents=True, exist_ok=True)
    csproj = ritsulib_root / "STS2-RitsuLib.csproj"
    before = {p.resolve() for p in artifacts_dir.glob("*.nupkg")}
    args = [
        "dotnet",
        "pack",
        str(csproj),
        "-c",
        configuration,
        "-o",
        str(artifacts_dir),
        "/p:ContinuousIntegrationBuild=false",
        f"/p:Sts2ApiCompat={compat_target}",
    ]
    if skip_build:
        args.append("--no-build")

    subprocess.run(args, cwd=ritsulib_root, check=True)

    nupkgs = sorted((p for p in artifacts_dir.glob("*.nupkg") if not p.name.endswith(".snupkg")))
    created = [p for p in nupkgs if p.resolve() not in before]
    if not created:
        msg = f"No new .nupkg generated for compat target {compat_target!r} under {artifacts_dir}"
        raise RuntimeError(msg)
    return max(created, key=lambda p: p.stat().st_mtime)


def _resolve_api_key(api_key: str | None) -> str:
    key = api_key or os.environ.get("NUGET_API_KEY")
    if not key or not key.strip():
        msg = "NuGet API key missing. Pass --api-key or set NUGET_API_KEY."
        raise RuntimeError(msg)
    return key.strip()


def publish_nugets(
    ritsulib_root: Path,
    *,
    configuration: str,
    source: str,
    api_key: str | None,
    skip_build: bool,
    compat_targets: list[str],
) -> list[Path]:
    artifacts_dir = ritsulib_root / "artifacts" / "nuget"
    key = _resolve_api_key(api_key)
    published: list[Path] = []
    for compat_target in compat_targets:
        package = run_pack(
            ritsulib_root,
            configuration=configuration,
            skip_build=skip_build,
            artifacts_dir=artifacts_dir,
            compat_target=compat_target,
        )
        run_push(package, source=source, api_key=key)
        published.append(package)
    return published


def publish_nuget(
    ritsulib_root: Path,
    *,
    configuration: str,
    source: str,
    api_key: str | None,
    skip_build: bool,
) -> Path:
    packages = publish_nugets(
        ritsulib_root,
        configuration=configuration,
        source=source,
        api_key=api_key,
        skip_build=skip_build,
        compat_targets=["0.104.0"],
    )
    return packages[0]


def verify_pack_in_tempdir(
    ritsulib_root: Path,
    *,
    configuration: str,
    skip_build: bool,
    compat_targets: list[str],
) -> list[str]:
    tmp = Path(tempfile.mkdtemp(prefix="ritsulib-nuget-"))
    try:
        package_names: list[str] = []
        for compat_target in compat_targets:
            pkg = run_pack(
                ritsulib_root,
                configuration=configuration,
                skip_build=skip_build,
                artifacts_dir=tmp,
                compat_target=compat_target,
            )
            package_names.append(pkg.name)
        return package_names
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


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


