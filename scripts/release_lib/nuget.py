from __future__ import annotations

import json
import hashlib
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
    COMPAT_TARGET_MARKER_NAME,
    GITHUB_ZIP_FILENAME_SUFFIX,
    GODOT_MONO_BIN_PREFIX,
    GODOT_MONO_OBJ_PREFIX,
    MOD_MANIFEST_NAME,
    RITSULIB_CSPROJ_NAME,
    SNUPKG_SUFFIX,
    VIEWER_DIST_REL,
    VIEWER_OUTPUT_DIR_NAME,
    VARIANT_MANIFEST_NAME,
    ritsulib_built_dll_name,
    ritsulib_built_doc_xml_name,
    ritsulib_built_pdb_name,
)


def prepare_bundle_staging(bundle_staging_root: Path) -> None:
    shutil.rmtree(bundle_staging_root, ignore_errors=True)
    bundle_staging_root.mkdir(parents=True, exist_ok=True)


def snapshot_bundle_variant_after_pack(
    ritsulib_root: Path,
    *,
    configuration: str,
    compat_target: str,
    bundle_staging_root: Path,
    latest_compat: str,
) -> None:
    """After dotnet pack for a compat target, copy RitsuLib bin outputs into bundle staging."""
    bin_dir = ritsulib_root / GODOT_MONO_BIN_PREFIX / configuration
    lib_dest = bundle_staging_root / "lib" / compat_target
    lib_dest.mkdir(parents=True, exist_ok=True)
    for name in (ritsulib_built_dll_name(), ritsulib_built_doc_xml_name(), ritsulib_built_pdb_name()):
        src = bin_dir / name
        if src.is_file():
            shutil.copy2(src, lib_dest / name)
    (lib_dest / COMPAT_TARGET_MARKER_NAME).write_text(
        compat_target + "\n",
        encoding="utf-8",
        newline="\n",
    )

    manifest_dest = bundle_staging_root / MOD_MANIFEST_NAME
    if compat_target == latest_compat or not manifest_dest.is_file():
        obj_dir = ritsulib_root / GODOT_MONO_OBJ_PREFIX / configuration
        gen = obj_dir / "mod_manifest.generated.json"
        if gen.is_file():
            shutil.copy2(gen, manifest_dest)
        copy_viewer_dist_to(bundle_staging_root, ritsulib_root=ritsulib_root)


def finalize_bundle_manifest(
    bundle_staging_root: Path,
    *,
    min_game_version: str,
) -> None:
    manifest_path = bundle_staging_root / MOD_MANIFEST_NAME
    if not manifest_path.is_file():
        msg = f"bundle staging missing {MOD_MANIFEST_NAME}: {manifest_path}"
        raise RuntimeError(msg)
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as e:
        msg = f"Invalid bundle manifest JSON: {manifest_path}: {e}"
        raise RuntimeError(msg) from e
    if not isinstance(manifest, dict):
        msg = f"Bundle manifest root must be a JSON object: {manifest_path}"
        raise RuntimeError(msg)
    manifest["name"] = "RitsuLib"
    manifest["min_game_version"] = min_game_version
    manifest_path.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    write_bundle_variant_manifest(bundle_staging_root)


