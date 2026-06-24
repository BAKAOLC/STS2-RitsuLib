from __future__ import annotations

import json
import os
import shutil
import subprocess
from pathlib import Path


DEFAULT_WORKSHOP_TITLE = "RitsuLib"
DEFAULT_WORKSHOP_TAGS = ("Tools & APIs",)

WORKSHOP_CONFIG_NAME = "workshop.json"
WORKSHOP_CONTENT_DIR_NAME = "content"
WORKSHOP_PREVIEW_IMAGE_NAME = "image.png"
WORKSHOP_MOD_ID_NAME = "mod_id.txt"
_REPO_ROOT = Path(__file__).resolve().parents[2]
_DEFAULT_MARKDOWN_STEAM_BBCODE_PROJECT = (
    _REPO_ROOT / "tools" / "MarkdownSteamBbCode" / "MarkdownSteamBbCode.csproj"
)


def read_workshop_change_note(release_notes: Path | None, *, fallback: str) -> str:
    if release_notes is None or not release_notes.is_file():
        return fallback

    text = release_notes.read_text(encoding="utf-8").strip()
    if not text:
        return fallback

    english = text.split("\n---", 1)[0].strip()
    return markdown_to_steam_bbcode(english) if english else fallback


def markdown_to_steam_bbcode(markdown: str) -> str:
    tool_project = _resolve_markdown_steam_bbcode_project()
    try:
        result = subprocess.run(
            [
                "dotnet",
                "run",
                "--project",
                str(tool_project),
                "--",
                "md2bb",
            ],
            input=markdown,
            text=True,
            encoding="utf-8",
            capture_output=True,
            check=True,
        )
    except FileNotFoundError as e:
        msg = "dotnet was not found while converting Markdown to Steam BBCode."
        raise RuntimeError(msg) from e
    except subprocess.CalledProcessError as e:
        detail = (e.stderr or e.stdout or "").strip()
        msg = "Markdown to Steam BBCode conversion failed"
        if detail:
            msg += f": {detail}"
        raise RuntimeError(msg) from e

    return result.stdout.strip()


def _resolve_markdown_steam_bbcode_project() -> Path:
    raw = os.environ.get("RITSLIB_MARKDOWN_STEAM_BBCODE_PROJECT", "").strip()
    if raw:
        project = Path(raw).expanduser()
        project = project.resolve() if project.is_absolute() else (_REPO_ROOT / project).resolve()
    else:
        project = _DEFAULT_MARKDOWN_STEAM_BBCODE_PROJECT

    if not project.is_file():
        msg = (
            "MarkdownSteamBbCode tool project was not found. "
            "Set RITSLIB_MARKDOWN_STEAM_BBCODE_PROJECT to the tool csproj path. "
            f"Default path: {project}"
        )
        raise RuntimeError(msg)

    return project


def prepare_workshop_workspace(
    *,
    bundle_staging_root: Path,
    workspace: Path,
    title: str,
    description: str | None,
    visibility: str,
    tags: list[str],
    change_note: str,
    localized: dict[str, dict[str, str]],
) -> None:
    if not bundle_staging_root.is_dir():
        msg = f"bundle staging directory does not exist: {bundle_staging_root}"
        raise RuntimeError(msg)

    workspace.mkdir(parents=True, exist_ok=True)
    preview_image = workspace / WORKSHOP_PREVIEW_IMAGE_NAME
    if not preview_image.is_file():
        msg = f"Workshop workspace missing preview image: {preview_image}"
        raise RuntimeError(msg)

    content_dir = workspace / WORKSHOP_CONTENT_DIR_NAME
    shutil.rmtree(content_dir, ignore_errors=True)
    content_dir.mkdir(parents=True, exist_ok=True)
    copy_tree_contents(bundle_staging_root, content_dir)

    write_workshop_config(
        workspace / WORKSHOP_CONFIG_NAME,
        title=title,
        description=description,
        visibility=visibility,
        tags=tags,
        change_note=change_note,
        localized=localized,
    )


def upload_workshop_workspace(
    *,
    uploader_exe: Path,
    workspace: Path,
    item_id: str | None,
    allow_create: bool,
) -> None:
    if not uploader_exe.is_file():
        msg = f"ModUploader executable does not exist: {uploader_exe}"
        raise RuntimeError(msg)
    if not workspace.is_dir():
        msg = f"Workshop workspace does not exist: {workspace}"
        raise RuntimeError(msg)
    if not allow_create and not item_id and not (workspace / WORKSHOP_MOD_ID_NAME).is_file():
        msg = (
            f"Workshop upload needs --workshop-item-id or {WORKSHOP_MOD_ID_NAME}. "
            "Pass --workshop-create to allow creating a new Steam Workshop item."
        )
        raise RuntimeError(msg)

    cmd = [str(uploader_exe), "upload", "-w", str(workspace)]
    if item_id:
        cmd.extend(["-i", item_id])
    try:
        subprocess.run(cmd, cwd=uploader_exe.parent, check=True)
    except FileNotFoundError as e:
        msg = f"ModUploader executable could not be started: {uploader_exe}"
        raise RuntimeError(msg) from e
    except subprocess.CalledProcessError as e:
        msg = f"ModUploader upload failed with exit code {e.returncode}: {uploader_exe}"
        raise RuntimeError(msg) from e


def copy_tree_contents(source: Path, destination: Path) -> None:
    for child in source.iterdir():
        target = destination / child.name
        if child.is_dir():
            shutil.copytree(child, target)
        elif child.is_file():
            shutil.copy2(child, target)


def write_workshop_config(
    path: Path,
    *,
    title: str,
    description: str | None,
    visibility: str,
    tags: list[str],
    change_note: str,
    localized: dict[str, dict[str, str]],
) -> None:
    config = {
        "title": title,
        "visibility": visibility,
        "changeNote": change_note,
        "tags": tags,
        "dependencies": [],
    }
    if description:
        config["description"] = description
    if localized:
        config["localized"] = localized
    path.write_text(
        json.dumps(config, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
