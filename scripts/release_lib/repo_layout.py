from __future__ import annotations

from pathlib import Path

RITSULIB_CSPROJ_NAME = "STS2-RitsuLib.csproj"
MOD_MANIFEST_NAME = "mod_manifest.json"
VARIANT_MANIFEST_NAME = "ritsulib-variants.manifest"
COMPAT_TARGET_MARKER_NAME = "compat-target.txt"
CONST_CS_NAME = "Const.cs"
CONST_CS_REL = Path("src") / CONST_CS_NAME

DEV_PACKAGE_VERSION_PREFIX = "9999.0.0-dev"

NUGET_ORG_V3_INDEX_URL = "https://api.nuget.org/v3/index.json"

def validate_configuration(configuration: str) -> str:
    if configuration not in ("Debug", "Release"):
        raise ValueError("RitsuLib configuration must be Debug or Release.")
    return configuration


def nuget_artifacts(configuration: str = "Release") -> Path:
    return Path("artifacts") / "packages" / validate_configuration(configuration) / "nuget"


def github_artifacts(configuration: str = "Release") -> Path:
    return Path("artifacts") / "packages" / validate_configuration(configuration) / "github"


def bundle_staging(configuration: str = "Release") -> Path:
    return Path("artifacts") / "bundle" / validate_configuration(configuration)


def runtime_directory(repo: Path, configuration: str, compat_target: str) -> Path:
    return repo / "artifacts" / "runtime" / validate_configuration(configuration) / compat_target

RITSULIB_LOADER_DIR_REL = Path("components") / "loader"
RITSULIB_LOADER_CSPROJ_REL = RITSULIB_LOADER_DIR_REL / "STS2-RitsuLib-Loader.csproj"

VIEWER_DIST_REL = Path("components") / "viewer" / "dist"
VIEWER_OUTPUT_DIR_NAME = "viewer"

GITHUB_ZIP_FILENAME_SUFFIX = ".github.zip"

GITHUB_BUNDLE_ZIP_SUFFIX = ".variant-pack.zip"

SNUPKG_SUFFIX = ".snupkg"

GITHUB_PRERELEASE_TAG_NAME = "dev-build"

RITSLIB_RELEASE_TAG_MESSAGE_BASENAME = "ritsulib-release-tag-message.md"

DEFAULT_GIT_REMOTE = "origin"

GIT_DEFAULT_DEV_BRANCH = "dev"
GIT_DEFAULT_MAIN_BRANCH = "main"

SIGNATURE_EXPECTED_DLL_NAMES = ("sts2.dll", "0Harmony.dll", "SmartFormat.dll")



def dev_package_version(*, run_id: str, sha: str) -> str:
    short = sha[:8] if sha else "local"
    run = run_id if run_id else "0"
    return f"{DEV_PACKAGE_VERSION_PREFIX}.{run}+{short}"


def ritsulib_csproj(repo_root: Path) -> Path:
    return (repo_root / RITSULIB_CSPROJ_NAME).resolve()


def ritsulib_built_assembly_stem() -> str:
    return Path(RITSULIB_CSPROJ_NAME).stem


def ritsulib_built_dll_name() -> str:
    return ritsulib_built_assembly_stem() + ".dll"


def ritsulib_built_doc_xml_name() -> str:
    """C# XML documentation file from GenerateDocumentationFile (assembly API comments)."""
    return ritsulib_built_assembly_stem() + ".xml"


def ritsulib_built_pdb_name() -> str:
    return ritsulib_built_assembly_stem() + ".pdb"
