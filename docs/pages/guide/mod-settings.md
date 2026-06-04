---
title:
  en: Mod Settings
  zh-CN: Mod 设置
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Register A Page{lang="en"}

::: en

Register settings pages from your mod initializer after the backing data is registered.

```csharp
RitsuLibFramework.RegisterModSettings("MyMod", page => page
    .WithTitle(ModSettingsText.Literal("My Mod"))
    .WithModDisplayName(ModSettingsText.Literal("My Mod"))
    .AddSection("general", section => section
        .WithTitle(ModSettingsText.Literal("General"))
        .AddToggle(
            "enabled",
            ModSettingsText.Literal("Enabled"),
            new ModSettingsValueBinding<MySettings, bool>(
                "MyMod",
                "settings",
                SaveScope.Global,
                s => s.Enabled,
                (s, value) => s.Enabled = value))
        .AddIntSlider(
            "volume",
            ModSettingsText.Literal("Volume"),
            new ModSettingsValueBinding<MySettings, int>(
                "MyMod",
                "settings",
                SaveScope.Global,
                s => s.Volume,
                (s, value) => s.Volume = value),
            minValue: 0,
            maxValue: 100)));
```

Use `ModSettingsText.Literal(...)` for fixed text. Use `ModSettingsText.I18N(...)` or `ModSettingsText.LocString(...)` when the UI should localize.

:::

## 注册页面{lang="zh-CN"}

::: zh-CN

设置页面应在持久化数据注册之后，从 Mod 初始化入口注册。

```csharp
RitsuLibFramework.RegisterModSettings("MyMod", page => page
    .WithTitle(ModSettingsText.Literal("My Mod"))
    .WithModDisplayName(ModSettingsText.Literal("My Mod"))
    .AddSection("general", section => section
        .WithTitle(ModSettingsText.Literal("通用"))
        .AddToggle(
            "enabled",
            ModSettingsText.Literal("启用"),
            new ModSettingsValueBinding<MySettings, bool>(
                "MyMod",
                "settings",
                SaveScope.Global,
                s => s.Enabled,
                (s, value) => s.Enabled = value))
        .AddIntSlider(
            "volume",
            ModSettingsText.Literal("音量"),
            new ModSettingsValueBinding<MySettings, int>(
                "MyMod",
                "settings",
                SaveScope.Global,
                s => s.Volume,
                (s, value) => s.Volume = value),
            minValue: 0,
            maxValue: 100)));
```

固定文本使用 `ModSettingsText.Literal(...)`。需要本地化时，使用 `ModSettingsText.I18N(...)` 或 `ModSettingsText.LocString(...)`。

:::

## Controls{lang="en"}

::: en

| Control | Builder method |
| --- | --- |
| Toggle | `AddToggle` |
| Integer slider | `AddIntSlider` |
| Floating slider | `AddSlider` |
| Choice / enum | `AddChoice`, `AddEnumChoice` |
| Color | `AddColor` |
| Single-line text | `AddString` |
| Multiline text | `AddMultilineString` |
| Key binding | `AddKeyBinding`, `AddMultiKeyBinding` |
| Button | `AddButton` |
| Editable list | `AddList` |
| Read-only content | `AddHeader`, `AddParagraph`, `AddInfoCard`, `AddImage`, `AddRuntimeHotkeySummary` |
| Nested page | `AddSubpage` |
| Custom Godot control | `AddCustom` |

Every interactive control should have a stable entry id. Changing ids after release breaks clipboard and saved UI metadata expectations.

:::

## 控件{lang="zh-CN"}

::: zh-CN

| 控件 | Builder 方法 |
| --- | --- |
| 开关 | `AddToggle` |
| 整数滑条 | `AddIntSlider` |
| 浮点滑条 | `AddSlider` |
| 选项 / enum | `AddChoice`、`AddEnumChoice` |
| 颜色 | `AddColor` |
| 单行文本 | `AddString` |
| 多行文本 | `AddMultilineString` |
| 按键绑定 | `AddKeyBinding`、`AddMultiKeyBinding` |
| 按钮 | `AddButton` |
| 可编辑列表 | `AddList` |
| 只读内容 | `AddHeader`、`AddParagraph`、`AddInfoCard`、`AddImage`、`AddRuntimeHotkeySummary` |
| 嵌套页面 | `AddSubpage` |
| 自定义 Godot 控件 | `AddCustom` |

每个交互控件都应有稳定 entry id。发布后改 id 会破坏剪贴板和 UI 元数据预期。

:::

## Bindings{lang="en"}

::: en

Use `ModSettingsValueBinding<TModel,TValue>` for fields stored in `ModDataStore`. For temporary UI, use `InMemoryModSettingsValueBinding<TValue>`.

When a control edits part of a larger value, wrap the parent binding with `ProjectedModSettingsValueBinding<TSource,TValue>`.

```csharp
var settingsBinding = new ModSettingsValueBinding<MySettings, MySettings>(
    "MyMod", "settings", SaveScope.Global, s => s, (_, value) => value);

var volumeBinding = new ProjectedModSettingsValueBinding<MySettings, int>(
    settingsBinding,
    "volume",
    s => s.Volume,
    (s, value) => { s.Volume = value; return s; });
```

:::

## 绑定{lang="zh-CN"}

::: zh-CN

保存在 `ModDataStore` 中的字段使用 `ModSettingsValueBinding<TModel,TValue>`。临时 UI 使用 `InMemoryModSettingsValueBinding<TValue>`。

当控件编辑较大对象的一部分时，用 `ProjectedModSettingsValueBinding<TSource,TValue>` 包装父绑定。

```csharp
var settingsBinding = new ModSettingsValueBinding<MySettings, MySettings>(
    "MyMod", "settings", SaveScope.Global, s => s, (_, value) => value);

var volumeBinding = new ProjectedModSettingsValueBinding<MySettings, int>(
    settingsBinding,
    "volume",
    s => s.Volume,
    (s, value) => { s.Volume = value; return s; });
```

:::

## Visibility And Host Surfaces{lang="en"}

::: en

Use visibility and read-only gates for settings that only make sense in certain places:

```csharp
page
    .WithVisibleOnHostSurfaces(ModSettingsHostSurface.MainMenu | ModSettingsHostSurface.RunPause)
    .WithReadOnlyOnHostSurfaces(ModSettingsHostSurface.CombatPause);

section.WithEntryReadOnlyOnHostSurfaces("dangerous_option", ModSettingsHostSurface.CombatPause);
```

Use `WithVisibleWhen` and `WithEnabledWhen` for runtime conditions.
Use `WithEntryEnabledWhen` for one entry:

```csharp
section.WithEntryEnabledWhen("dangerous_option", () => MyRuntime.CanEditDangerousOption);
```

:::

## 可见性与宿主界面{lang="zh-CN"}

::: zh-CN

只在特定界面有意义的设置，可以限制可见性或只读状态：

```csharp
page
    .WithVisibleOnHostSurfaces(ModSettingsHostSurface.MainMenu | ModSettingsHostSurface.RunPause)
    .WithReadOnlyOnHostSurfaces(ModSettingsHostSurface.CombatPause);

section.WithEntryReadOnlyOnHostSurfaces("dangerous_option", ModSettingsHostSurface.CombatPause);
```

运行时条件使用 `WithVisibleWhen` 和 `WithEnabledWhen`。
单个条目可使用 `WithEntryEnabledWhen`：

```csharp
section.WithEntryEnabledWhen("dangerous_option", () => MyRuntime.CanEditDangerousOption);
```

:::
