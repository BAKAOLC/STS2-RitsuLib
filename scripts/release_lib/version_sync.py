from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path

from release_lib.msbuild_eval import get_csproj_property
from release_lib.repo_layout import CONST_CS_NAME, MOD_MANIFEST_NAME, RITSULIB_CSPROJ_NAME


@dataclass(frozen=True)
class VersionTriple:
    major: int
    minor: int
    patch: int

    def __str__(self) -> str:
        return f"{self.major}.{self.minor}.{self.patch}"

    @classmethod
    def parse(cls, text: str) -> VersionTriple:
        parts = text.strip().split(".")
        if len(parts) != 3:
            msg = f"Invalid version format: {text!r}. Use X.Y.Z"
            raise ValueError(msg)
        try:
            return cls(int(parts[0]), int(parts[1]), int(parts[2]))
        except ValueError as e:
            msg = f"Invalid version format: {text!r}. Use X.Y.Z"
            raise ValueError(msg) from e


def read_csproj_version(path: Path) -> str:
    try:
        raw = get_csproj_property(path, "Version")
    except (OSError, RuntimeError) as e:
        msg = f"Could not read Version from {path}: {e}"
        raise ValueError(msg) from e
    if not raw.strip():
        msg = f"Version evaluated empty for {path}"
        raise ValueError(msg)
    return raw.strip()


def read_paths(ritsulib_root: Path) -> tuple[Path, Path, Path]:
    csproj = ritsulib_root / RITSULIB_CSPROJ_NAME
    manifest = ritsulib_root / MOD_MANIFEST_NAME
    const_cs = ritsulib_root / CONST_CS_NAME
    for p in (csproj, manifest, const_cs):
        if not p.is_file():
            msg = f"Required file not found: {p}"
            raise FileNotFoundError(msg)
    return csproj, manifest, const_cs


def resolve_next_version(
    current: VersionTriple,
    *,
    bump: str,
    explicit: str | None,
) -> VersionTriple:
    if explicit:
        return VersionTriple.parse(explicit)
    if bump == "major":
        return VersionTriple(current.major + 1, 0, 0)
    if bump == "minor":
        return VersionTriple(current.major, current.minor + 1, 0)
    if bump == "patch":
        return VersionTriple(current.major, current.minor, current.patch + 1)
    if bump == "none":
        return current
    msg = f"Unknown bump: {bump}"
    raise ValueError(msg)


def write_version_files(
    csproj: Path,
    manifest: Path,
    const_cs: Path,
    new_version: str,
) -> None:
    _patch_csproj(csproj, new_version)
    _patch_manifest(manifest, new_version)
    _patch_const(const_cs, new_version)


def _patch_csproj(path: Path, new_version: str) -> None:
    content = path.read_text(encoding="utf-8")
    pattern = r"(<Version>\s*)([^<]+?)(\s*</Version>)"
    if not re.search(pattern, content):
        msg = f"Failed to locate <Version> in {path}"
        raise ValueError(msg)
    updated = re.sub(
        pattern,
        lambda m: m.group(1) + new_version + m.group(3),
        content,
        count=1,
    )
    if updated != content:
        path.write_text(updated, encoding="utf-8", newline="\n")


def _patch_manifest(path: Path, new_version: str) -> None:
    content = path.read_text(encoding="utf-8")
    pattern = r'"version"\s*:\s*"[^"]+"'
    if not re.search(pattern, content):
        msg = f'Failed to locate "version" field in {path}'
        raise ValueError(msg)
    updated = re.sub(pattern, f'"version": "{new_version}"', content, count=1)
    if updated != content:
        path.write_text(updated, encoding="utf-8", newline="\n")


def _patch_const(path: Path, new_version: str) -> None:
    content = path.read_text(encoding="utf-8")
    pattern = r'public\s+const\s+string\s+Version\s*=\s*"[^"]+";'
    if not re.search(pattern, content):
        msg = f"Failed to locate Const.Version in {path}"
        raise ValueError(msg)
    replacement = f'public const string Version = "{new_version}";'
    updated = re.sub(pattern, replacement, content, count=1)
    if updated != content:
        path.write_text(updated, encoding="utf-8", newline="\n")
