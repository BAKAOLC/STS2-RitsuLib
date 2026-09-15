from __future__ import annotations

import zipfile
from pathlib import Path

from release_lib.runtime_layout import validate_runtime_payload

NUGET_CONTENT_ROOT = "contentFiles/any/any/"


def validate_nuget_viewer(package: Path) -> None:
    with _open_artifact(package) as archive:
        def read_file(path: str) -> bytes:
            if path == "STS2-RitsuLib.dll":
                return archive.read(NUGET_CONTENT_ROOT + "loader/STS2-RitsuLib.dll")
            if path.startswith("shared/") or path.startswith("compat/"):
                filename = path.rsplit("/", 1)[-1]
                if filename.startswith("STS2-RitsuLib"):
                    return archive.read("lib/net9.0/" + filename)
            return archive.read(NUGET_CONTENT_ROOT + path)
        validate_runtime_payload(read_file)
        expected_dlls = {
            "lib/net9.0/STS2-RitsuLib.dll",
            "lib/net9.0/STS2-RitsuLib.Runtime.dll",
            "lib/net9.0/STS2-RitsuLib.Shared.dll",
            "lib/net9.0/STS2-RitsuLib.Ui.dll",
            "lib/net9.0/STS2-RitsuLib.Settings.dll",
            NUGET_CONTENT_ROOT + "loader/STS2-RitsuLib.dll",
            NUGET_CONTENT_ROOT + "shared/System.IO.Hashing.dll",
        }
        if {name for name in archive.namelist() if name.endswith(".dll")} != expected_dlls:
            raise RuntimeError(f"Unexpected assembly layout in {package.name}.")
        targets = [name for name in archive.namelist() if name.startswith("buildTransitive/") and name.endswith(".targets")]
        if len(targets) != 1:
            raise RuntimeError(f"{package.name} must contain exactly one deployment targets file.")
    symbols = package.with_suffix(".snupkg")
    with _open_artifact(symbols) as archive:
        expected_pdbs = {name.removesuffix(".dll") + ".pdb" for name in expected_dlls if name.startswith("lib/")}
        if {name for name in archive.namelist() if name.endswith(".pdb")} != expected_pdbs:
            raise RuntimeError(f"Unexpected module symbols in {symbols.name}.")
        for name in expected_pdbs:
            if not archive.read(name).startswith(b"BSJB"):
                raise RuntimeError(f"Invalid portable PDB: {symbols.name}/{name}")


def validate_github_zip_viewer(zip_path: Path) -> None:
    with _open_artifact(zip_path) as archive:
        validate_runtime_payload(archive.read)


def validate_viewer_artifacts(
    *,
    packages: list[Path] | tuple[Path, ...] = (),
    zips: list[Path] | tuple[Path, ...] = (),
) -> None:
    for package in packages:
        validate_nuget_viewer(package)
    for zip_path in zips:
        validate_github_zip_viewer(zip_path)


def _open_artifact(path: Path) -> zipfile.ZipFile:
    if not path.is_file():
        raise RuntimeError(f"Missing artifact: {path}")
    archive = zipfile.ZipFile(path)
    names = archive.namelist()
    if len(names) != len(set(names)):
        archive.close()
        raise RuntimeError(f"Duplicate entries in artifact: {path}")
    return archive
