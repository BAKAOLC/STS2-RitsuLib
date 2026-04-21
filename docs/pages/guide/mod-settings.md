---
title:
  en: Mod Settings
  zh-CN: Mod 设置界面
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

RitsuLib provides a settings UI layer for player-editable values.
It is built on top of `ModDataStore`, but it does not replace the persistence model.

Use this system when you need to expose a selected subset of persisted values, organize them into pages and sections, and localize the visible text. Settings pages are registered explicitly by design.

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

RitsuLib 提供一套用于玩家可编辑值的设置 UI。它构建在 `ModDataStore` 之上，但不替代底层持久化模型。

这套系统适合用于暴露一部分持久化字段、按页面和分区组织设置项，并统一管理界面文案。所有设置项都需要显式注册，这一限制是有意设计。

---

:::

## Architecture{lang="en"}

::: en

Keep these responsibilities separate:

- `ModDataStore`: persistence, scopes, defaults, migrations
- `IModSettingsValueBinding<T>`: read/write bridge between UI and stored data
- page and section builders: UI structure and ordering
- `ModSettingsText`: text source abstraction for labels and descriptions

This separation prevents runtime state, internal metadata, and user-editable configuration from collapsing into one model.

---

:::

## 架构分层{lang="zh-CN"}

::: zh-CN

建议保持以下职责分离：

- `ModDataStore`：持久化、作用域、默认值、迁移
- `IModSettingsValueBinding<T>`：UI 与存储值之间的读写桥接
- 页面 / 分区构建器：页面结构、层级与排序
- `ModSettingsText`：标签与描述的文本来源抽象

这样可以避免把运行时状态、内部元数据与玩家配置混入同一个模型。

---

:::

## Core APIs{lang="en"}

::: en

| API | Purpose |
|---|---|
| `RitsuLibFramework.RegisterModSettings(modId, configure, pageId?)` | Register a settings page; when `pageId` is omitted it defaults to `modId` |
| `RitsuLibFramework.GetRegisteredModSettings()` | Return all registered pages |
| `ModSettingsBindings.Global(...)` / `Profile(...)` | Bind a control to persisted data |
| `ModSettingsBindings.InMemory(...)` | Bind a control to preview-only state |
| `ModSettingsText.Literal(...)` | Plain text |
| `ModSettingsText.I18N(...)` | `I18N`-backed settings text |
| `ModSettingsText.LocString(...)` | Game-native localization text |
| `ModSettingsText.Dynamic(...)` | Re-evaluate text on UI refresh |
| `WithModDisplayName(...)` | Override the mod label shown in the sidebar |
| `WithSortOrder(...)` | Sort sibling pages within one mod |
| `AsChildOf(parentPageId)` | Register a page as a child page |
| `section.Collapsible(startCollapsed?)` | Make a section collapsible |
| `page.WithVisibleWhen(...)` / `section.WithVisibleWhen(...)` | Conditional page or section visibility |
| `AddToggle(...)`, `AddSlider(...)`, `AddIntSlider(...)`, `AddChoice(...)`, `AddEnumChoice(...)` | Standard value editors |
| `AddColor(...)`, `AddKeyBinding(...)`, `AddImage(...)` | Specialized editors and previews |
| `AddButton(...)`, `AddHeader(...)`, `AddParagraph(...)` | Structural and action entries |
| `AddSubpage(...)` | Navigate to a child page |
| `AddList(...)` | Structured list editor |
| `ModSettingsUiActionRegistry.Register*ActionAppender(...)` | Extend the actions menu for rows, list items, pages, or sections |

---

:::

## 核心 API{lang="zh-CN"}

::: zh-CN

