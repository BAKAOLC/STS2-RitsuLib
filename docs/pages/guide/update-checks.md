---
title:
  en: Update Checks
  zh-CN: 更新检查
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Runtime update toast{lang="en"}

::: en

Use `RitsuLibFramework.RegisterModUpdateCheck(...)` when a mod should check a small mirrored or self-hosted JSON file after the first main menu loads. Do not point `ManifestUri` at GitHub, GitHub API, or raw GitHub content; use a CDN, your own site, or another mirror. The release page can still be whichever page you want users to open.

```csharp
RitsuLibFramework.RegisterModUpdateCheck(new()
{
    ModId = MyModConst.ModId,
    DisplayName = MyModConst.Name,
    CurrentVersion = MyModConst.Version,
    ManifestUri = new("https://example.com/my-mod/update.json"),
    ReleasePageUri = new("https://example.com/my-mod/releases"),
});
```

For mods that are also distributed through Steam Workshop, wrap the options with `SkipModUpdateCheckWhenLoadedFromSteamWorkshop` and pass the mod's Workshop item id. This keeps local/manual installs on the external update source, while that exact Workshop item is left to Steam's Workshop update flow.

```csharp
RitsuLibFramework.RegisterModUpdateCheck(
    RitsuLibFramework.SkipModUpdateCheckWhenLoadedFromSteamWorkshop(new()
    {
        ModId = MyModConst.ModId,
        DisplayName = MyModConst.Name,
        CurrentVersion = MyModConst.Version,
        ManifestUri = new("https://example.com/my-mod/update.json"),
        ReleasePageUri = new("https://example.com/my-mod/releases"),
    }, typeof(MyModPlugin).Assembly, 1234567890));
```

If the manifest version is newer, RitsuLib shows a normal non-persistent info toast. Clicking the toast opens `release_page_url` from the manifest, or `ReleasePageUri` from options when the manifest omits it.

```json
{
  "schema": "ritsulib.update.v1",
  "latest_version": "1.2.3",
  "release_page_url": "https://example.com/my-mod/releases",
  "title": "My Mod update available",
  "message": "Version 1.2.3 of My Mod is available. Click to open the release page."
}
```

:::

## 运行时更新 toast{lang="zh-CN"}

::: zh-CN

如果一个 Mod 需要在首次主菜单加载后检查更新，使用 `RitsuLibFramework.RegisterModUpdateCheck(...)`。`ManifestUri` 不要指向 GitHub、GitHub API 或 raw GitHub 内容；请使用 CDN、自有站点或其他镜像。发布页可以是你希望用户点击后打开的任意页面。

```csharp
RitsuLibFramework.RegisterModUpdateCheck(new()
{
    ModId = MyModConst.ModId,
    DisplayName = MyModConst.Name,
    CurrentVersion = MyModConst.Version,
    ManifestUri = new("https://example.com/my-mod/update.json"),
    ReleasePageUri = new("https://example.com/my-mod/releases"),
});
```

如果 Mod 同时通过 Steam 创意工坊发布，可以用 `SkipModUpdateCheckWhenLoadedFromSteamWorkshop` 包装选项，并传入该 Mod 的 Workshop item id。这样本地/手动安装仍然使用外部更新源，而该精确 Workshop item 交给 Steam 的 Workshop 更新流程。

```csharp
RitsuLibFramework.RegisterModUpdateCheck(
    RitsuLibFramework.SkipModUpdateCheckWhenLoadedFromSteamWorkshop(new()
    {
        ModId = MyModConst.ModId,
        DisplayName = MyModConst.Name,
        CurrentVersion = MyModConst.Version,
        ManifestUri = new("https://example.com/my-mod/update.json"),
        ReleasePageUri = new("https://example.com/my-mod/releases"),
    }, typeof(MyModPlugin).Assembly, 1234567890));
```

如果 manifest 里的版本更新，RitsuLib 会显示一个普通、非持久的信息 toast。点击 toast 时优先打开 manifest 里的 `release_page_url`；如果 manifest 没有提供，则打开选项里的 `ReleasePageUri`。

```json
{
  "schema": "ritsulib.update.v1",
  "latest_version": "1.2.3",
  "release_page_url": "https://example.com/my-mod/releases",
  "title": "My Mod update available",
  "message": "Version 1.2.3 of My Mod is available. Click to open the release page."
}
```

:::
