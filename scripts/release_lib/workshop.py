from __future__ import annotations

import json
import shutil
import subprocess
from pathlib import Path


DEFAULT_WORKSHOP_TITLE = "RitsuLib"
DEFAULT_WORKSHOP_DESCRIPTION = (
    "Shared framework library for Slay the Spire 2 mods. RitsuLib provides reusable "
    "patching, persistence, lifecycle, localization, settings UI, content registration, "
    "and utility APIs for other mods.\n\n"
    "[b]Support / Bug Reports[/b]\n\n"
    "If you run into a crash, load failure, or unexpected behavior, please include your game log when reporting the issue.\n\n"
    "[b]How to find the log[/b]\n"
    "[list]\n"
    "[*]In game: press [b]~[/b] to open the console, then type [b]open logs[/b].\n"
    "[*]Windows: open [b]%appdata%\\SlayTheSpire2\\logs[/b] and attach [b]godot.log[/b].\n"
    "[*]Linux: open [b]~/.local/share/SlayTheSpire2/logs[/b] and attach [b]godot.log[/b].\n"
    "[*]macOS: open [b]~/Library/Application Support/SlayTheSpire2/logs[/b] and attach [b]godot.log[/b].\n"
    "[/list]\n\n"
    "Contact: [b]utsumabo@ritsukage.com[/b]"
)
DEFAULT_WORKSHOP_SCHINESE_TITLE = "RitsuLib"
DEFAULT_WORKSHOP_SCHINESE_DESCRIPTION = (
    "Slay the Spire 2 模组共享框架库。RitsuLib 为其他模组提供可复用的补丁、"
    "持久化、生命周期、本地化、设置界面、内容注册和工具 API。\n\n"
    "[b]支持 / 问题反馈[/b]\n\n"
    "如果遇到崩溃、加载失败或异常行为，反馈时请附带游戏日志。\n\n"
    "[b]如何找到日志[/b]\n"
    "[list]\n"
    "[*]游戏内：按 [b]~[/b] 打开控制台，然后输入 [b]open logs[/b]。\n"
    "[*]Windows：打开 [b]%appdata%\\SlayTheSpire2\\logs[/b]，附上 [b]godot.log[/b]。\n"
    "[*]Linux：打开 [b]~/.local/share/SlayTheSpire2/logs[/b]，附上 [b]godot.log[/b]。\n"
    "[*]macOS：打开 [b]~/Library/Application Support/SlayTheSpire2/logs[/b]，附上 [b]godot.log[/b]。\n"
    "[/list]\n\n"
    "联系邮箱：[b]utsumabo@ritsukage.com[/b]"
)
DEFAULT_WORKSHOP_TAGS = ("Tools & APIs",)

WORKSHOP_CONFIG_NAME = "workshop.json"
WORKSHOP_CONTENT_DIR_NAME = "content"
WORKSHOP_PREVIEW_IMAGE_NAME = "image.png"
WORKSHOP_MOD_ID_NAME = "mod_id.txt"


def read_workshop_change_note(release_notes: Path | None, *, fallback: str) -> str:
    if release_notes is None or not release_notes.is_file():
        return fallback

    text = release_notes.read_text(encoding="utf-8").strip()
    if not text:
        return fallback

    english = text.split("\n---", 1)[0].strip()
    return english or fallback


def prepare_workshop_workspace(
    *,
    bundle_staging_root: Path,
    workspace: Path,
    title: str,
    description: str,
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
    subprocess.run(cmd, cwd=uploader_exe.parent, check=True)


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
    description: str,
    visibility: str,
    tags: list[str],
    change_note: str,
    localized: dict[str, dict[str, str]],
) -> None:
    config = {
        "title": title,
        "description": description,
        "localized": localized,
        "visibility": visibility,
        "changeNote": change_note,
        "tags": tags,
        "dependencies": [],
    }
    path.write_text(
        json.dumps(config, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