| API | 作用 |
|---|---|
| `RitsuLibFramework.RegisterModSettings(modId, configure, pageId?)` | 注册设置页；省略 `pageId` 时默认为 `modId` |
| `RitsuLibFramework.GetRegisteredModSettings()` | 返回当前所有已注册设置页 |
| `ModSettingsBindings.Global(...)` / `Profile(...)` | 将控件绑定到持久化数据 |
| `ModSettingsBindings.InMemory(...)` | 绑定到仅预览状态 |
| `ModSettingsText.Literal(...)` | 纯文本 |
| `ModSettingsText.I18N(...)` | 基于 `I18N` 的设置界面文本 |
| `ModSettingsText.LocString(...)` | 游戏原生本地化文本 |
| `ModSettingsText.Dynamic(...)` | 在 UI 刷新时重新求值 |
| `WithModDisplayName(...)` | 覆盖侧栏中的 Mod 名称 |
| `WithSortOrder(...)` | 控制同级页面排序 |
| `AsChildOf(parentPageId)` | 将页面注册为子页 |
| `section.Collapsible(startCollapsed?)` | 声明可折叠分区 |
| `page.WithVisibleWhen(...)` / `section.WithVisibleWhen(...)` | 按条件显示或隐藏页面、分区 |
| `AddToggle(...)`、`AddSlider(...)`、`AddIntSlider(...)`、`AddChoice(...)`、`AddEnumChoice(...)` | 标准值编辑控件 |
| `AddColor(...)`、`AddKeyBinding(...)`、`AddImage(...)` | 专用编辑控件与预览 |
| `AddButton(...)`、`AddHeader(...)`、`AddParagraph(...)` | 结构项与动作项 |
| `AddSubpage(...)` | 导航到子页 |
| `AddList(...)` | 结构化列表编辑器 |
| `ModSettingsUiActionRegistry.Register*ActionAppender(...)` | 扩展行、列表项、页面或分区的 Actions 菜单 |

---

:::

## Recommended Flow{lang="en"}

::: en

1. Register the complete persisted model in `ModDataStore`.
2. Create bindings only for fields that players should edit.
3. Register pages and sections around those bindings.
4. Localize all visible labels, descriptions, and option names.

The result is an explicit contract between stored data and the settings UI.

---

:::

## 推荐流程{lang="zh-CN"}

::: zh-CN

1. 在 `ModDataStore` 中注册完整持久化模型。
2. 仅为需要暴露给玩家的字段创建绑定。
3. 围绕这些绑定注册页面和分区。
4. 补齐所有可见标签、描述与选项名称的本地化。

这样可以把存储结构与设置 UI 的公开范围明确分开。

---

:::

## UI Behavior{lang="en"}

::: en

- Entry point: Main menu -> `Settings` -> `General`. When at least one page is registered, RitsuLib injects a `Mod Settings (RitsuLib)` row that opens `RitsuModSettingsSubmenu`.
- Sidebar: grouped by mod. One mod group is expanded at a time. The selected page also exposes section shortcuts.
- Content pane: page header, optional back navigation for child pages, and a scrollable section body.
- Save timing: dirty bindings are flushed on a debounce of about `0.35s`. Closing or hiding the submenu, leaving the tree, or changing the game locale forces an immediate flush.

`WithVisibleWhen(...)` and row-level `visibleWhen` predicates are re-evaluated on debounced refresh. Predicates should stay cheap and should not throw. If evaluation fails, the control remains visible.

---

:::

## 界面行为{lang="zh-CN"}

::: zh-CN

- **入口**：主菜单 -> `设置` -> `General`。当至少存在一个已注册页面时，RitsuLib 会注入 `Mod Settings (RitsuLib)` 入口并打开 `RitsuModSettingsSubmenu`。
- **侧栏**：按 Mod 分组，同一时间只展开一个分组。当前页下方会显示对应分区快捷入口。
- **内容区**：顶部显示页面标题；子页提供返回导航；正文按分区滚动显示。
- **保存时机**：绑定被标记为脏后，约 `0.35s` 防抖保存；关闭或隐藏子菜单、退出场景树、切换游戏语言时会立即刷写。

`WithVisibleWhen(...)` 与行级 `visibleWhen` 谓词会在防抖刷新时重新计算。谓词应保持轻量且避免抛异常；如果求值失败，控件保持显示。

---

:::

## Auto-Mirror Policy (BaseLib / ModConfig){lang="en"}

::: en

`RitsuModSettingsSubmenu` automatically tries to mirror settings from both `BaseLib` and `ModConfig`.  
When your mod intentionally supports multiple settings stacks, you can control mirror behavior with assembly-level `AssemblyMetadata` directives (requires only `System.Reflection`, no `STS2RitsuLib` reference).

Supported keys (case-insensitive):

