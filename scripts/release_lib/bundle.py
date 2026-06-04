from __future__ import annotations

import shutil
import subprocess
import zipfile
from pathlib import Path

from release_lib.artifact_validation import validate_github_zip_viewer
from release_lib.repo_layout import (
    ARTIFACTS_GITHUB,
    GITHUB_BUNDLE_ZIP_SUFFIX,
    MOD_MANIFEST_NAME,
    RITSULIB_LOADER_CSPROJ_REL,
)
from release_lib.nuget import copy_viewer_dist_to


def compose_bundle_zip(
    ritsulib_root: Path,
    *,
    configuration: str,
    effective_version: str,
    sts2_api_signature_root: Path | None,
    sts2_dir: Path | None,
    bundle_staging_root: Path,
) -> Path:
    """Build the loader, place it as ``STS2-RitsuLib.dll`` on staging root, then zip staging for GitHub."""
    manifest = bundle_staging_root / MOD_MANIFEST_NAME
    if not manifest.is_file():
        msg = f"bundle staging missing {MOD_MANIFEST_NAME}: {manifest}"
        raise RuntimeError(msg)

    lib_root = bundle_staging_root / "lib"
    if not lib_root.is_dir() or not any(lib_root.iterdir()):
        msg = f"bundle staging missing lib variants under {lib_root}"
        raise RuntimeError(msg)

    loader_csproj = ritsulib_root / RITSULIB_LOADER_CSPROJ_REL
    if not loader_csproj.is_file():
        msg = f"Missing loader project: {loader_csproj}"
        raise RuntimeError(msg)

    cmd: list[str] = [
        "dotnet",
        "build",
        str(loader_csproj),
        "-c",
        configuration,
        "-v",
        "q",
    ]
    if sts2_api_signature_root is not None:
        cmd.append(f"/p:Sts2ApiSignatureRoot={sts2_api_signature_root}")
    if sts2_dir is not None:
        cmd.append(f"/p:Sts2Dir={sts2_dir}")

    subprocess.run(cmd, cwd=ritsulib_root, check=True)

    loader_out = ritsulib_root / "Loader" / "bin" / configuration / "net9.0"
    loader_dll = loader_out / "STS2-RitsuLib.Loader.dll"
    loader_pdb = loader_out / "STS2-RitsuLib.Loader.pdb"
    if not loader_dll.is_file():
        msg = f"Loader build did not produce {loader_dll}"
        raise RuntimeError(msg)

    shutil.copy2(loader_dll, bundle_staging_root / "STS2-RitsuLib.dll")
    if loader_pdb.is_file():
        shutil.copy2(loader_pdb, bundle_staging_root / "STS2-RitsuLib.Loader.pdb")

    copy_viewer_dist_to(bundle_staging_root, ritsulib_root=ritsulib_root)

    safe_ver = (
        effective_version.strip().replace("+", "-").replace("/", "-").replace("\\", "-")
    )
    out_dir = ritsulib_root / ARTIFACTS_GITHUB
    out_dir.mkdir(parents=True, exist_ok=True)
    zip_path = out_dir / f"STS2-RitsuLib.{safe_ver}{GITHUB_BUNDLE_ZIP_SUFFIX}"

    if zip_path.is_file():
        zip_path.unlink()

    with zipfile.ZipFile(zip_path, mode="w", compression=zipfile.ZIP_DEFLATED) as zf:
        for path in bundle_staging_root.rglob("*"):
            if path.is_file():
                arc = path.relative_to(bundle_staging_root).as_posix()
                zf.write(path, arcname=arc)

    validate_github_zip_viewer(zip_path)
    return zip_path
