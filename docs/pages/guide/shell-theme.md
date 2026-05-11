---
title:
  en: Runtime UI And Shell Theme
  zh-CN: 运行时 UI 与 Shell 主题
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Settings Shell Theme{lang="en"}

::: en

RitsuLib settings pages use shell theme tokens for colors, typography, spacing, and component defaults. Most mods should rely on the active theme. Add a custom theme only when your settings UI needs a coherent visual identity across several custom controls.

Theme JSON files belong in your mod resources and are registered through the shell theme APIs. Keep token names semantic: describe the role, not the color.

:::

## 设置 Shell 主题{lang="zh-CN"}

::: zh-CN

RitsuLib 设置页面使用 shell theme token 控制颜色、字体、间距和控件默认样式。大多数 Mod 直接使用当前主题即可。只有当你的设置 UI 有多个自定义控件，并且需要统一视觉身份时，才添加自定义主题。

主题 JSON 放在 Mod 资源中，并通过 shell theme API 注册。Token 名称应表达用途，不要只描述颜色。

:::

## Toasts{lang="en"}

::: en

Use toast messages for short runtime feedback that should not interrupt play.

```csharp
RitsuToastService.ShowInfo("Settings saved.", "My Mod");
RitsuToastService.ShowWarning("Optional bank failed to load.", "My Mod");
RitsuToastService.ShowError("Required setup failed.", "My Mod");
```

For custom placement, image, duration, or click behavior, build a `RitsuToastRequest`.

:::

## Toast{lang="zh-CN"}

::: zh-CN

短运行时反馈可以使用 toast，不打断玩家操作。

```csharp
RitsuToastService.ShowInfo("设置已保存。", "My Mod");
RitsuToastService.ShowWarning("可选 bank 加载失败。", "My Mod");
RitsuToastService.ShowError("必要初始化失败。", "My Mod");
```

需要自定义位置、图片、持续时间或点击行为时，构建 `RitsuToastRequest`。

:::

## Runtime Hotkeys{lang="en"}

::: en

Register hotkeys when the action must be available outside a settings page.

```csharp
var handle = RuntimeHotkeyService.Register(
    "Ctrl+Shift+M",
    callback: ToggleMyOverlay,
    id: "my_mod.toggle_overlay",
    title: "Toggle overlay");
```

Expose hotkeys in settings with `AddKeyBinding`, `AddMultiKeyBinding`, or `AddRuntimeHotkeySummary`.

:::

## 运行时快捷键{lang="zh-CN"}

::: zh-CN

当操作需要在设置页面之外可用时，注册运行时快捷键。

```csharp
var handle = RuntimeHotkeyService.Register(
    "Ctrl+Shift+M",
    callback: ToggleMyOverlay,
    id: "my_mod.toggle_overlay",
    title: "Toggle overlay");
```

在设置页面中展示或编辑快捷键时，使用 `AddKeyBinding`、`AddMultiKeyBinding` 或 `AddRuntimeHotkeySummary`。

:::

## Top-Bar Buttons{lang="en"}

::: en

Register top-bar buttons through the content pack:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .TopBarButtonOwned("my_panel", new ModTopBarButtonSpec(
        IconPath: "res://MyMod/images/ui/my_panel.png",
        Handler: new MyTopBarButtonHandler()))
    .Apply();
```

Owned button ids use `MY_MOD_TOPBARBUTTON_MY_PANEL` and read hover text from `static_hover_tips`.

:::

## 顶栏按钮{lang="zh-CN"}

::: zh-CN

通过 content pack 注册顶栏按钮：

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .TopBarButtonOwned("my_panel", new ModTopBarButtonSpec(
        IconPath: "res://MyMod/images/ui/my_panel.png",
        Handler: new MyTopBarButtonHandler()))
    .Apply();
```

Owned button id 会是 `MY_MOD_TOPBARBUTTON_MY_PANEL`，hover 文本从 `static_hover_tips` 读取。

:::

## Card Piles{lang="en"}

::: en

Custom piles are useful for extra hand-like or discard-like collections.

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .CardPileOwned("archive", new ModCardPileSpec
    {
        DisplayName = "Archive"
    })
    .Apply();
```

Use stable local stems. Pile ids are part of UI text, save-like state, and player expectations.

:::

## 自定义卡堆{lang="zh-CN"}

::: zh-CN

自定义卡堆适合额外的手牌区、弃牌区或类似集合。

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .CardPileOwned("archive", new ModCardPileSpec
    {
        DisplayName = "Archive"
    })
    .Apply();
```

使用稳定 local stem。卡堆 ID 会影响 UI 文本、类似保存的状态和玩家预期。

:::