- `RitsuLib.ModSettingsMirror.Global.DisableSources`
- `RitsuLib.ModSettingsMirror.Global.PreferredSource`
- `RitsuLib.ModSettingsMirror.Mod.<ModId>.DisableSources`
- `RitsuLib.ModSettingsMirror.Mod.<ModId>.PreferredSource`
- `RitsuLib.ModSettingsMirror.Type.<FullTypeName>.DisableSources`
- `RitsuLib.ModSettingsMirror.Type.<FullTypeName>.PreferredSource`

Value rules:

- `DisableSources`: `baselib`, `modconfig`, `all` (multiple values can be separated by `,` / `;` / `|`)
- `PreferredSource`: `baselib` or `modconfig`

Priority (high -> low): `Type` -> `Mod` -> `Global`.  
`PreferredSource` suppresses non-preferred mirror sources, and `DisableSources` blocks specific sources directly.

Example:

```csharp
using System.Reflection;

[assembly: AssemblyMetadata("RitsuLib.ModSettingsMirror.Mod.MyMod.DisableSources", "modconfig")]
[assembly: AssemblyMetadata("RitsuLib.ModSettingsMirror.Mod.MyMod.PreferredSource", "baselib")]
[assembly: AssemblyMetadata(
    "RitsuLib.ModSettingsMirror.Type.MyMod.Config.AdvancedSettings.DisableSources",
    "baselib")]
```

You can also place the same directives directly in `csproj`:

```xml
<ItemGroup>
  <AssemblyMetadata Include="RitsuLib.ModSettingsMirror.Mod.MyMod.DisableSources" Value="modconfig" />
  <AssemblyMetadata Include="RitsuLib.ModSettingsMirror.Mod.MyMod.PreferredSource" Value="baselib" />
  <AssemblyMetadata Include="RitsuLib.ModSettingsMirror.Type.MyMod.Config.AdvancedSettings.DisableSources" Value="baselib" />
</ItemGroup>
```

---

:::

## 自动镜像策略（BaseLib / ModConfig）{lang="zh-CN"}

::: zh-CN

`RitsuModSettingsSubmenu` 会自动尝试镜像 `BaseLib` 与 `ModConfig` 的设置页。  
当你的模组同时接入多套设置源时，可以通过程序集级 `AssemblyMetadata` 指令（仅依赖 `System.Reflection`）控制镜像行为，无需引用 `STS2RitsuLib`。

支持的键（不区分大小写）：

- `RitsuLib.ModSettingsMirror.Global.DisableSources`
- `RitsuLib.ModSettingsMirror.Global.PreferredSource`
- `RitsuLib.ModSettingsMirror.Mod.<ModId>.DisableSources`
- `RitsuLib.ModSettingsMirror.Mod.<ModId>.PreferredSource`
- `RitsuLib.ModSettingsMirror.Type.<FullTypeName>.DisableSources`
- `RitsuLib.ModSettingsMirror.Type.<FullTypeName>.PreferredSource`

值约定：

- `DisableSources`：`baselib`、`modconfig`、`all`（可用 `,` / `;` / `|` 分隔多个值）
- `PreferredSource`：`baselib` 或 `modconfig`

优先级（高 -> 低）：`Type` -> `Mod` -> `Global`。  
`PreferredSource` 会让非首选来源不参与镜像；`DisableSources` 会直接禁用对应来源镜像。

示例：

```csharp
using System.Reflection;

[assembly: AssemblyMetadata("RitsuLib.ModSettingsMirror.Mod.MyMod.DisableSources", "modconfig")]
[assembly: AssemblyMetadata("RitsuLib.ModSettingsMirror.Mod.MyMod.PreferredSource", "baselib")]
[assembly: AssemblyMetadata(
    "RitsuLib.ModSettingsMirror.Type.MyMod.Config.AdvancedSettings.DisableSources",
    "baselib")]
```

也可以直接写在 `csproj` 中：

```xml
<ItemGroup>
  <AssemblyMetadata Include="RitsuLib.ModSettingsMirror.Mod.MyMod.DisableSources" Value="modconfig" />
  <AssemblyMetadata Include="RitsuLib.ModSettingsMirror.Mod.MyMod.PreferredSource" Value="baselib" />
  <AssemblyMetadata Include="RitsuLib.ModSettingsMirror.Type.MyMod.Config.AdvancedSettings.DisableSources" Value="baselib" />
</ItemGroup>
```

---

:::

## Runtime Reflection Protocol (No Library Reference){lang="en"}

