from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path

_scripts_dir = Path(__file__).resolve().parent.parent
if str(_scripts_dir) not in sys.path:
    sys.path.insert(0, str(_scripts_dir))

from release_lib.msbuild_eval import get_csproj_property
from release_lib.repo_layout import (
    ARTIFACTS_GITHUB,
    ARTIFACTS_NUGET,
    DEFAULT_GIT_REMOTE,
    GITHUB_PRERELEASE_TAG_NAME,
    GITHUB_ZIP_FILENAME_SUFFIX,
    NUGET_ORG_V3_INDEX_URL,
    SNUPKG_SUFFIX,
    dev_package_version,
    ritsulib_csproj,
)


def _github_repository() -> str:
    r = os.environ.get("GITHUB_REPOSITORY", "").strip()
    if not r:
        print("GITHUB_REPOSITORY must be set (GitHub Actions provides it).", file=sys.stderr)
        raise SystemExit(1)
    return r


def _gh(args: list[str]) -> None:
    subprocess.run(["gh", *args], check=True)


def _gh_output(args: list[str]) -> str:
    r = subprocess.run(["gh", *args], check=True, capture_output=True, text=True)
    return r.stdout


def _release_exists(tag: str, repo: str) -> bool:
    r = subprocess.run(
        ["gh", "release", "view", tag, "--repo", repo],
        capture_output=True,
    )
    return r.returncode == 0


def _release_asset_names(tag: str, repo: str) -> list[str]:
    try:
        out = _gh_output(
            [
                "release",
                "view",
                tag,
                "--repo",
                repo,
                "--json",
                "assets",
                "--jq",
                ".assets[].name",
            ]
        )
    except subprocess.CalledProcessError:
        return []
    return [line.strip() for line in out.splitlines() if line.strip()]


def _clear_release_assets(tag: str, repo: str) -> None:
    for name in _release_asset_names(tag, repo):
        _gh(["release", "delete-asset", tag, name, "--yes", "--repo", repo])


def _generate_release_notes(tag: str, repo: str, target_commitish: str) -> str:
    args = [
        "api",
        "-X",
        "POST",
        f"repos/{repo}/releases/generate-notes",
        "-F",
        f"tag_name={tag}",
    ]
    if target_commitish:
        args.extend(["-F", f"target_commitish={target_commitish}"])
    args.extend(["--jq", ".body"])
    return _gh_output(args).strip()


def _read_tag_annotation(repo_root: Path, tag: str) -> str:
    tag_ref = f"refs/tags/{tag}"
    kind = subprocess.run(
        ["git", "cat-file", "-t", tag_ref],
        cwd=repo_root,
        capture_output=True,
        text=True,
    )
    if kind.returncode != 0:
        return ""
    obj_type = kind.stdout.strip()
    if obj_type == "tag":
        show = subprocess.run(
            ["git", "cat-file", "-p", tag_ref],
            cwd=repo_root,
            check=True,
            capture_output=True,
            text=True,
        )
        blob = show.stdout
        if "\n\n" in blob:
            return blob.split("\n\n", 1)[1].strip()
        return ""
    # commit = lightweight tag pointing at commit; do not use commit body as "tag description"
    return ""


def _ensure_tag_points_to_sha(repo_root: Path, tag: str, sha: str) -> None:
    if not sha:
        print("GITHUB_SHA is required to move release tag.", file=sys.stderr)
        raise SystemExit(1)
    subprocess.run(["git", "tag", "-f", tag, sha], cwd=repo_root, check=True)
    subprocess.run(
        ["git", "push", "--force", DEFAULT_GIT_REMOTE, f"refs/tags/{tag}"],
        cwd=repo_root,
        check=True,
    )


def _read_csproj_version(repo_root: Path) -> str:
    csproj = ritsulib_csproj(repo_root)
    try:
        v = get_csproj_property(csproj, "Version")
    except (OSError, RuntimeError) as e:
        print(f"Could not read Version from project: {e}", file=sys.stderr)
        raise SystemExit(1) from e
    if not v.strip():
        print("Version evaluated empty (dotnet msbuild -getProperty).", file=sys.stderr)
        raise SystemExit(1)
    return v.strip()


