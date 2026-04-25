from __future__ import annotations

import argparse
import os
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

from release_lib import git_ops
from release_lib import nuget as nuget_ops
from release_lib import plan_analysis
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
    p.add_argument(
        "--compat-targets",
        default="all",
        help='Compatibility targets to publish, comma-separated (for example "x.y.z,a.b.c"), or "all" (default).',
    )
    p.add_argument("--nuget-source", default=DEFAULT_NUGET_SOURCE)
    p.add_argument("--api-key", default=None, help="NuGet API key (else env NUGET_API_KEY)")
    p.add_argument("--skip-build", action="store_true", help="Pass --no-build to dotnet pack")
    p.add_argument(
        "--force-tag",
        action="store_true",
        help="Allow overwriting an existing release tag (git tag -f; git push --force for the tag only)",
    )
    p.add_argument(
        "--plan-fetch",
        action="store_true",
        help="With --dry-run: run git fetch for dev/main before computing [may conflict] hints from refs",
    )
    return p.parse_args(argv)


def _read_default_compat_targets(csproj_path: Path) -> list[str]:
    root = ET.fromstring(csproj_path.read_text(encoding="utf-8"))
    node = root.find(".//RitsuLibCompatTargets")
    if node is None or not node.text:
        msg = "RitsuLibCompatTargets is missing in STS2-RitsuLib.csproj."
        raise RuntimeError(msg)
    return [v.strip() for v in node.text.split(";") if v.strip()]


def _resolve_compat_targets(raw: str, defaults: list[str]) -> list[str]:
    if raw.strip().lower() == "all":
        return defaults
    return [v.strip() for v in raw.split(",") if v.strip()]


def _commit_message_bump(v: str) -> str:
    return f"chore(release): bump version to {v}"


def _commit_message_merge(dev: str, main: str, v: str) -> str:
    return f"chore(release): merge {dev} into {main} for v{v}"


def _tag_name(v: str) -> str:
    return f"v{v}"


def _preflight_release(
    repo: Path,
    remote: str,
    dev_branch: str,
    main_branch: str,
    tag: str,
    *,
    allow_tag_override: bool,
) -> None:
    if not git_ops.remote_exists(repo, remote):
        raise RuntimeError(
            f"Git remote {remote!r} is not configured. "
            f"Add it with: git remote add {remote} <url>"
        )
    if allow_tag_override:
        try:
            if git_ops.local_tag_exists(repo, tag) or git_ops.remote_tag_exists(
                repo, remote, tag
            ):
                print(
                    "[release] --force-tag: tag exists locally and/or on remote; "
                    "will use git tag -f and force-push the tag ref only.",
                    file=sys.stderr,
                )
        except RuntimeError as e:
            print(f"[release] --force-tag: could not check remote tag ({e})", file=sys.stderr)
    else:
        git_ops.ensure_tag_available(repo, remote, tag)
    for br in (dev_branch, main_branch):
        if not git_ops.remote_head_ref_exists(repo, remote, br):
            raise RuntimeError(
                f"Remote {remote!r} has no branch {br!r}. "
                "Push the branch first or fix --dev-branch / --main-branch."
            )