::: en

Besides BaseLib / ModConfig mirrors, RitsuLib also supports a pure reflection protocol for settings pages.  
Your mod does not need to reference `STS2RitsuLib`; you only need to explicitly declare provider types in assembly metadata:

```xml
<ItemGroup>
  <AssemblyMetadata Include="RitsuLib.ModSettingsInterop.ProviderType" Value="YourMod.Scripts.RitsuLibModSettingsInteropProvider" />
</ItemGroup>
```

Runtime-initiated explicit registration is also supported (for reflection-driven init flows):

- `ModSettingsRuntimeReflectionInteropMirror.RegisterProviderType(string providerTypeFullName, string? assemblyName = null)`
- `ModSettingsRuntimeReflectionInteropMirror.RegisterProviderType(Type providerType)`
- `ModSettingsRuntimeReflectionInteropMirror.RegisterProviderTypeAndTryRegister(string providerTypeFullName, string? assemblyName = null)`
- `ModSettingsRuntimeReflectionInteropMirror.RegisterProviderTypeAndTryRegister(Type providerType)`

Provider contract (all methods are `static`):

- `object CreateRitsuLibSettingsSchema()`
- `object? GetRitsuLibSettingValue(string key)`
- `void SetRitsuLibSettingValue(string key, object value)`
- Optional: `void SaveRitsuLibSettings()`
- Optional: `void InvokeRitsuLibSettingAction(string key)` (for button actions)
- Optional typed overrides (preferred over object resolver):
  - `bool GetRitsuLibSettingBool(string key)` / `void SetRitsuLibSettingBool(string key, bool value)`
  - `int GetRitsuLibSettingInt(string key)` / `void SetRitsuLibSettingInt(string key, int value)`
  - `double GetRitsuLibSettingDouble(string key)` / `void SetRitsuLibSettingDouble(string key, double value)`
  - `string GetRitsuLibSettingString(string key)` / `void SetRitsuLibSettingString(string key, string value)`

`CreateRitsuLibSettingsSchema()` can return:

- `Dictionary<string, object?>` (or equivalent object)
- a JSON string (root must be an object)
- a JSON file path (file root must be an object)

Godot paths (`res://`, `user://`) are recommended, and regular file paths are also supported.

Structure:

- page: `modId`, `pageId`, `title`, `description`, `sortOrder`, `sections`
- section: `id`, `title`, `description`, `entries`
- entry:
  - common fields: `id`, `type`, `key`, `label`, `description`, `scope`
  - `type=toggle|string|button|choice|slider|int-slider`
  - `choice`: `options` (`[{ value, label }]`)
  - `slider/int-slider`: `min`, `max`, `step`
  - `string`: `maxLength`
  - `button`: `buttonText`, `tone`

---

:::

## 运行时反射协议（无库引用）{lang="zh-CN"}

::: zh-CN

除了 BaseLib / ModConfig 镜像外，RitsuLib 还支持“纯反射协议”注册设置页。  
模组无需引用 `STS2RitsuLib`，只需在程序集元数据中显式声明 provider 类型：

```xml
<ItemGroup>
  <AssemblyMetadata Include="RitsuLib.ModSettingsInterop.ProviderType" Value="YourMod.Scripts.RitsuLibModSettingsInteropProvider" />
</ItemGroup>
```

也支持在运行时主动注册 provider（适合你在初始化流程中按需反射调用）：

- `ModSettingsRuntimeReflectionInteropMirror.RegisterProviderType(string providerTypeFullName, string? assemblyName = null)`
- `ModSettingsRuntimeReflectionInteropMirror.RegisterProviderType(Type providerType)`
- `ModSettingsRuntimeReflectionInteropMirror.RegisterProviderTypeAndTryRegister(string providerTypeFullName, string? assemblyName = null)`
- `ModSettingsRuntimeReflectionInteropMirror.RegisterProviderTypeAndTryRegister(Type providerType)`

Provider 约定（全部为 `static` 方法）：

