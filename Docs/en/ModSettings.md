# Mod Settings

RitsuLib provides a player-facing settings API for values that players should edit in-game.
It sits on top of `ModDataStore`, but it does not replace persistence.

Use this layer when you want to:

- expose a curated subset of persisted values to players
- organize settings into pages, sections, and subpages
- localize labels and descriptions cleanly
- support structured editors such as reorderable lists

Do not use it as an automatic schema-to-UI generator.
Every setting is registered explicitly on purpose.

---

## Mental Model

Keep these responsibilities separate:

- `ModDataStore`: persistence, scopes, migrations, defaults
- `IModSettingsValueBinding<T>`: read/write bridge between UI and data
- settings page builders: page structure and player-facing presentation
- `ModSettingsText`: text source abstraction for labels and descriptions

That separation keeps internal data, runtime caches, and player configuration from collapsing into one unstructured blob.

---

## Main APIs

| API | Purpose |
|---|---|
| `RitsuLibFramework.RegisterModSettings(...)` | Register a settings page |
| `ModSettingsBindings.Global(...)` / `Profile(...)` | Bind a field to persisted data |
| `ModSettingsBindings.InMemory(...)` | Create preview-only or transient bindings |
| `ModSettingsText.Literal(...)` | Plain text |
| `ModSettingsText.LocString(...)` | Game localization text |
| `ModSettingsText.I18N(...)` | `I18N`-backed helper text |
| `WithModDisplayName(...)` | Override the mod name shown in the sidebar |
| `AddToggle(...)`, `AddSlider(...)`, `AddIntSlider(...)`, `AddChoice(...)`, `AddEnumChoice(...)` | Standard value-entry builders |
| `AddButton(...)`, `AddHeader(...)`, `AddParagraph(...)` | Action and structure helpers |
| `AddSubpage(...)` | Link to a child page |
| `AddList(...)` | Structured, reorderable, nestable list editor |

---

## Recommended Flow

1. Register the persisted model in `ModDataStore`
2. Create bindings only for values that should be player-editable
3. Register pages and sections for those bindings
4. Localize all visible labels, descriptions, and option names

This gives you explicit control over what players can edit and how it is presented.

---

## Minimal Example

First register persisted data:

```csharp
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

public sealed class MyModSettings
{
    public bool EnableFancyVfx { get; set; } = true;
    public float ScreenShakeScale { get; set; } = 1.0f;
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

var shakeScale = ModSettingsBindings.Global<MyModSettings, float>(
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
            minValue: 0.0f,
            maxValue: 2.0f,
            step: 0.05f,
            valueFormatter: value => $"{value:0.00}x")
        .AddEnumChoice(
            "difficulty_mode",
            ModSettingsText.I18N(settingsLoc, "difficulty.label", "Difficulty"),
            difficulty,
            value => ModSettingsText.I18N(settingsLoc, $"difficulty.{value}", value.ToString()))));
```

`WithModDisplayName(...)` controls the mod-group label shown in the left navigation.
If you omit it, RitsuLib falls back to the manifest name, then to the mod id.

---

## Text Sources

Use `ModSettingsText` so your page definition stays independent from how text is loaded.

- `Literal(...)`: simple hardcoded text or quick prototypes
- `I18N(...)`: mod-owned helper text and settings UI copy
- `LocString(...)`: text already managed by the game localization pipeline

Recommended split:

- gameplay and content-facing names -> `LocString`
- settings-only labels and descriptions -> `I18N`

---

## Supported Entry Types

- `AddToggle(...)` for `bool`
- `AddSlider(...)` for `float`
- `AddIntSlider(...)` for `int`
- `AddChoice(...)` for typed option lists
- `AddEnumChoice(...)` for enum-backed choices
- `AddButton(...)` for reset, sync, import, export, and helper actions
- `AddSubpage(...)` for child-page navigation
- `AddList(...)` for reorderable structured collections
- `AddHeader(...)` / `AddParagraph(...)` for explanatory structure

---

## Structured Lists

`AddList(...)` is the framework entry point for structured list editing.

It supports:

- add / remove / reorder
- nested list editors
- item-level structured copy / paste / duplicate
- custom item editors via `ModSettingsListItemContext<TItem>`

Typical use cases:

- weighted pools
- ordered rule chains
- per-item configuration blocks
- nested presets or tag lists

If your item type is structured, provide an item adapter so copy/paste and duplication can clone and serialize reliably.

---

## Page Structure Guidance

The UI is organized as:

- mod group
- page
- section
- entry

In most mods, one root page with several sections is enough.
Reach for additional pages only when content is genuinely separate.

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

Typical examples:

- `Global`: graphics, accessibility, debug toggles, machine-level defaults
- `Profile`: profile-specific gameplay preferences or campaign-adjacent options

---

## What Should Be Exposed

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

The intended pattern is to persist a complete model, then expose only the player-facing subset.

---

## Built-In Reference

RitsuLib registers its own page as a live reference implementation.

The built-in reference currently demonstrates:

- persisted framework options
- transient preview controls
- collapsible sections
- nested structured list editing
- copy / paste / duplicate item workflows

Use it as a behavior reference when designing your own settings pages.

---

## Related Docs

- [Persistence Guide](PersistenceGuide.md)
- [Localization & Keywords](LocalizationAndKeywords.md)
- [Lifecycle Events](LifecycleEvents.md)
