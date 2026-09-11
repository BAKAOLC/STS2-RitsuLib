from __future__ import annotations

import hashlib
import json
import re
from collections.abc import Callable, Iterator
from pathlib import Path

from release_lib.repo_layout import MOD_MANIFEST_NAME, VARIANT_MANIFEST_NAME

SHARED_MODULES = ("STS2-RitsuLib.Shared", "STS2-RitsuLib.Ui", "STS2-RitsuLib.Settings", "System.IO.Hashing")
VARIANT_MODULES = ("STS2-RitsuLib.Runtime", "STS2-RitsuLib")


def read_module_manifest(data: bytes) -> dict:
    if len(data) > 1024 * 1024:
        raise RuntimeError("RitsuLib module manifest exceeds 1 MiB.")
    manifest = json.loads(data.decode("utf-8-sig"))
    if not isinstance(manifest, dict) or manifest.get("schema") != 2:
        raise RuntimeError("Unsupported RitsuLib module manifest schema.")
    variants = manifest.get("variants")
    if not isinstance(variants, list) or not 1 <= len(variants) <= 64:
        raise RuntimeError("RitsuLib bundle must contain between 1 and 64 variants.")
    seen = set()
    for variant in variants:
        target = variant.get("compatTarget") if isinstance(variant, dict) else None
        if not isinstance(target, str) or re.fullmatch(r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)", target) is None or target in seen:
            raise RuntimeError(f"Invalid or duplicate RitsuLib compatibility target: {target!r}")
        seen.add(target)
    list(module_entries(manifest))
    return manifest


def module_entries(manifest: dict) -> Iterator[tuple[str, str]]:
    def entries(directory: str, files: object, expected: tuple[str, ...]) -> Iterator[tuple[str, str]]:
        if not isinstance(files, list) or len(files) != len(expected):
            raise RuntimeError(f"Unexpected module count in {directory}.")
        seen = set()
        for entry in files:
            name = entry.get("assembly") if isinstance(entry, dict) else None
            if not isinstance(name, str) or name not in expected or name in seen:
                raise RuntimeError(f"Unexpected or duplicate module in {directory}: {name!r}")
            seen.add(name)
            digest = entry.get("sha256")
            if not isinstance(digest, str) or re.fullmatch(r"[a-fA-F0-9]{64}", digest) is None:
                raise RuntimeError(f"Invalid module hash: {name}")
            yield f"{directory}/{name}.dll", digest.lower()

    yield from entries("shared", manifest.get("shared"), SHARED_MODULES)
    for variant in manifest["variants"]:
        yield from entries(f"compat/{variant['compatTarget']}", variant.get("files"), VARIANT_MODULES)


def validate_runtime_payload(read_file: Callable[[str], bytes]) -> dict:
    manifest = read_module_manifest(read_file(VARIANT_MANIFEST_NAME))
    mod_manifest = json.loads(read_file(MOD_MANIFEST_NAME).decode("utf-8-sig"))
    if mod_manifest.get("id") != "STS2-RitsuLib" or mod_manifest.get("has_dll") is not True:
        raise RuntimeError("Invalid RitsuLib mod manifest.")
    if not read_file("STS2-RitsuLib.dll").startswith(b"MZ"):
        raise RuntimeError("Missing or invalid RitsuLib loader assembly.")
    if not read_file("viewer/index.html"):
        raise RuntimeError("The debug log viewer is empty.")
    if not read_file("RitsuLib.References.props"):
        raise RuntimeError("The directory-reference entry point is missing.")
    for path, digest in module_entries(manifest):
        payload = read_file(path)
        if not payload.startswith(b"MZ") or hashlib.sha256(payload).hexdigest() != digest:
            raise RuntimeError(f"RitsuLib module hash mismatch or invalid assembly: {path}")
        if Path(path).name.startswith("STS2-RitsuLib"):
            if not read_file(str(Path(path).with_suffix(".xml")).replace("\\", "/")):
                raise RuntimeError(f"Missing API documentation for {path}")
    return manifest


def validate_runtime_directory(root: Path) -> dict:
    return validate_runtime_payload(lambda path: (root / path).read_bytes())


def write_module_manifest(root: Path) -> None:
    def files(directory: Path, names: tuple[str, ...]) -> list[dict[str, str]]:
        return [{"assembly": name, "sha256": hashlib.sha256((directory / f"{name}.dll").read_bytes()).hexdigest()} for name in names]

    variants = []
    for directory in sorted((root / "compat").iterdir(), key=lambda path: tuple(int(part) for part in path.name.split("."))):
        if not directory.is_dir():
            continue
        variants.append({"compatTarget": directory.name, "files": files(directory, VARIANT_MODULES)})
    manifest = {"schema": 2, "shared": files(root / "shared", SHARED_MODULES), "variants": variants}
    payload = (json.dumps(manifest, indent=2) + "\n").encode("utf-8")
    read_module_manifest(payload)
    (root / VARIANT_MANIFEST_NAME).write_bytes(payload)