- `object CreateRitsuLibSettingsSchema()`
- `object? GetRitsuLibSettingValue(string key)`
- `void SetRitsuLibSettingValue(string key, object value)`
- 可选：`void SaveRitsuLibSettings()`
- 可选：`void InvokeRitsuLibSettingAction(string key)`（用于 button）
- 可选强类型覆盖（优先于 object resolver）：
  - `bool GetRitsuLibSettingBool(string key)` / `void SetRitsuLibSettingBool(string key, bool value)`
  - `int GetRitsuLibSettingInt(string key)` / `void SetRitsuLibSettingInt(string key, int value)`
  - `double GetRitsuLibSettingDouble(string key)` / `void SetRitsuLibSettingDouble(string key, double value)`
  - `string GetRitsuLibSettingString(string key)` / `void SetRitsuLibSettingString(string key, string value)`

`CreateRitsuLibSettingsSchema()` 可以返回：

- `Dictionary<string, object?>`（或等价对象）
- JSON 字符串（根节点必须是对象）
- JSON 文件路径（内容根节点必须是对象）

推荐使用 Godot 路径（`res://`、`user://`），也支持普通文件路径。

字段结构：

- page: `modId`, `pageId`, `title`, `description`, `sortOrder`, `sections`
- section: `id`, `title`, `description`, `entries`
- entry:
  - 公共字段：`id`, `type`, `key`, `label`, `description`, `scope`
  - `type=toggle|string|button|choice|slider|int-slider`
  - `choice`：`options`（`[{ value, label }]`）
  - `slider/int-slider`：`min`, `max`, `step`
  - `string`：`maxLength`
  - `button`：`buttonText`, `tone`

---

:::

## Minimal Example{lang="en"}

::: en

First register persisted data:

```csharp
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

public sealed class MyModSettings
{
    public bool EnableFancyVfx { get; set; } = true;
    public double ScreenShakeScale { get; set; } = 1.0;
    public MyDifficultyMode DifficultyMode { get; set; } = MyDifficultyMode.Normal;
}

using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    var store = RitsuLibFramework.GetDataStore("MyMod");

    store.Register<MyModSettings>(
        key: "settings",
        fileName: "settings.json",
        scope: SaveScope.Global,
        defaultFactory: () => new MyModSettings(),
        autoCreateIfMissing: true);
}
```

Then create bindings and register the page:

```csharp
using STS2RitsuLib.Settings;

var settingsLoc = RitsuLibFramework.CreateModLocalization(
    modId: "MyMod",
    instanceName: "MyMod-Settings",
    resourceFolders: ["MyMod.Localization.Settings"]);

var fancyVfx = ModSettingsBindings.Global<MyModSettings, bool>(
    "MyMod",
    "settings",
    model => model.EnableFancyVfx,
    (model, value) => model.EnableFancyVfx = value);

var shakeScale = ModSettingsBindings.Global<MyModSettings, double>(
    "MyMod",
    "settings",
    model => model.ScreenShakeScale,
    (model, value) => model.ScreenShakeScale = value);

var difficulty = ModSettingsBindings.Global<MyModSettings, MyDifficultyMode>(
    "MyMod",
    "settings",
    model => model.DifficultyMode,
    (model, value) => model.DifficultyMode = value);

RitsuLibFramework.RegisterModSettings("MyMod", page => page
    .WithModDisplayName(ModSettingsText.I18N(settingsLoc, "mod.display_name", "My Fancy Mod"))
    .WithTitle(ModSettingsText.I18N(settingsLoc, "page.title", "Settings"))
    .WithDescription(ModSettingsText.I18N(settingsLoc, "page.description", "Player-facing options for this mod."))
    .AddSection("general", section => section
        .WithTitle(ModSettingsText.I18N(settingsLoc, "general.title", "General"))
        .AddToggle(
            "fancy_vfx",
            ModSettingsText.I18N(settingsLoc, "fancy_vfx.label", "Fancy VFX"),
            fancyVfx,
            ModSettingsText.I18N(settingsLoc, "fancy_vfx.desc", "Enable additional visual polish."))
        .AddSlider(
            "screen_shake_scale",
            ModSettingsText.I18N(settingsLoc, "screen_shake.label", "Screen Shake Scale"),
            shakeScale,
            minValue: 0.0,
            maxValue: 2.0,
            step: 0.05,
            valueFormatter: value => $"{value:0.00}x")
        .AddEnumChoice(
            "difficulty_mode",
            ModSettingsText.I18N(settingsLoc, "difficulty.label", "Difficulty"),
            difficulty,
            value => ModSettingsText.I18N(settingsLoc, $"difficulty.{value}", value.ToString()))));
```

