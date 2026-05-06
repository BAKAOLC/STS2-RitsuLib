from __future__ import annotations

from pathlib import Path

from release_lib import git_ops

# Step ids aligned with _print_git_plan in release_cli
PULL_DEV = "pull_dev"
PULL_MAIN = "pull_main"
MERGE = "merge"
TAG = "tag"
PUSH_DEV = "push_dev"
PUSH_MAIN = "push_main"
PUSH_TAG = "push_tag"
NUGET = "nuget"


def _symmetric_counts(repo: Path, ref_a: str, ref_b: str) -> tuple[int, int] | None:
    p = git_ops.run_git(
        repo,
        ["rev-list", "--left-right", "--count", f"{ref_a}...{ref_b}"],
        check=False,
    )
    if p.returncode != 0:
        return None
    parts = p.stdout.strip().split()
    if len(parts) != 2:
        return None
    try:
        return int(parts[0]), int(parts[1])
    except ValueError:
        return None


def _diverged(repo: Path, ref_a: str, ref_b: str) -> bool | None:
    c = _symmetric_counts(repo, ref_a, ref_b)
    if c is None:
        return None
    left, right = c
    return left > 0 and right > 0


def _is_ancestor(repo: Path, ancestor_ref: str, descendant_ref: str) -> bool | None:
    p = git_ops.run_git(
        repo,
        ["merge-base", "--is-ancestor", ancestor_ref, descendant_ref],
        check=False,
    )
    if p.returncode == 0:
        return True
    if p.returncode == 1:
        return False
    return None


def merge_would_conflict(repo: Path, into_ref: str, from_ref: str) -> bool | None:
    """
    Simulate merging from_ref into into_ref using merge-tree (no working tree changes).
    None = could not tell (treat as risky).
    """
    b = git_ops.run_git(repo, ["merge-base", into_ref, from_ref], check=False)
    if b.returncode != 0:
        return True
    mb = b.stdout.strip()
    m = git_ops.run_git(repo, ["merge-tree", mb, into_ref, from_ref], check=False)
    out = m.stdout or ""
    if "<<<<<<<" in out:
        return True
    if m.returncode != 0:
        err = (m.stderr or "").lower()
        if "usage:" in err or "unknown" in err:
            return None
        return True
    return False


def compute_conflict_marks(
    repo: Path,
    remote: str,
    dev: str,
    main: str,
    tag: str,
    *,
    force_tag: bool,
    no_pull: bool,
    push_nuget: bool,
) -> frozenset[str]:
    """
    Decide which plan lines deserve [may conflict] from current refs (no mutating git state).
    Uses refs/heads/* and refs/remotes/<remote>/*; run git fetch first for fresh data.
    """
    marks: set[str] = set()
    if push_nuget:
        marks.add(NUGET)

    ld = f"refs/heads/{dev}"
    rd = f"refs/remotes/{remote}/{dev}"
    lm = f"refs/heads/{main}"
    rm = f"refs/remotes/{remote}/{main}"

    if not no_pull:
        if not git_ops.ref_exists(repo, rd):
            marks.add(PULL_DEV)
        elif not git_ops.ref_exists(repo, ld):
            marks.add(PULL_DEV)
        else:
            div = _diverged(repo, rd, ld)
            if div is True or div is None:
                marks.add(PULL_DEV)

        if not git_ops.ref_exists(repo, rm):
            marks.add(PULL_MAIN)
        elif not git_ops.ref_exists(repo, lm):
            marks.add(PULL_MAIN)
        else:
            div = _diverged(repo, rm, lm)
            if div is True or div is None:
                marks.add(PULL_MAIN)

    if not git_ops.ref_exists(repo, rm) or not git_ops.ref_exists(repo, ld):
        marks.add(MERGE)
    else:
        mc = merge_would_conflict(repo, rm, ld)
        if mc is not False:
            marks.add(MERGE)

    if not force_tag:
        if git_ops.local_tag_exists(repo, tag):
            marks.add(TAG)
            marks.add(PUSH_TAG)
        else:
            try:
                if git_ops.remote_tag_exists(repo, remote, tag):
                    marks.add(TAG)
                    marks.add(PUSH_TAG)
            except RuntimeError:
                marks.add(TAG)
                marks.add(PUSH_TAG)

    if git_ops.ref_exists(repo, rd) and git_ops.ref_exists(repo, ld):
        ff = _is_ancestor(repo, rd, ld)
        if ff is False or ff is None:
            marks.add(PUSH_DEV)

    if MERGE in marks:
        marks.add(PUSH_MAIN)
    elif git_ops.ref_exists(repo, rm) and git_ops.ref_exists(repo, ld):
        if _is_ancestor(repo, rm, ld) is not True:
            marks.add(PUSH_MAIN)
    elif git_ops.ref_exists(repo, rm):
        marks.add(PUSH_MAIN)

    return frozenset(marks)
