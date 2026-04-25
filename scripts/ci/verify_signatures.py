from __future__ import annotations

import argparse
import sys
from pathlib import Path

_scripts_dir = Path(__file__).resolve().parent.parent
if str(_scripts_dir) not in sys.path:
    sys.path.insert(0, str(_scripts_dir))

from release_lib.msbuild_eval import get_csproj_property
from release_lib.repo_layout import RITSULIB_CSPROJ_NAME, SIGNATURE_EXPECTED_DLL_NAMES


def read_compat_targets(repo_root: Path) -> list[str]:
    csproj = (repo_root / RITSULIB_CSPROJ_NAME).resolve()
    if not csproj.is_file():
        print(f"Missing csproj: {csproj}", file=sys.stderr)
        raise SystemExit(1)
    try:
        raw = get_csproj_property(csproj, "RitsuLibCompatTargets")
    except (OSError, RuntimeError) as e:
        print(f"Could not evaluate MSBuild property RitsuLibCompatTargets: {e}", file=sys.stderr)
        raise SystemExit(1) from e
    if not raw.strip():
        print("RitsuLibCompatTargets evaluated empty (dotnet msbuild -getProperty).", file=sys.stderr)
        raise SystemExit(1)
    return [v.strip() for v in raw.split(";") if v.strip()]


def verify_signature_tree(*, repo_root: Path, signature_root: Path) -> list[str]:
    versions = read_compat_targets(repo_root)
    sig = signature_root.resolve()
    for v in versions:
        for name in SIGNATURE_EXPECTED_DLL_NAMES:
            path = sig / v / name
            if not path.is_file():
                print(f"Missing required file: {path}", file=sys.stderr)
                print(
                    "Ensure the API signatures repo has one folder per compat version with the three DLLs.",
                    file=sys.stderr,
                )
                raise SystemExit(1)
    return versions


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(description="Verify STS2-API-Signatures layout against RitsuLibCompatTargets.")
    p.add_argument("--repo-root", type=Path, default=Path("."))
    p.add_argument("--signature-root", type=Path, required=True)
    args = p.parse_args(argv)
    versions = verify_signature_tree(repo_root=args.repo_root, signature_root=args.signature_root)
    print("Signature layout OK:", ", ".join(versions))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