def main(argv: list[str] | None = None) -> int:
    args = _parse_args(argv or sys.argv[1:])
    repo_is_git = True

    try:
        repo = git_ops.git_root(_SCRIPTS_DIR)
    except RuntimeError as e:
        if args.dry_run:
            repo = _SCRIPTS_DIR.parent
            repo_is_git = False
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
    compat_targets = _resolve_compat_targets(
        args.compat_targets,
        _read_default_compat_targets(csproj),
    )
    if not compat_targets:
        print("[release] no compatibility targets resolved from --compat-targets.", file=sys.stderr)
        return 1
    current_text = read_csproj_version(csproj)
    current_v = VersionTriple.parse(current_text)
    next_v = resolve_next_version(
        current_v,
        bump=args.bump,
        explicit=args.explicit_version,
    )
    next_text = str(next_v)
    tag = _tag_name(next_text)

    print(f"[release] repo root: {repo}", flush=True)
    print(f"[release] version:   {current_text} -> {next_text}", flush=True)
    print(f"[release] tag:       {tag}", flush=True)
    print(f"[release] nuget targets: {', '.join(compat_targets)}", flush=True)

    if args.dry_run:
        _dry_run_git_warnings(repo, args.dev_branch)
        if args.force_tag:
            print(
                "[release] DRY-RUN: --force-tag -> git tag -f; push uses --force for tag ref only.",
                file=sys.stderr,
            )
        else:
            git_ops.report_tag_collision_hints(repo, args.remote, tag)
        if git_ops.remote_exists(repo, args.remote):
            for br in (args.dev_branch, args.main_branch):
                try:
                    if not git_ops.remote_head_ref_exists(repo, args.remote, br):
                        print(
                            f"[release] DRY-RUN warning: remote {args.remote!r} "
                            f"has no branch {br!r}.",
                            file=sys.stderr,
                        )
                except RuntimeError as e:
                    print(f"[release] DRY-RUN warning: {e}", file=sys.stderr)
        else:
            print(
                f"[release] DRY-RUN warning: remote {args.remote!r} is not configured.",
                file=sys.stderr,
            )
        print("[release] DRY-RUN: no file writes, git mutations, push, or NuGet push")
        plan_marks: frozenset[str] | None = None
        if repo_is_git:
            try:
                if args.plan_fetch and git_ops.remote_exists(repo, args.remote):
                    git_ops.fetch_plan_refs(
                        repo,
                        args.remote,
                        args.dev_branch,
                        args.main_branch,
                    )
                elif args.plan_fetch and not git_ops.remote_exists(repo, args.remote):
                    print(
                        "[release] DRY-RUN warning: --plan-fetch skipped (remote not configured).",
                        file=sys.stderr,
                    )
                plan_marks = plan_analysis.compute_conflict_marks(
                    repo,
                    args.remote,
                    args.dev_branch,
                    args.main_branch,
                    tag,
                    force_tag=args.force_tag,
                    no_pull=args.no_pull,
                    skip_nuget=args.skip_nuget,
                )
            except (RuntimeError, subprocess.CalledProcessError) as e:
                print(
                    f"[release] DRY-RUN warning: plan analysis failed ({e}); "
                    "using conservative [may conflict] marks.",
                    file=sys.stderr,
                )
                plan_marks = None
        _print_git_plan(
            args,
            next_text,
            tag,
            compat_targets,
            conflict_marks=plan_marks,
            suggest_plan_fetch=bool(
                plan_marks is not None
                and not args.plan_fetch
                and git_ops.remote_exists(repo, args.remote),
            ),
        )
        if args.dry_run_verify_pack:
            print("[release] DRY-RUN: verifying dotnet pack (temp directory)...")
            pkg_names = nuget_ops.verify_pack_in_tempdir(
                ritsulib,
                configuration=args.configuration,
                skip_build=args.skip_build,
                compat_targets=compat_targets,
            )
            print(f"[release] DRY-RUN: pack OK -> {', '.join(pkg_names)} (temp removed)")
        return 0

    try:
        git_ops.require_branch(repo, args.dev_branch)
        git_ops.require_clean_worktree(repo)
        _preflight_release(
            repo,
            args.remote,
            args.dev_branch,
            args.main_branch,
            tag,
            allow_tag_override=args.force_tag,
        )
    except RuntimeError as e:
        print(f"[release] {e}", file=sys.stderr)
        return 1

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
    tag_cmd = ["git", "tag", tag]
    if args.force_tag:
        tag_cmd.insert(2, "-f")
    subprocess.run(tag_cmd, cwd=repo, check=True)

    subprocess.run(["git", "checkout", args.dev_branch], cwd=repo, check=True)

    subprocess.run(["git", "push", args.remote, args.dev_branch], cwd=repo, check=True)
    subprocess.run(["git", "push", args.remote, args.main_branch], cwd=repo, check=True)
    tag_push = ["git", "push", args.remote, "refs/tags/" + tag]
    if args.force_tag:
        tag_push.insert(2, "--force")
    subprocess.run(tag_push, cwd=repo, check=True)

    if not args.skip_nuget:
        published, github_zips = nuget_ops.publish_nugets(
            ritsulib,
            configuration=args.configuration,
            source=args.nuget_source,
            api_key=args.api_key,
            skip_build=args.skip_build,
            compat_targets=compat_targets,
        )
        print(f"[release] NuGet published: {', '.join(pkg.name for pkg in published)}")
        print(f"[release] GitHub zips: {', '.join(zip_path.name for zip_path in github_zips)}")

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