`WithModDisplayName(...)` controls the label used in the left navigation. If it is omitted, RitsuLib falls back to the manifest name and then the mod id.

---

:::

## 最小示例{lang="zh-CN"}

::: zh-CN

先注册持久化数据：

```csharp
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

public sealed class MyModSettings
{
    public bool EnableFancyVfx { get; set; } = true;
    public double ScreenShakeScale { get; set; } = 1.0;
    public MyDifficultyMode DifficultyMode { get; set; } = MyDifficultyMode.Normal;
}

using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    var store = RitsuLibFramework.GetDataStore("MyMod");

    store.Register<MyModSettings>(
        key: "settings",
        fileName: "settings.json",
        scope: SaveScope.Global,
        defaultFactory: () => new MyModSettings(),
        autoCreateIfMissing: true);
}
```

然后创建绑定并注册设置页：

```csharp
using STS2RitsuLib.Settings;

var settingsLoc = RitsuLibFramework.CreateModLocalization(
    modId: "MyMod",
    instanceName: "MyMod-Settings",
    resourceFolders: ["MyMod.Localization.Settings"]);

var fancyVfx = ModSettingsBindings.Global<MyModSettings, bool>(
    "MyMod",
    "settings",
    model => model.EnableFancyVfx,
    (model, value) => model.EnableFancyVfx = value);

var shakeScale = ModSettingsBindings.Global<MyModSettings, double>(
    "MyMod",
    "settings",
    model => model.ScreenShakeScale,
    (model, value) => model.ScreenShakeScale = value);

var difficulty = ModSettingsBindings.Global<MyModSettings, MyDifficultyMode>(
    "MyMod",
    "settings",
    model => model.DifficultyMode,
    (model, value) => model.DifficultyMode = value);

RitsuLibFramework.RegisterModSettings("MyMod", page => page
    .WithModDisplayName(ModSettingsText.I18N(settingsLoc, "mod.display_name", "My Fancy Mod"))
    .WithTitle(ModSettingsText.I18N(settingsLoc, "page.title", "Settings"))
    .WithDescription(ModSettingsText.I18N(settingsLoc, "page.description", "Player-facing options for this mod."))
    .AddSection("general", section => section
        .WithTitle(ModSettingsText.I18N(settingsLoc, "general.title", "General"))
        .AddToggle(
            "fancy_vfx",
            ModSettingsText.I18N(settingsLoc, "fancy_vfx.label", "Fancy VFX"),
            fancyVfx,
            ModSettingsText.I18N(settingsLoc, "fancy_vfx.desc", "Enable additional visual polish."))
        .AddSlider(
            "screen_shake_scale",
            ModSettingsText.I18N(settingsLoc, "screen_shake.label", "Screen Shake Scale"),
            shakeScale,
            minValue: 0.0,
            maxValue: 2.0,
            step: 0.05,
            valueFormatter: value => $"{value:0.00}x")
        .AddEnumChoice(
            "difficulty_mode",
            ModSettingsText.I18N(settingsLoc, "difficulty.label", "Difficulty"),
            difficulty,
            value => ModSettingsText.I18N(settingsLoc, $"difficulty.{value}", value.ToString()))));
```

`WithModDisplayName(...)` 控制左侧导航中的 Mod 标签。若未设置，RitsuLib 会回退到 manifest 名称，再回退到 mod id。

---

:::

## Ordering And Navigation{lang="en"}

::: en

- **Mod groups**: call `WithModSidebarOrder(int)` on the page builder, or `ModSettingsRegistry.RegisterModSidebarOrder` / `RitsuLibFramework.RegisterModSettingsSidebarOrder`. Lower values appear earlier.
- **Pages within one mod**: use `WithSortOrder(int)` for sibling pages that share the same `ParentPageId`.
- **Child pages**: register the child separately with `AsChildOf(parentPageId)`, then link to it from the parent with `AddSubpage(...)`.

### Multiple Pages And Subpages

- **Default page id**: `RegisterModSettings("MyMod", configure)` uses `PageId == "MyMod"`.
- **Extra root pages**: call `RegisterModSettings("MyMod", configure, pageId: "audio")` and use `WithSortOrder(...)` to order multiple root pages.
- **Child page registration**: register the child in its own call and chain `AsChildOf("parentPageId")`.
- **Child UI**: child pages show a back control in the header; the sidebar tree still reflects the hierarchy.

