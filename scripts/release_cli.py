from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path

from release_lib import git_ops
from release_lib import nuget as nuget_ops
from release_lib.version_sync import (
    read_csproj_version,
    read_paths,
    resolve_next_version,
    write_version_files,
    VersionTriple,
)

_SCRIPTS_DIR = Path(__file__).resolve().parent
DEFAULT_DEV_BRANCH = "dev"
DEFAULT_MAIN_BRANCH = "main"
DEFAULT_REMOTE = "origin"
DEFAULT_NUGET_SOURCE = "https://api.nuget.org/v3/index.json"

_VERSIONED_FILES = (
    "STS2-RitsuLib.csproj",
    "mod_manifest.json",
    "Const.cs",
)


def _parse_args(argv: list[str]) -> argparse.Namespace:
    p = argparse.ArgumentParser(
        description="STS2-RitsuLib release: dev → version bump → merge main → tag → push → NuGet",
    )
    p.add_argument(
        "--bump",
        choices=("major", "minor", "patch", "none"),
        default="patch",
        help="Semantic bump for X.Y.Z when --version is omitted (default: patch)",
    )
    p.add_argument(
        "--version",
        dest="explicit_version",
        metavar="X.Y.Z",
        help="Set exact version instead of bumping",
    )
    p.add_argument("--dev-branch", default=os.environ.get("RELEASE_DEV_BRANCH", DEFAULT_DEV_BRANCH))
    p.add_argument("--main-branch", default=os.environ.get("RELEASE_MAIN_BRANCH", DEFAULT_MAIN_BRANCH))
    p.add_argument("--remote", default=os.environ.get("RELEASE_REMOTE", DEFAULT_REMOTE))
    p.add_argument("--dry-run", action="store_true", help="Print plan only; no file or git changes")
    p.add_argument(
        "--dry-run-verify-pack",
        action="store_true",
        help="With --dry-run: run dotnet pack to a temp dir only (no repo writes, temp cleaned)",
    )
    p.add_argument("--no-pull", action="store_true", help="Skip git pull on dev/main before merge")
    p.add_argument("--skip-nuget", action="store_true", help="Do not pack/push NuGet after push")
    p.add_argument("--configuration", default="Release")
    p.add_argument("--nuget-source", default=DEFAULT_NUGET_SOURCE)
    p.add_argument("--api-key", default=None, help="NuGet API key (else env NUGET_API_KEY)")
    p.add_argument("--skip-build", action="store_true", help="Pass --no-build to dotnet pack")
    return p.parse_args(argv)


def _commit_message_bump(v: str) -> str:
    return f"chore(release): bump version to {v}"


def _commit_message_merge(dev: str, main: str, v: str) -> str:
    return f"chore(release): merge {dev} into {main} for v{v}"


def _tag_name(v: str) -> str:
    return f"v{v}"


