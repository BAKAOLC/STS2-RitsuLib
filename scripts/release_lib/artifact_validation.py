from __future__ import annotations

import zipfile
from pathlib import Path

NUGET_VIEWER_INDEX_ENTRY = "contentFiles/any/any/viewer/index.html"
ZIP_VIEWER_INDEX_ENTRY = "viewer/index.html"


def validate_nuget_viewer(package: Path) -> None:
    _require_zip_entry(package, NUGET_VIEWER_INDEX_ENTRY)


def validate_github_zip_viewer(zip_path: Path) -> None:
    _require_zip_entry(zip_path, ZIP_VIEWER_INDEX_ENTRY)


def validate_viewer_artifacts(
    *,
    packages: list[Path] | tuple[Path, ...] = (),
    zips: list[Path] | tuple[Path, ...] = (),
) -> None:
    for package in packages:
        validate_nuget_viewer(package)
    for zip_path in zips:
        validate_github_zip_viewer(zip_path)


def _require_zip_entry(path: Path, entry: str) -> None:
    if not path.is_file():
        msg = f"Missing artifact: {path}"
        raise RuntimeError(msg)
    try:
        with zipfile.ZipFile(path) as zf:
            names = {info.filename for info in zf.infolist() if not info.is_dir()}
    except zipfile.BadZipFile as e:
        msg = f"Invalid zip artifact: {path}"
        raise RuntimeError(msg) from e
    if entry not in names:
        msg = f"{path.name} is missing required viewer asset: {entry}"
        raise RuntimeError(msg)
