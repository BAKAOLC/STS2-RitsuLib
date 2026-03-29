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
| `RitsuLibFramework.RegisterModSettings(modId, configure, pageId?)` | Register one page; when `pageId` is omitted it defaults to `modId` |
| `RitsuLibFramework.GetRegisteredModSettings()` | Returns all registered pages (read-only) |
| `ModSettingsBindings.Global(...)` / `Profile(...)` | Bind a field to persisted data |
| `ModSettingsBindings.InMemory(...)` | Create preview-only or transient bindings |
| `ModSettingsText.Literal(...)` | Plain text |
| `ModSettingsText.LocString(...)` | Game localization text |
| `ModSettingsText.I18N(...)` | `I18N`-backed helper text |
| `ModSettingsText.Dynamic(...)` | Dynamic string resolved whenever the UI refreshes (useful with preview state) |
| `WithModDisplayName(...)` | Override the mod name shown in the sidebar |
| `WithSortOrder(...)` | Ordering when a mod registers multiple root pages (lower sorts earlier) |
| `AsChildOf(parentPageId)` | Mark this page as a child (must match `AddSubpage` `targetPageId` registration) |
| `section.Collapsible(startCollapsed?)` | Collapsible section; optional initial collapsed state |
| `AddToggle(...)`, `AddSlider(...)`, `AddIntSlider(...)`, `AddChoice(...)`, `AddEnumChoice(...)` | Standard value-entry builders |
| `AddColor(...)`, `AddKeyBinding(...)`, `AddImage(...)` | Color string, key binding, image preview |
| `AddButton(...)`, `AddHeader(...)`, `AddParagraph(...)` | Action and structure helpers |
| `AddSubpage(...)` | Navigate to a registered child page |
| `AddList(...)` | Structured, reorderable, nestable list editor |
| `ModSettingsUiActionRegistry.Register*ActionAppender(...)` | Append items to the Actions menu for rows, list items, pages, or sections |

---

## Recommended Flow

1. Register the persisted model in `ModDataStore`
2. Create bindings only for values that should be player-editable
3. Register pages and sections for those bindings
4. Localize all visible labels, descriptions, and option names

This gives you explicit control over what players can edit and how it is presented.

---

## In-game entry and UI behavior

- **Entry point**: Main menu → **Settings** → **General**. When at least one settings page is registered (`ModSettingsRegistry.HasPages`), the patch adds a **Mod Settings (RitsuLib)** row (divider + button) to that panel; pressing it pushes `RitsuModSettingsSubmenu` onto the main-menu submenu stack. If no mod registers a page, the row is **not** injected (avoids an empty screen).
- **Left sidebar**: Mod groups; only one mod expanded at a time. When expanded, shows a **root page** tree (parent/child depth). Under the selected page, quick **Section** buttons are listed.
- **Right pane**: Page header (child pages show a back affordance) and scrollable body. Clicking a section scrolls to its anchor; scrolling updates which section is highlighted on the left based on viewport position.
- **Persistence timing**: Dirty bindings are saved on a **~0.35s** debounce after edits. Closing the submenu, hiding it, leaving the scene tree, or **changing the game locale** triggers an **immediate** flush so values and layout stay consistent.

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

`WithModDisplayName(...)` controls the mod-group label shown in the left navigation.
If you omit it, RitsuLib falls back to the manifest name, then to the mod id.

---

## Sidebar ordering

- **Mod groups**: call `WithModSidebarOrder(int)` on the page builder, or `ModSettingsRegistry.RegisterModSidebarOrder` / `RitsuLibFramework.RegisterModSettingsSidebarOrder`. **Lower** values appear earlier. Mods without an explicit value use `0` and sort by **display name** (`WithModDisplayName` → manifest name → mod id).
- **Pages within one mod** (siblings sharing `ParentPageId`): use `WithSortOrder(int)` when building; after registration you can call `RegisterPageSortOrder`, `TryRegisterPageSortOrderAfter`, or `TryRegisterPageSortOrderBefore` (or the `RitsuLibFramework` wrappers) to pin a page before/after another.

---

## Text Sources

Use `ModSettingsText` so your page definition stays independent from how text is loaded.

- `Literal(...)`: simple hardcoded text or quick prototypes
- `I18N(...)`: mod-owned helper text and settings UI copy
- `LocString(...)`: text already managed by the game localization pipeline
- `Dynamic(...)`: delegate resolved on each UI rebuild (for descriptions that track live control state; see the built-in Debug Showcase)

Recommended split:

- gameplay and content-facing names -> `LocString`
- settings-only labels and descriptions -> `I18N`

---

## Supported Entry Types

- `AddToggle(...)` for `bool`
- `AddSlider(...)` for `double`
- `AddIntSlider(...)` for `int`
- `AddChoice(...)` / `AddEnumChoice(...)` for option lists; optional `ModSettingsChoicePresentation`: **Stepper** or **Dropdown**
- `AddColor(...)` for color strings (parsed and shown by the UI)
- `AddKeyBinding(...)` for binding strings (modifier combos, modifier-only, and left/right distinction are configurable)
- `AddImage(...)` for a `Func<Texture2D?>` preview with height
- `AddButton(...)` for custom actions (optional `ModSettingsButtonTone`)
- `AddSubpage(...)` to navigate to a registered child page (see **Multiple pages and subpages** below)
- `AddList(...)` for reorderable structured collections
- `AddHeader(...)` / `AddParagraph(...)` for explanatory structure
- **Collapsible sections**: inside `AddSection`, call `.Collapsible(startCollapsed: false)` (or `true` to start collapsed) on the section builder

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

### Multiple pages and subpages

- **Default page id**: `RegisterModSettings("MyMod", configure)` without a third argument uses `PageId == "MyMod"` (same as `ModSettingsPageBuilder`).
- **Extra root pages**: call again with `RegisterModSettings("MyMod", configure, pageId: "audio")` and use `WithSortOrder` to order multiple roots for the same mod in the sidebar.
- **Child page registration**: register the child in its own call and chain `AsChildOf("parentPageId")`, e.g. when the parent id is the default `"MyMod"`:  
  `RegisterModSettings("MyMod", p => p.AsChildOf("MyMod").WithTitle(...).AddSection(...), "my-child")`.  
  The parent links with `AddSubpage(..., targetPageId: "my-child", ...)`.
- **Child UI**: Child pages show a back control in the header; the sidebar tree still reflects the full hierarchy.

---

## Extending the Actions menu

Built-in copy/paste/reset flows are injected by the framework. To add commands for specific value types, list items, whole pages, or sections, use `ModSettingsUiActionRegistry`:

- `RegisterBindingActionAppender<TValue>(...)`
- `RegisterListItemActionAppender<TItem>(...)`
- `RegisterPageActionAppender(...)` / `RegisterSectionActionAppender(...)`

Callbacks receive `IModSettingsUiActionHost` so you can call `RequestRefresh()` and `MarkDirty(...)` to drive UI and saves.

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
- [Patching Guide](PatchingGuide.md) (settings entry and submenu injection live in `Settings/Patches/ModSettingsUiPatches.cs`, including General panel height refresh)
