from __future__ import annotations

from pathlib import Path

RITSULIB_CSPROJ_NAME = "STS2-RitsuLib.csproj"
MOD_MANIFEST_NAME = "mod_manifest.json"
CONST_CS_NAME = "Const.cs"

DEV_PACKAGE_VERSION_PREFIX = "9999.0.0-dev"

NUGET_ORG_V3_INDEX_URL = "https://api.nuget.org/v3/index.json"

ARTIFACTS_NUGET = Path("artifacts") / "nuget"
ARTIFACTS_GITHUB = Path("artifacts") / "github"

GITHUB_ZIP_FILENAME_SUFFIX = ".github.zip"

SNUPKG_SUFFIX = ".snupkg"

GITHUB_PRERELEASE_TAG_NAME = "dev-build"

RITSLIB_RELEASE_TAG_MESSAGE_BASENAME = "ritsulib-release-tag-message.md"

DEFAULT_GIT_REMOTE = "origin"

GIT_DEFAULT_DEV_BRANCH = "dev"
GIT_DEFAULT_MAIN_BRANCH = "main"

SIGNATURE_EXPECTED_DLL_NAMES = ("sts2.dll", "0Harmony.dll", "Steamworks.NET.dll")

GODOT_MONO_BIN_PREFIX = Path(".godot") / "mono" / "temp" / "bin"


def dev_package_version(*, run_id: str, sha: str) -> str:
    short = sha[:8] if sha else "local"
    run = run_id if run_id else "0"
    return f"{DEV_PACKAGE_VERSION_PREFIX}.{run}+{short}"


def ritsulib_csproj(repo_root: Path) -> Path:
    return (repo_root / RITSULIB_CSPROJ_NAME).resolve()


def ritsulib_built_dll_name() -> str:
    return Path(RITSULIB_CSPROJ_NAME).stem + ".dll"
