from __future__ import annotations

import os
import shutil
import subprocess
import tempfile
import zipfile
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
) -> tuple[list[Path], list[Path]]:
    artifacts_dir = ritsulib_root / "artifacts" / "nuget"
    github_dir = ritsulib_root / "artifacts" / "github"
    key = _resolve_api_key(api_key)
    published: list[Path] = []
    zips: list[Path] = []
    for compat_target in compat_targets:
        package = run_pack(
            ritsulib_root,
            configuration=configuration,
            skip_build=skip_build,
            artifacts_dir=artifacts_dir,
            compat_target=compat_target,
        )
        zip_path = create_github_zip(
            ritsulib_root,
            package=package,
            configuration=configuration,
            compat_target=compat_target,
            output_dir=github_dir,
        )
        run_push(package, source=source, api_key=key)
        published.append(package)
        zips.append(zip_path)
    return published, zips


def publish_nuget(
    ritsulib_root: Path,
    *,
    configuration: str,
    source: str,
    api_key: str | None,
    skip_build: bool,
    compat_target: str | None = None,
) -> Path:
    if compat_target is None or not compat_target.strip():
        msg = "compat_target is required for publish_nuget(). Use publish_nugets() for multi-target release."
        raise RuntimeError(msg)
    packages, _ = publish_nugets(
        ritsulib_root,
        configuration=configuration,
        source=source,
        api_key=api_key,
        skip_build=skip_build,
        compat_targets=[compat_target.strip()],
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


def create_github_zip(
    ritsulib_root: Path,
    *,
    package: Path,
    configuration: str,
    compat_target: str,
    output_dir: Path,
) -> Path:
    output_dir.mkdir(parents=True, exist_ok=True)
    dll_path = ritsulib_root / ".godot" / "mono" / "temp" / "bin" / configuration / "STS2-RitsuLib.dll"
    manifest_path = ritsulib_root / "mod_manifest.json"
    if not dll_path.is_file():
        msg = f"Could not find built DLL for zip packaging: {dll_path}"
        raise RuntimeError(msg)
    if not manifest_path.is_file():
        msg = f"Could not find mod_manifest.json for zip packaging: {manifest_path}"
        raise RuntimeError(msg)

    zip_name = f"{package.stem}.github.zip"
    zip_path = output_dir / zip_name
    with zipfile.ZipFile(zip_path, mode="w", compression=zipfile.ZIP_DEFLATED) as zf:
        zf.write(dll_path, arcname="STS2-RitsuLib.dll")
        zf.write(manifest_path, arcname="mod_manifest.json")
        zf.writestr("compat-target.txt", compat_target + "\n")
    return zip_path


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