def main(argv: list[str] | None = None) -> int:
    args = _parse_args(argv or sys.argv[1:])

    try:
        repo = git_ops.git_root(_SCRIPTS_DIR)
    except RuntimeError as e:
        if args.dry_run:
            repo = _SCRIPTS_DIR.parent
            print(f"[release] warning: {e}", file=sys.stderr)
            print("[release] dry-run: using project root for paths only", file=sys.stderr)
        else:
            print(f"[release] {e}", file=sys.stderr)
            return 1

    if not (repo / "STS2-RitsuLib.csproj").is_file():
        print(f"[release] STS2-RitsuLib.csproj not found under {repo}", file=sys.stderr)
        return 1

    ritsulib = repo
    csproj, manifest, const_cs = read_paths(ritsulib)
    current_text = read_csproj_version(csproj)
    current_v = VersionTriple.parse(current_text)
    next_v = resolve_next_version(
        current_v,
        bump=args.bump,
        explicit=args.explicit_version,
    )
    next_text = str(next_v)
    tag = _tag_name(next_text)

    print(f"[release] repo root: {repo}")
    print(f"[release] version:   {current_text} -> {next_text}")
    print(f"[release] tag:       {tag}")

    if args.dry_run:
        _dry_run_git_warnings(repo, args.dev_branch)
        print("[release] DRY-RUN: no file writes, git mutations, push, or NuGet push")
        _print_git_plan(args, next_text, tag)
        if args.dry_run_verify_pack:
            print("[release] DRY-RUN: verifying dotnet pack (temp directory)...")
            pkg_name = nuget_ops.verify_pack_in_tempdir(
                ritsulib,
                configuration=args.configuration,
                skip_build=args.skip_build,
            )
            print(f"[release] DRY-RUN: pack OK -> {pkg_name} (temp removed)")
        return 0

    git_ops.require_branch(repo, args.dev_branch)
    git_ops.require_clean_worktree(repo)

    if not args.no_pull:
        subprocess.run(
            ["git", "pull", args.remote, args.dev_branch],
            cwd=repo,
            check=True,
        )

    write_version_files(csproj, manifest, const_cs, next_text)
    subprocess.run(
        ["git", "add", *_VERSIONED_FILES],
        cwd=repo,
        check=True,
    )
    subprocess.run(
        ["git", "commit", "-m", _commit_message_bump(next_text)],
        cwd=repo,
        check=True,
    )

    if not args.no_pull:
        subprocess.run(
            ["git", "fetch", args.remote, args.main_branch],
            cwd=repo,
            check=True,
        )

    subprocess.run(["git", "checkout", args.main_branch], cwd=repo, check=True)
    if not args.no_pull:
        subprocess.run(
            ["git", "pull", args.remote, args.main_branch],
            cwd=repo,
            check=True,
        )

    subprocess.run(
        [
            "git",
            "merge",
            "--no-ff",
            args.dev_branch,
            "-m",
            _commit_message_merge(args.dev_branch, args.main_branch, next_text),
        ],
        cwd=repo,
        check=True,
    )
    subprocess.run(["git", "tag", tag], cwd=repo, check=True)

    subprocess.run(["git", "checkout", args.dev_branch], cwd=repo, check=True)

    subprocess.run(["git", "push", args.remote, args.dev_branch], cwd=repo, check=True)
    subprocess.run(["git", "push", args.remote, args.main_branch], cwd=repo, check=True)
    subprocess.run(["git", "push", args.remote, "refs/tags/" + tag], cwd=repo, check=True)

    if not args.skip_nuget:
        pkg = nuget_ops.publish_nuget(
            ritsulib,
            configuration=args.configuration,
            source=args.nuget_source,
            api_key=args.api_key,
            skip_build=args.skip_build,
        )
        print(f"[release] NuGet published: {pkg.name}")

    print("[release] done.")
    return 0


def _dry_run_git_warnings(repo: Path, dev_branch: str) -> None:
    try:
        br = git_ops.current_branch(repo)
        if br != dev_branch:
            print(
                f"[release] DRY-RUN warning: not on {dev_branch!r} (current {br!r}); "
                "a real release requires the dev branch.",
            )
        if not git_ops.worktree_clean(repo):
            print(
                "[release] DRY-RUN warning: working tree is not clean; "
                "a real release requires a clean tree.",
            )
    except (RuntimeError, subprocess.CalledProcessError):
        print("[release] DRY-RUN: skipped git state checks (no repo or git error)")


def _print_git_plan(args: argparse.Namespace, next_text: str, tag: str) -> None:
    merge_msg = _commit_message_merge(args.dev_branch, args.main_branch, next_text)
    rel = " ".join(_VERSIONED_FILES)
    print("  Planned git steps (not executed):")
    if not args.no_pull:
        print(f"    git pull {args.remote} {args.dev_branch}")
    print(f"    (write) {rel} -> version {next_text}")
    print(f"    git add {rel}")
    print(f'    git commit -m "{_commit_message_bump(next_text)}"')
    if not args.no_pull:
        print(f"    git fetch {args.remote} {args.main_branch}")
    print(f"    git checkout {args.main_branch}")
    if not args.no_pull:
        print(f"    git pull {args.remote} {args.main_branch}")
    print(f'    git merge --no-ff {args.dev_branch} -m "{merge_msg}"')
    print(f"    git tag {tag}")
    print(f"    git checkout {args.dev_branch}")
    print(f"    git push {args.remote} {args.dev_branch}")
    print(f"    git push {args.remote} {args.main_branch}")
    print(f"    git push {args.remote} refs/tags/{tag}")
    if args.skip_nuget:
        print("    (skip NuGet)")
    else:
        print("    dotnet pack + dotnet nuget push")


if __name__ == "__main__":
    raise SystemExit(main())
