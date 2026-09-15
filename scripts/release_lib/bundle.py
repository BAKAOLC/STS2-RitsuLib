from __future__ import annotations

import shutil
import zipfile
from pathlib import Path

from release_lib.artifact_validation import validate_github_zip_viewer
from release_lib.repo_layout import (
    github_artifacts,
    GITHUB_BUNDLE_ZIP_SUFFIX,
    MOD_MANIFEST_NAME,
    RITSULIB_LOADER_DIR_REL,
    RITSULIB_LOADER_CSPROJ_REL,
)
from release_lib.runtime_layout import validate_runtime_directory


def compose_bundle_zip(
    ritsulib_root: Path,
    *,
    configuration: str,
    effective_version: str,
    sts2_api_signature_root: Path | None,
    sts2_dir: Path | None,
    bundle_staging_root: Path,
) -> Path:
    """Archive the validated modules already produced by the compatibility build pipeline."""
    validate_runtime_directory(bundle_staging_root)

    safe_ver = (
        effective_version.strip().replace("+", "-").replace("/", "-").replace("\\", "-")
    )
    out_dir = ritsulib_root / github_artifacts(configuration)
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