---

:::

## 排序与导航{lang="zh-CN"}

::: zh-CN

- **Mod 分组**：在页面构建器上调用 `WithModSidebarOrder(int)`，或使用 `ModSettingsRegistry.RegisterModSidebarOrder` / `RitsuLibFramework.RegisterModSettingsSidebarOrder`。数值越小越靠前。
- **同一 Mod 内的页面**：对共享 `ParentPageId` 的兄弟页使用 `WithSortOrder(int)`。
- **子页**：子页需单独注册，并通过 `AsChildOf(parentPageId)` 绑定父页，再在父页中使用 `AddSubpage(...)` 跳转。

### 多页面与子页面

- **默认页面 id**：`RegisterModSettings("MyMod", configure)` 的 `PageId` 默认为 `"MyMod"`。
- **额外根页**：调用 `RegisterModSettings("MyMod", configure, pageId: "audio")`，并通过 `WithSortOrder(...)` 控制多个根页的顺序。
- **子页注册**：子页必须单独注册，并链式调用 `AsChildOf("parentPageId")`。
- **子页 UI**：子页标题栏提供返回控件，侧栏树仍保留完整层级。

---

:::

## Text Sources{lang="en"}

::: en

Use `ModSettingsText` so the page definition stays independent from how text is loaded.

- `Literal(...)`: simple hardcoded text or quick prototypes
- `I18N(...)`: mod-owned settings text
- `LocString(...)`: text already managed by the game localization pipeline
- `Dynamic(...)`: delegate resolved on each UI rebuild

Recommended split:

- gameplay and content-facing names -> `LocString`
- settings-only labels and descriptions -> `I18N`

---

:::

## 文本来源{lang="zh-CN"}

::: zh-CN

使用 `ModSettingsText`，可以让页面定义不依赖具体文本加载方式。

- `Literal(...)`：简单硬编码文本或快速原型
- `I18N(...)`：Mod 自有的设置界面文本
- `LocString(...)`：已纳入游戏本地化管线的文本
- `Dynamic(...)`：在每次 UI 刷新时通过委托重新生成文本

推荐分工：

- 游戏内容和内容名称 -> `LocString`
- 设置页专用标签与描述 -> `I18N`

---

:::

## Supported Controls{lang="en"}

::: en

- `AddToggle(...)` for `bool`
- `AddSlider(...)` for `double`
- `AddIntSlider(...)` for `int`
- `AddChoice(...)` / `AddEnumChoice(...)` for option lists; optional `ModSettingsChoicePresentation`: `Stepper` or `Dropdown`
- `AddColor(...)` for color strings
- `AddKeyBinding(...)` for binding strings
- `AddImage(...)` for a `Func<Texture2D?>` preview with height
- `AddButton(...)` for custom actions
- `AddSubpage(...)` to navigate to a registered child page
- `AddList(...)` for reorderable structured collections
- `AddHeader(...)` / `AddParagraph(...)` for explanatory structure
- collapsible sections via `.Collapsible(startCollapsed: false)` on the section builder

---

:::

## 支持的控件类型{lang="zh-CN"}

::: zh-CN

- `AddToggle(...)`：`bool`
- `AddSlider(...)`：`double`
- `AddIntSlider(...)`：`int`
- `AddChoice(...)` / `AddEnumChoice(...)`：候选列表；可选 `ModSettingsChoicePresentation`：`Stepper` 或 `Dropdown`
- `AddColor(...)`：颜色字符串
- `AddKeyBinding(...)`：按键绑定字符串
- `AddImage(...)`：通过 `Func<Texture2D?>` 提供图像预览
- `AddButton(...)`：自定义动作按钮
- `AddSubpage(...)`：跳转到已注册子页
- `AddList(...)`：可排序结构化集合
- `AddHeader(...)` / `AddParagraph(...)`：说明与结构辅助项
- 可折叠分区：在分区构建器上调用 `.Collapsible(startCollapsed: false)`

---

:::

## Structured Lists{lang="en"}

::: en

`AddList(...)` is the entry point for structured list editing.

It supports:

- add / remove / reorder
- nested list editors
- item-level structured copy / paste / duplicate
- custom item editors via `ModSettingsListItemContext<TItem>`

