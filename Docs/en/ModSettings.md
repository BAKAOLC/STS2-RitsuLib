# Mod Settings

RitsuLib provides a settings UI layer for player-editable values.
It is built on top of `ModDataStore`, but it does not replace the persistence model.

Use this system when you need to expose a selected subset of persisted values, organize them into pages and sections, and localize the visible text. Settings pages are registered explicitly by design.

---

## Architecture

Keep these responsibilities separate:

- `ModDataStore`: persistence, scopes, defaults, migrations
- `IModSettingsValueBinding<T>`: read/write bridge between UI and stored data
- page and section builders: UI structure and ordering
- `ModSettingsText`: text source abstraction for labels and descriptions

This separation prevents runtime state, internal metadata, and user-editable configuration from collapsing into one model.

---

## Core APIs

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

## Recommended Flow

1. Register the complete persisted model in `ModDataStore`.
2. Create bindings only for fields that players should edit.
3. Register pages and sections around those bindings.
4. Localize all visible labels, descriptions, and option names.

The result is an explicit contract between stored data and the settings UI.

---

## UI Behavior

- Entry point: Main menu -> `Settings` -> `General`. When at least one page is registered, RitsuLib injects a `Mod Settings (RitsuLib)` row that opens `RitsuModSettingsSubmenu`.
- Sidebar: grouped by mod. One mod group is expanded at a time. The selected page also exposes section shortcuts.
- Content pane: page header, optional back navigation for child pages, and a scrollable section body.
- Save timing: dirty bindings are flushed on a debounce of about `0.35s`. Closing or hiding the submenu, leaving the tree, or changing the game locale forces an immediate flush.

`WithVisibleWhen(...)` and row-level `visibleWhen` predicates are re-evaluated on debounced refresh. Predicates should stay cheap and should not throw. If evaluation fails, the control remains visible.

---

## Auto-Mirror Policy (BaseLib / ModConfig)

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

## Runtime Reflection Protocol (No Library Reference)

Besides BaseLib / ModConfig mirrors, RitsuLib also supports a pure reflection protocol for settings pages.  
Your mod does not need to reference `STS2RitsuLib`; you only need assembly metadata that points to a provider type:

```xml
<ItemGroup>
  <AssemblyMetadata Include="RitsuLib.ModSettingsInterop.ProviderType" Value="YourMod.Scripts.RitsuLibModSettingsInteropProvider" />
</ItemGroup>
```

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

## Minimal Example

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

## Ordering And Navigation

- **Mod groups**: call `WithModSidebarOrder(int)` on the page builder, or `ModSettingsRegistry.RegisterModSidebarOrder` / `RitsuLibFramework.RegisterModSettingsSidebarOrder`. Lower values appear earlier.
- **Pages within one mod**: use `WithSortOrder(int)` for sibling pages that share the same `ParentPageId`.
- **Child pages**: register the child separately with `AsChildOf(parentPageId)`, then link to it from the parent with `AddSubpage(...)`.

### Multiple Pages And Subpages

- **Default page id**: `RegisterModSettings("MyMod", configure)` uses `PageId == "MyMod"`.
- **Extra root pages**: call `RegisterModSettings("MyMod", configure, pageId: "audio")` and use `WithSortOrder(...)` to order multiple root pages.
- **Child page registration**: register the child in its own call and chain `AsChildOf("parentPageId")`.
- **Child UI**: child pages show a back control in the header; the sidebar tree still reflects the hierarchy.

---

## Text Sources

Use `ModSettingsText` so the page definition stays independent from how text is loaded.

- `Literal(...)`: simple hardcoded text or quick prototypes
- `I18N(...)`: mod-owned settings text
- `LocString(...)`: text already managed by the game localization pipeline
- `Dynamic(...)`: delegate resolved on each UI rebuild

Recommended split:

- gameplay and content-facing names -> `LocString`
- settings-only labels and descriptions -> `I18N`

---

## Supported Controls

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

## Structured Lists

`AddList(...)` is the entry point for structured list editing.

It supports:

- add / remove / reorder
- nested list editors
- item-level structured copy / paste / duplicate
- custom item editors via `ModSettingsListItemContext<TItem>`

If the item type is structured, provide an item adapter so copy/paste and duplication can clone and serialize reliably.

---

## Page Structure

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

## Scope Guidance

Bindings preserve the scope of the underlying persisted value.

- `SaveScope.Global`: shared across all profiles
- `SaveScope.Profile`: varies by player profile

Typical usage:

- `Global`: graphics, accessibility, debug toggles, machine-level defaults
- `Profile`: profile-specific gameplay preferences or campaign-adjacent options

---

## What To Expose

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

## Built-In Reference Page

RitsuLib registers its own page as a reference implementation. It demonstrates persisted settings, preview-only bindings, collapsible sections, nested list editing, and item copy/paste workflows.

---

## Related Docs

- [Persistence Guide](PersistenceGuide.md)
- [Localization & Keywords](LocalizationAndKeywords.md)
- [Lifecycle Events](LifecycleEvents.md)
- [Patching Guide](PatchingGuide.md) (`Settings/Patches/ModSettingsUiPatches.cs` contains the menu entry and submenu injection)
