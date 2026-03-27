from __future__ import annotations

import subprocess
import sys
from pathlib import Path


def run_git(
    repo: Path,
    args: list[str],
    *,
    check: bool = True,
) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args],
        cwd=repo,
        check=check,
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )


def git_root(start: Path) -> Path:
    try:
        p = run_git(start, ["rev-parse", "--show-toplevel"], check=True)
    except subprocess.CalledProcessError as e:
        msg = "Not a git repository (git rev-parse --show-toplevel failed)."
        raise RuntimeError(msg) from e
    return Path(p.stdout.strip()).resolve()


def current_branch(repo: Path) -> str:
    p = run_git(repo, ["branch", "--show-current"])
    return p.stdout.strip()


def worktree_clean(repo: Path) -> bool:
    p = run_git(repo, ["status", "--porcelain"])
    return p.stdout.strip() == ""


def require_clean_worktree(repo: Path) -> None:
    if not worktree_clean(repo):
        msg = "Working tree is not clean. Commit or stash changes before release."
        raise RuntimeError(msg)


def require_branch(repo: Path, expected: str) -> None:
    actual = current_branch(repo)
    if actual != expected:
        msg = f"Must be on branch {expected!r} (current: {actual!r})."
        raise RuntimeError(msg)


def remote_exists(repo: Path, name: str) -> bool:
    p = run_git(repo, ["remote", "get-url", name], check=False)
    return p.returncode == 0


def ref_exists(repo: Path, ref: str) -> bool:
    p = run_git(repo, ["rev-parse", "-q", "--verify", ref], check=False)
    return p.returncode == 0


def fetch_plan_refs(repo: Path, remote: str, dev: str, main: str) -> None:
    """Update refs/remotes/<remote>/<dev|main> (read-only for working tree)."""
    run_git(repo, ["fetch", remote, dev, main], check=True)


def local_branch_exists(repo: Path, branch: str) -> bool:
    p = run_git(repo, ["show-ref", "--verify", f"refs/heads/{branch}"], check=False)
    return p.returncode == 0


def remote_head_ref_exists(repo: Path, remote: str, branch: str) -> bool:
    """True if remote has refs/heads/<branch> (after fetch or on server)."""
    p = run_git(repo, ["ls-remote", "--heads", remote, branch], check=False)
    if p.returncode != 0:
        stderr = (p.stderr or "").strip()
        raise RuntimeError(
            f"git ls-remote --heads {remote!r} failed (cannot verify branch): {stderr}"
        )
    return bool(p.stdout.strip())


def local_tag_exists(repo: Path, tag: str) -> bool:
    p = run_git(repo, ["rev-parse", "-q", "--verify", f"refs/tags/{tag}"], check=False)
    return p.returncode == 0


def remote_tag_exists(repo: Path, remote: str, tag: str) -> bool:
    ref = f"refs/tags/{tag}"
    p = run_git(repo, ["ls-remote", remote, ref], check=False)
    if p.returncode != 0:
        stderr = (p.stderr or "").strip()
        raise RuntimeError(
            f"git ls-remote {remote!r} failed (cannot verify if tag exists): {stderr}"
        )
    return bool(p.stdout.strip())


def ensure_tag_available(repo: Path, remote: str, tag: str) -> None:
    if local_tag_exists(repo, tag):
        msg = (
            f"Tag {tag!r} already exists locally. "
            "Delete the tag, use a different --version / bump, or pass --force-tag."
        )
        raise RuntimeError(msg)
    if remote_tag_exists(repo, remote, tag):
        msg = (
            f"Tag {tag!r} already exists on remote {remote!r}. "
            "Use a different --version / bump, remove the remote tag, or pass --force-tag."
        )
        raise RuntimeError(msg)


def report_tag_collision_hints(repo: Path, remote: str, tag: str) -> None:
    """Print stderr hints when a tag already exists (dry-run)."""
    try:
        if local_tag_exists(repo, tag):
            print(
                f"[release] DRY-RUN warning: tag {tag!r} already exists locally.",
                file=sys.stderr,
            )
        if remote_tag_exists(repo, remote, tag):
            print(
                f"[release] DRY-RUN warning: tag {tag!r} already exists on {remote!r}.",
                file=sys.stderr,
            )
    except RuntimeError as e:
        print(f"[release] DRY-RUN warning: {e}", file=sys.stderr)
