from __future__ import annotations

import subprocess
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
