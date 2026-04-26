from __future__ import annotations

import os
import shutil
import subprocess
import tempfile
import time
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

from release_lib.msbuild_eval import get_csproj_property
from release_lib.repo_layout import (
    ARTIFACTS_GITHUB,
    ARTIFACTS_NUGET,
    GITHUB_ZIP_FILENAME_SUFFIX,
    GODOT_MONO_BIN_PREFIX,
    MOD_MANIFEST_NAME,
    RITSULIB_CSPROJ_NAME,
    SNUPKG_SUFFIX,
    ritsulib_built_dll_name,
)


def run_pack(
    ritsulib_root: Path,
    *,
    configuration: str,
    skip_build: bool,
    artifacts_dir: Path,
    compat_target: str,
    version_override: str | None = None,
    sts2_api_signature_root: Path | None = None,
    sts2_dir: Path | None = None,
) -> Path:
    artifacts_dir.mkdir(parents=True, exist_ok=True)
    csproj = ritsulib_root / RITSULIB_CSPROJ_NAME
    started_at = time.time()
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
    if version_override is not None and version_override.strip():
        override = version_override.strip()
        args.append(f"/p:Version={override}")
        args.append(f"/p:PackageVersion={override}")
        args.append(f"/p:AssemblyInformationalVersion={override}")
    if sts2_api_signature_root is not None:
        args.append(f"/p:Sts2ApiSignatureRoot={sts2_api_signature_root}")
    if sts2_dir is not None:
        args.append(f"/p:Sts2Dir={sts2_dir}")
    if skip_build:
        args.append("--no-build")

    subprocess.run(args, cwd=ritsulib_root, check=True)

    nupkgs = sorted(
        (p for p in artifacts_dir.glob("*.nupkg") if not p.name.endswith(SNUPKG_SUFFIX))
    )
    created = [p for p in nupkgs if p.resolve() not in before]
    if created:
        return max(created, key=lambda p: p.stat().st_mtime)

    refreshed = [p for p in nupkgs if p.stat().st_mtime >= started_at - 1]
    if refreshed:
        return max(refreshed, key=lambda p: p.stat().st_mtime)

    expected = _resolve_expected_package_path(
        csproj,
        artifacts_dir,
        compat_target,
        version_override=version_override,
    )
    if expected is not None and expected.is_file():
        return expected

    msg = f"No .nupkg generated for compat target {compat_target!r} under {artifacts_dir}"
    raise RuntimeError(msg)


def _resolve_expected_package_path(
    csproj: Path,
    artifacts_dir: Path,
    compat_target: str,
    *,
    version_override: str | None,
) -> Path | None:
    try:
        root = ET.fromstring(csproj.read_text(encoding="utf-8"))
    except ET.ParseError:
        return None

    base_package_id = _first_node_text(root, ".//PackageId")
    version = version_override.strip() if version_override and version_override.strip() else _first_node_text(root, ".//Version")
    if not base_package_id or not version:
        return None

    try:
        latest_compat = get_csproj_property(csproj, "RitsuLibLatestApiCompat").strip() or None
    except (OSError, RuntimeError):
        latest_compat = None

    if latest_compat and compat_target != latest_compat:
        package_id = f"{base_package_id}.Compat.{compat_target}"
    else:
        package_id = base_package_id
    return artifacts_dir / f"{package_id}.{version}.nupkg"


def _first_node_text(root: ET.Element, xpath: str) -> str | None:
    node = root.find(xpath)
    if node is None or node.text is None:
        return None
    value = node.text.strip()
    return value or None


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
    version_override: str | None = None,
    sts2_api_signature_root: Path | None = None,
    sts2_dir: Path | None = None,
) -> tuple[list[Path], list[Path]]:
    artifacts_dir = ritsulib_root / ARTIFACTS_NUGET
    github_dir = ritsulib_root / ARTIFACTS_GITHUB
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
            version_override=version_override,
            sts2_api_signature_root=sts2_api_signature_root,
            sts2_dir=sts2_dir,
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


def build_artifacts(
    ritsulib_root: Path,
    *,
    configuration: str,
    skip_build: bool,
    compat_targets: list[str],
    version_override: str | None = None,
    sts2_api_signature_root: Path | None = None,
    sts2_dir: Path | None = None,
) -> tuple[list[Path], list[Path]]:
    artifacts_dir = ritsulib_root / ARTIFACTS_NUGET
    github_dir = ritsulib_root / ARTIFACTS_GITHUB
    packages: list[Path] = []
    zips: list[Path] = []
    for compat_target in compat_targets:
        package = run_pack(
            ritsulib_root,
            configuration=configuration,
            skip_build=skip_build,
            artifacts_dir=artifacts_dir,
            compat_target=compat_target,
            version_override=version_override,
            sts2_api_signature_root=sts2_api_signature_root,
            sts2_dir=sts2_dir,
        )
        zip_path = create_github_zip(
            ritsulib_root,
            package=package,
            configuration=configuration,
            compat_target=compat_target,
            output_dir=github_dir,
        )
        packages.append(package)
        zips.append(zip_path)
    return packages, zips


def publish_nuget(
    ritsulib_root: Path,
    *,
    configuration: str,
    source: str,
    api_key: str | None,
    skip_build: bool,
    compat_target: str | None = None,
    version_override: str | None = None,
    sts2_api_signature_root: Path | None = None,
    sts2_dir: Path | None = None,
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
        version_override=version_override,
        sts2_api_signature_root=sts2_api_signature_root,
        sts2_dir=sts2_dir,
    )
    return packages[0]


def verify_pack_in_tempdir(
    ritsulib_root: Path,
    *,
    configuration: str,
    skip_build: bool,
    compat_targets: list[str],
    version_override: str | None = None,
    sts2_api_signature_root: Path | None = None,
    sts2_dir: Path | None = None,
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
                version_override=version_override,
                sts2_api_signature_root=sts2_api_signature_root,
                sts2_dir=sts2_dir,
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
    dll_path = ritsulib_root / GODOT_MONO_BIN_PREFIX / configuration / ritsulib_built_dll_name()
    manifest_path = ritsulib_root / MOD_MANIFEST_NAME
    if not dll_path.is_file():
        msg = f"Could not find built DLL for zip packaging: {dll_path}"
        raise RuntimeError(msg)
    if not manifest_path.is_file():
        msg = f"Could not find mod_manifest.json for zip packaging: {manifest_path}"
        raise RuntimeError(msg)

    zip_name = f"{package.stem}{GITHUB_ZIP_FILENAME_SUFFIX}"
    zip_path = output_dir / zip_name
    with zipfile.ZipFile(zip_path, mode="w", compression=zipfile.ZIP_DEFLATED) as zf:
        zf.write(dll_path, arcname=ritsulib_built_dll_name())
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