def cmd_dev_prerelease(repo_root: Path) -> None:
    repo = _github_repository()
    tag = GITHUB_PRERELEASE_TAG_NAME
    sha = os.environ.get("GITHUB_SHA", "").strip()
    run_id = os.environ.get("GITHUB_RUN_ID", "").strip()
    server = os.environ.get("GITHUB_SERVER_URL", "https://github.com").strip()
    repo_url = f"{server}/{repo}"
    commit_url = f"{repo_url}/commit/{sha}"
    run_url = f"{repo_url}/actions/runs/{run_id}" if run_id else ""
    base_version = _read_csproj_version(repo_root)
    dev_version = dev_package_version(run_id=run_id, sha=sha)
    notes = (
        "This is an automated **development build** from branch `dev`.\n\n"
        f"- Development Version: `{dev_version}` (independent from stable release versions)\n"
        f"- Current Stable Line in repo: `{base_version}`\n"
        "- Purpose: quick testing and integration validation.\n"
        "- Stability: may change frequently and can include breaking changes.\n"
        "- Install target: developers/testers only.\n\n"
        f"- Repository: [{repo}]({repo_url})\n"
        f"- Commit: [`{sha[:8]}`]({commit_url})\n"
        + (f"- Workflow Run: [#{run_id}]({run_url})\n" if run_url else "")
    )
    _ensure_tag_points_to_sha(repo_root, tag, sha)
    zips = sorted((repo_root / ARTIFACTS_GITHUB).glob(f"*{GITHUB_ZIP_FILENAME_SUFFIX}"))
    if not zips:
        print(f"No *{GITHUB_ZIP_FILENAME_SUFFIX} under {ARTIFACTS_GITHUB}/", file=sys.stderr)
        raise SystemExit(1)
    upload_files: list[str] = []
    for p in zips:
        # Keep original artifact name (already includes package/version), append only a short build marker.
        short = sha[:8] if sha else "local"
        dev_name = p.with_name(f"{p.stem}.sha.{short}{p.suffix}")
        dev_name.write_bytes(p.read_bytes())
        upload_files.append(str(dev_name))
    if _release_exists(tag, repo):
        _clear_release_assets(tag, repo)
        _gh(["release", "upload", tag, *upload_files, "--repo", repo])
        _gh(["release", "edit", tag, "--notes", notes, "--prerelease", "--repo", repo])
    else:
        _gh(
            [
                "release",
                "create",
                tag,
                *upload_files,
                "--repo",
                repo,
                "--title",
                "Development build (dev)",
                "--notes",
                notes,
                "--prerelease",
                "--target",
                sha,
            ]
        )


def cmd_tag_release(repo_root: Path, tag: str) -> None:
    repo = _github_repository()
    if not tag:
        print("Tag is empty; set GITHUB_REF_NAME or pass --tag.", file=sys.stderr)
        raise SystemExit(1)
    zips = sorted((repo_root / ARTIFACTS_GITHUB).glob(f"*{GITHUB_ZIP_FILENAME_SUFFIX}"))
    nupkgs = sorted(
        p
        for p in (repo_root / ARTIFACTS_NUGET).glob("*.nupkg")
        if not p.name.endswith(SNUPKG_SUFFIX)
    )
    assets = [str(p) for p in (*zips, *nupkgs)]
    if not assets:
        print(
            f"No release assets under {ARTIFACTS_GITHUB}/ or {ARTIFACTS_NUGET}/.",
            file=sys.stderr,
        )
        raise SystemExit(1)
    sha = os.environ.get("GITHUB_SHA", "").strip()
    generated_notes = _generate_release_notes(tag, repo, sha)
    tag_note_prefix = _read_tag_annotation(repo_root, tag)
    final_notes = (
        f"{tag_note_prefix}\n\n---\n\n{generated_notes}".strip()
        if tag_note_prefix
        else generated_notes
    )
    if _release_exists(tag, repo):
        _clear_release_assets(tag, repo)
        _gh(["release", "upload", tag, *assets, "--repo", repo])
        _gh(["release", "edit", tag, "--notes", final_notes, "--latest", "--repo", repo])
    else:
        _gh(
            [
                "release",
                "create",
                tag,
                *assets,
                "--repo",
                repo,
                "--notes",
                final_notes,
                "--verify-tag",
            ]
        )


def cmd_nuget_push(repo_root: Path) -> None:
    key = os.environ.get("NUGET_API_KEY", "").strip()
    if not key:
        print("NUGET_API_KEY not set; skipping NuGet.org push.")
        return
    nupkgs = sorted(
        p
        for p in (repo_root / ARTIFACTS_NUGET).glob("*.nupkg")
        if not p.name.endswith(SNUPKG_SUFFIX)
    )
    if not nupkgs:
        print(f"No .nupkg files under {ARTIFACTS_NUGET}/; skipping push.")
        return
    source = NUGET_ORG_V3_INDEX_URL
    for pkg in nupkgs:
        subprocess.run(
            [
                "dotnet",
                "nuget",
                "push",
                str(pkg),
                "--api-key",
                key,
                "--source",
                source,
                "--skip-duplicate",
            ],
            cwd=repo_root,
            check=True,
        )


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(description="GitHub Releases / NuGet publish helpers for CI.")
    p.add_argument("--repo-root", type=Path, default=Path("."))
    sub = p.add_subparsers(dest="command", required=True)

    sub.add_parser(
        "dev-prerelease",
        help=f"Upsert fixed prerelease {GITHUB_PRERELEASE_TAG_NAME!r} with GitHub zips.",
    )

    p_tag = sub.add_parser("tag-release", help="Create or update GitHub Release for a version tag.")
    p_tag.add_argument(
        "--tag",
        default=os.environ.get("GITHUB_REF_NAME", "").strip(),
        help="Release tag (default: GITHUB_REF_NAME from Actions)",
    )

    sub.add_parser("nuget-push", help="Push artifacts/nuget/*.nupkg when NUGET_API_KEY is set.")

    args = p.parse_args(argv)
    root = args.repo_root.resolve()
    if args.command == "dev-prerelease":
        cmd_dev_prerelease(root)
    elif args.command == "tag-release":
        cmd_tag_release(root, args.tag.strip())
    elif args.command == "nuget-push":
        cmd_nuget_push(root)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