def write_bundle_variant_manifest(bundle_staging_root: Path) -> None:
    lib_root = bundle_staging_root / "lib"
    if not lib_root.is_dir():
        msg = f"bundle staging missing lib variants under {lib_root}"
        raise RuntimeError(msg)

    variants: list[dict[str, str]] = []
    for lib_dir in sorted((p for p in lib_root.iterdir() if p.is_dir()), key=lambda p: _compat_version_key(p.name)):
        compat_target = lib_dir.name
        marker = lib_dir / COMPAT_TARGET_MARKER_NAME
        dll = lib_dir / ritsulib_built_dll_name()
        if not marker.is_file():
            msg = f"bundle variant missing {COMPAT_TARGET_MARKER_NAME}: {marker}"
            raise RuntimeError(msg)
        if marker.read_text(encoding="utf-8").strip() != compat_target:
            msg = f"bundle variant marker does not match directory: {marker}"
            raise RuntimeError(msg)
        if not dll.is_file():
            msg = f"bundle variant missing DLL: {dll}"
            raise RuntimeError(msg)
        variants.append(
            {
                "compatTarget": compat_target,
                "directory": f"lib/{compat_target}",
                "assembly": ritsulib_built_dll_name(),
                "sha256": _sha256_file(dll),
            }
        )

    if not variants:
        msg = f"bundle staging missing lib variants under {lib_root}"
        raise RuntimeError(msg)

    manifest = {"schema": 1, "variants": variants}
    (bundle_staging_root / VARIANT_MANIFEST_NAME).write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def _sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def copy_viewer_dist_to(dest_root: Path, *, ritsulib_root: Path) -> None:
    viewer_dist = ritsulib_root / VIEWER_DIST_REL
    if not (viewer_dist / "index.html").is_file():
        return

    dest = dest_root / VIEWER_OUTPUT_DIR_NAME
    if dest.exists():
        shutil.rmtree(dest)
    shutil.copytree(viewer_dist, dest)


def write_viewer_dist_to_zip(zf: zipfile.ZipFile, *, ritsulib_root: Path) -> None:
    viewer_dist = ritsulib_root / VIEWER_DIST_REL
    if not (viewer_dist / "index.html").is_file():
        return

    for path in viewer_dist.rglob("*"):
        if path.is_file():
            arc = Path(VIEWER_OUTPUT_DIR_NAME) / path.relative_to(viewer_dist)
            zf.write(path, arcname=arc.as_posix())


def lowest_compat_target(compat_targets: list[str]) -> str:
    if not compat_targets:
        msg = "compat_targets must not be empty."
        raise RuntimeError(msg)
    return min(compat_targets, key=_compat_version_key)