If the item type is structured, provide an item adapter so copy/paste and duplication can clone and serialize reliably.

---

:::

## 结构化列表{lang="zh-CN"}

::: zh-CN

`AddList(...)` 是结构化列表编辑入口。

它支持：

- 新增 / 删除 / 排序
- 嵌套列表编辑
- 列表项级复制 / 粘贴 / 创建副本
- 通过 `ModSettingsListItemContext<TItem>` 自定义列表项编辑器

如果列表项类型是结构化数据，建议提供 item adapter，以保证复制、粘贴和副本操作可以正确克隆与序列化。

---

:::

## Page Structure{lang="en"}

::: en

The UI hierarchy is:

- mod group
- page
- section
- entry

For most mods, one root page with several sections is sufficient. Introduce additional pages only when the content represents a distinct feature area.

Use:

- multiple pages for large feature areas
- `AddSubpage(...)` for drill-down flows
- collapsible sections for low-frequency settings
- lists when players edit collections rather than single values

---

:::

## 页面结构{lang="zh-CN"}

::: zh-CN

当前 UI 层级为：

- mod 分组
- page
- section
- entry

对于大多数 Mod，一个根页面配多个分区就足够。只有在功能区域明确分离时，才建议拆出额外页面。

适合使用的场景：

- 多页面：大型功能区分离
- `AddSubpage(...)`：钻取式设置流
- 可折叠 section：收纳低频选项
- 列表：编辑集合而非单个值

---

:::

## Scope Guidance{lang="en"}

::: en

Bindings preserve the scope of the underlying persisted value.

- `SaveScope.Global`: shared across all profiles
- `SaveScope.Profile`: varies by player profile

Typical usage:

- `Global`: graphics, accessibility, debug toggles, machine-level defaults
- `Profile`: profile-specific gameplay preferences or campaign-adjacent options

---

:::

## 作用域建议{lang="zh-CN"}

::: zh-CN

绑定会保留底层持久化值的作用域。

- `SaveScope.Global`：所有档位共享
- `SaveScope.Profile`：按玩家档位区分

常见用途：

- `Global`：画面、辅助功能、调试开关、机器级默认项
- `Profile`：按档位变化的玩法偏好或流程相关设置

---

:::

## What To Expose{lang="en"}

::: en

Good candidates for the settings UI:

- feature toggles
- cosmetic preferences
- accessibility adjustments
- gameplay options players are expected to tune

Poor candidates for the settings UI:

- caches
- migration bookkeeping
- runtime mirrors
- purely internal implementation state

The intended pattern is to persist a complete model, then expose only the user-editable subset.

---

:::

## 适合暴露到设置页的内容{lang="zh-CN"}

::: zh-CN

适合放入设置界面的内容：

- 功能开关
- 外观偏好
- 辅助功能调整项
- 玩家预期可调的玩法参数

不适合放入设置界面的内容：

- 缓存
- 迁移元数据
- 运行时镜像状态
- 纯内部实现字段

推荐模式是先持久化完整模型，再选择性暴露玩家真正需要调整的那部分。

---

:::

## Built-In Reference Page{lang="en"}

::: en

RitsuLib registers its own page as a reference implementation. It demonstrates persisted settings, preview-only bindings, collapsible sections, nested list editing, and item copy/paste workflows.

---

:::

## 内置参考页{lang="zh-CN"}

::: zh-CN

RitsuLib 自身注册了一页参考设置，用于展示已持久化设置、仅预览绑定、可折叠分区、嵌套列表编辑以及列表项复制粘贴工作流。

---

:::

## Related Docs{lang="en"}

::: en

- [Persistence Guide](/guide/persistence-guide)
- [Localization & Keywords](/guide/localization-and-keywords)
- [Lifecycle Events](/guide/lifecycle-events)
- [Patching Guide](/guide/patching-guide) (`Settings/Patches/ModSettingsUiPatches.cs` contains the menu entry and submenu injection)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [持久化设计](/guide/persistence-guide)
- [本地化与关键词](/guide/localization-and-keywords)
- [生命周期事件](/guide/lifecycle-events)
- [补丁系统](/guide/patching-guide)（`Settings/Patches/ModSettingsUiPatches.cs` 包含菜单入口与子菜单注入逻辑）

:::