def _print_git_plan(
    args: argparse.Namespace,
    next_text: str,
    tag: str,
    compat_targets: list[str],
    *,
    conflict_marks: frozenset[str] | None = None,
    suggest_plan_fetch: bool = False,
) -> None:
    merge_msg = _commit_message_merge(args.dev_branch, args.main_branch, next_text)
    rel = " ".join(_VERSIONED_FILES)

    def mark(step_id: str) -> bool:
        if not step_id:
            return False
        if conflict_marks is None:
            return step_id in _PESSIMISTIC_CONFLICT_STEPS
        return step_id in conflict_marks

    def step(line: str, *, step_id: str) -> None:
        mc = mark(step_id)
        suffix = "  [may conflict]" if mc else ""
        print(f"    {line}{suffix}")

    print("  Planned git steps (not executed):")
    if conflict_marks is None:
        print(
            "  [may conflict] = conservative list (analysis skipped or failed); "
            "merge/tag/push/NuGet may still fail for the usual reasons.",
        )
    else:
        print(
            "  [may conflict] = from current refs (divergence, merge-tree, tag probe); "
            "ignores the not-yet-made version bump commit and post-push races.",
        )
        if suggest_plan_fetch:
            print(
                "  Tip: add --plan-fetch with --dry-run to refresh refs/remotes before analyzing.",
            )

    if not args.no_pull:
        step(f"git pull {args.remote} {args.dev_branch}", step_id=plan_analysis.PULL_DEV)
    step(f"(write) {rel} -> version {next_text}", step_id="")
    step(f"git add {rel}", step_id="")
    step(f'git commit -m "{_commit_message_bump(next_text)}"', step_id="")
    if not args.no_pull:
        step(f"git fetch {args.remote} {args.main_branch}", step_id="")
    step(f"git checkout {args.main_branch}", step_id="")
    if not args.no_pull:
        step(f"git pull {args.remote} {args.main_branch}", step_id=plan_analysis.PULL_MAIN)
    step(
        f'git merge --no-ff {args.dev_branch} -m "{merge_msg}"',
        step_id=plan_analysis.MERGE,
    )
    if args.force_tag:
        step(f"git tag -f {tag}", step_id="")
    else:
        step(f"git tag {tag}", step_id=plan_analysis.TAG)
    step(f"git checkout {args.dev_branch}", step_id="")
    step(f"git push {args.remote} {args.dev_branch}", step_id=plan_analysis.PUSH_DEV)
    step(f"git push {args.remote} {args.main_branch}", step_id=plan_analysis.PUSH_MAIN)
    if args.force_tag:
        step(f"git push --force {args.remote} refs/tags/{tag}", step_id="")
    else:
        step(f"git push {args.remote} refs/tags/{tag}", step_id=plan_analysis.PUSH_TAG)
    if args.skip_nuget:
        step("(skip NuGet)", step_id="")
    else:
        step(
            f"dotnet pack + github zip + dotnet nuget push (targets: {', '.join(compat_targets)})",
            step_id=plan_analysis.NUGET,
        )


_PESSIMISTIC_CONFLICT_STEPS = frozenset(
    {
        plan_analysis.PULL_DEV,
        plan_analysis.PULL_MAIN,
        plan_analysis.MERGE,
        plan_analysis.TAG,
        plan_analysis.PUSH_DEV,
        plan_analysis.PUSH_MAIN,
        plan_analysis.PUSH_TAG,
        plan_analysis.NUGET,
    }
)


if __name__ == "__main__":
    raise SystemExit(main())
