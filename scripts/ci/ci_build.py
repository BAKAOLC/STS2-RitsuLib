from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path

_scripts_dir = Path(__file__).resolve().parent.parent
if str(_scripts_dir) not in sys.path:
    sys.path.insert(0, str(_scripts_dir))

from release_lib.repo_layout import DEV_PACKAGE_VERSION_PREFIX, dev_package_version

from verify_signatures import verify_signature_tree


def _dev_build_version() -> str:
    return dev_package_version(
        run_id=(os.environ.get("GITHUB_RUN_ID", "") or "0").strip(),
        sha=os.environ.get("GITHUB_SHA", "").strip(),
    )


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(
        description="CI: verify signatures, run release_cli --artifacts-only with explicit MSBuild overrides.",
    )
    p.add_argument("--repo-root", type=Path, default=Path("."))
    p.add_argument("--signature-root", type=Path, required=True)
    p.add_argument("--configuration", default="Release")
    p.add_argument("--compat-targets", default="all")
    p.add_argument(
        "--use-dev-version",
        action="store_true",
        help=f"Pack artifacts with dev-only version {DEV_PACKAGE_VERSION_PREFIX}.<run>+<sha>.",
    )
    args = p.parse_args(argv)
    repo = args.repo_root.resolve()
    sig = args.signature_root.resolve()
    verify_signature_tree(repo_root=repo, signature_root=sig)
    release_cli = repo / "scripts" / "release_cli.py"
    ci_sts2_dir = repo / ".ci-sts2-dir"
    ci_sts2_dir.mkdir(parents=True, exist_ok=True)
    try:
        cmd = [
            sys.executable,
            str(release_cli),
            "--artifacts-only",
            "--compat-targets",
            args.compat_targets,
            "--configuration",
            args.configuration,
            "--sts2-api-signature-root",
            str(sig),
            "--sts2-dir",
            str(ci_sts2_dir),
        ]
        if args.use_dev_version:
            cmd.extend(["--version-override", _dev_build_version()])
        subprocess.run(cmd, cwd=repo, check=True)
    finally:
        shutil.rmtree(ci_sts2_dir, ignore_errors=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