def _compat_version_key(value: str) -> tuple[int, ...]:
    try:
        return tuple(int(part) for part in value.split("."))
    except ValueError as e:
        msg = f"Invalid compat target version: {value!r}"
        raise RuntimeError(msg) from e


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
        "/p:IncludeSymbols=true",
        "/p:SymbolPackageFormat=snupkg",
        f"/p:Sts2ApiCompat={compat_target}",
    ]
    if version_override is not None and version_override.strip():
        override = version_override.strip()
        args.append(f"/p:Version={override}")
        args.append(f"/p:PackageVersion={override}")
        args.append(f"/p:AssemblyInformationalVersion={override}")
        args.append("/p:RitsuLibTelemetryBuildChannel=dev")
    else:
        args.append("/p:RitsuLibTelemetryBuildChannel=release")
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
    bundle_staging_root: Path | None = None,
) -> tuple[list[Path], list[Path]]:
    artifacts_dir = ritsulib_root / ARTIFACTS_NUGET
    github_dir = ritsulib_root / ARTIFACTS_GITHUB
    key = _resolve_api_key(api_key)
    published: list[Path] = []
    zips: list[Path] = []
    latest_compat: str | None = None
    if bundle_staging_root is not None:
        prepare_bundle_staging(bundle_staging_root)
        csproj_path = ritsulib_root / RITSULIB_CSPROJ_NAME
        try:
            latest_compat = get_csproj_property(csproj_path, "RitsuLibLatestApiCompat").strip() or None
        except (OSError, RuntimeError):
            latest_compat = None
        if not latest_compat:
            msg = "bundle_staging_root set but RitsuLibLatestApiCompat could not be evaluated."
            raise RuntimeError(msg)

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
        if bundle_staging_root is not None and latest_compat is not None:
            snapshot_bundle_variant_after_pack(
                ritsulib_root,
                configuration=configuration,
                compat_target=compat_target,
                bundle_staging_root=bundle_staging_root,
                latest_compat=latest_compat,
            )
        run_push(package, source=source, api_key=key)
        symbol_package = package.with_suffix(SNUPKG_SUFFIX)
        if symbol_package.is_file():
            run_push(symbol_package, source=source, api_key=key)
        published.append(package)
        zips.append(zip_path)
    if bundle_staging_root is not None:
        finalize_bundle_manifest(
            bundle_staging_root,
            min_game_version=lowest_compat_target(compat_targets),
        )
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
    bundle_staging_root: Path | None = None,
) -> tuple[list[Path], list[Path]]:
    artifacts_dir = ritsulib_root / ARTIFACTS_NUGET
    github_dir = ritsulib_root / ARTIFACTS_GITHUB
    packages: list[Path] = []
    zips: list[Path] = []
    latest_compat: str | None = None
    if bundle_staging_root is not None:
        prepare_bundle_staging(bundle_staging_root)
        csproj_path = ritsulib_root / RITSULIB_CSPROJ_NAME
        try:
            latest_compat = get_csproj_property(csproj_path, "RitsuLibLatestApiCompat").strip() or None
        except (OSError, RuntimeError):
            latest_compat = None
        if not latest_compat:
            msg = "bundle_staging_root set but RitsuLibLatestApiCompat could not be evaluated."
            raise RuntimeError(msg)

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
        if bundle_staging_root is not None and latest_compat is not None:
            snapshot_bundle_variant_after_pack(
                ritsulib_root,
                configuration=configuration,
                compat_target=compat_target,
                bundle_staging_root=bundle_staging_root,
                latest_compat=latest_compat,
            )
        packages.append(package)
        zips.append(zip_path)
    if bundle_staging_root is not None:
        finalize_bundle_manifest(
            bundle_staging_root,
            min_game_version=lowest_compat_target(compat_targets),
        )
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
        bundle_staging_root=None,
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
    out_bin = ritsulib_root / GODOT_MONO_BIN_PREFIX / configuration
    out_obj = ritsulib_root / GODOT_MONO_OBJ_PREFIX / configuration
    dll_path = out_bin / ritsulib_built_dll_name()
    doc_xml_path = out_bin / ritsulib_built_doc_xml_name()
    pdb_path = out_bin / ritsulib_built_pdb_name()
    manifest_generated = out_obj / "mod_manifest.generated.json"
    manifest_path = manifest_generated if manifest_generated.is_file() else (ritsulib_root / MOD_MANIFEST_NAME)
    if not dll_path.is_file():
        msg = f"Could not find built DLL for zip packaging: {dll_path}"
        raise RuntimeError(msg)
    if not doc_xml_path.is_file():
        msg = (
            f"Could not find C# API documentation XML for zip packaging: {doc_xml_path}. "
            "Build with GenerateDocumentationFile (see STS2-RitsuLib.csproj)."
        )
        raise RuntimeError(msg)
    if not manifest_path.is_file():
        msg = f"Could not find mod_manifest.json for zip packaging: {manifest_path}"
        raise RuntimeError(msg)

    zip_name = f"{package.stem}{GITHUB_ZIP_FILENAME_SUFFIX}"
    zip_path = output_dir / zip_name
    with zipfile.ZipFile(zip_path, mode="w", compression=zipfile.ZIP_DEFLATED) as zf:
        zf.write(dll_path, arcname=ritsulib_built_dll_name())
        zf.write(doc_xml_path, arcname=ritsulib_built_doc_xml_name())
        if pdb_path.is_file():
            zf.write(pdb_path, arcname=ritsulib_built_pdb_name())
        zf.write(manifest_path, arcname="mod_manifest.json")
        zf.writestr("compat-target.txt", compat_target + "\n")
        write_viewer_dist_to_zip(zf, ritsulib_root=ritsulib_root)
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
