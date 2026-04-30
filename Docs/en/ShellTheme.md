# Shell Theme (Settings UI Theme System)

The settings UI shell (`RitsuModSettingsSubmenu`, sidebars, panels, lists, controls) renders against
a theme system based on the
[W3C Design Tokens Format Module](https://www.w3.org/community/design-tokens/) (DTFM).

What it gives you:

1. **Structured colors / fonts / metrics** — every visual constant is exposed as a named token, no
   ad-hoc hardcoded values.
2. **Layered overrides** — snapshots are built by merging in the order
   `mod-registered defaults → inheritance chain → scopes`, so the same UI can be adjusted globally,
   per shell, per ModSettings page, or per mod.
3. **Extensible** — mods can ship additional data via `extensions.<modId>` in theme JSON **and**
   register their own DTFM defaults at runtime; RitsuLib handles merging, reference resolution and
   refresh notifications.

---

## File locations

- Built-in themes: `sts-2-ritsulib/Ui/Shell/Themes/{default,warm,oled}.theme.json`, packed as
  embedded resources into the assembly.
- User / mod-supplied themes: `user://<global save path>/shell_themes/`. The directory is created
  on demand by `RitsuShellThemePaths.TryEnsureShellThemesDirectory`. On startup RitsuLib copies the
  embedded themes into this folder so players can edit them directly.
- The selected theme id is persisted in the global save (`UiShellThemeId`).
- JSON Schema: `sts-2-ritsulib/schemas/ui/shell/v1/schema.json`. Reference it from a `$schema`
  property to get IDE autocomplete and validation.

---

## Top-level document layout

```jsonc
{
  "$schema": "https://raw.githubusercontent.com/BAKAOLC/STS2-RitsuLib/main/schemas/ui/shell/v1/schema.json",
  "themeFormatVersion": 1,
  "id": "warm",                // required, lowercase identifier
  "displayName": "Warm",       // human-readable label
  "inherits": "default",       // parent theme id; null = root theme

  "core":       { /* primitives */ },
  "semantic":   { /* aliases, intent-named */ },
  "components": { /* component tokens */ },

  "scopes": {
    "global":      { /* applied everywhere */ },
    "shell":       { /* shell-only overlay */ },
    "modSettings": { /* RitsuModSettingsSubmenu overlay */ },
    "mod:my_mod":  { /* visible to my_mod only */ }
  },

  "extensions": {
    "my_mod": { "anything": { "goes": true } }
  }
}
```

### Three-layer token model

| Layer        | Role                                                            | Examples                                |
| ------------ | --------------------------------------------------------------- | --------------------------------------- |
| `core`       | Primitives — palette ramps, scales, font families               | `core.color.amber.500`, `core.size.2`   |
| `semantic`   | Aliases — intent-named, prefer references into `core`            | `semantic.color.surface.default`        |
| `components` | Component tokens — `component → variant → state`                | `components.button.primary.hover.bg`    |

Convention: UI consumes only `semantic` and `components`. When you need a new value, add a
primitive to `core` and reference it from `semantic` / `components`.

### Leaf tokens

Strict DTFM: every leaf is an object with required `$value` / `$type` and optional
`$description` / `$extensions`:

```jsonc
{
  "$value": "#F59E0B",
  "$type": "color",
  "$description": "Warm accent base"
}
```

Supported `$type` values:

- `color` — hex string, alpha optional (`#RRGGBB` / `#RRGGBBAA`).
- `dimension` — numeric pixel value (int or float).
- `fontFamily` — string path; supports `res://`, `user://` and absolute paths.
- `fontWeight` — integer.
- `number` — unitless float (line-height multipliers etc.).
- `boolean` — boolean (used for switches such as `sidebar.showInlinePageCount`).

### Reference syntax

A leaf's `$value` can be a `"{a.b.c}"` path that points at another leaf in
`core / semantic / components / scopes`. Cycles are forbidden;
`RitsuShellThemeReferenceResolver` resolves all references after the merge phase.

```jsonc
"semantic": {
  "color": {
    "accent": {
      "default": { "$value": "{core.color.amber.500}", "$type": "color" }
    }
  }
}
```

---

## Merge order (inheritance + scopes)

`RitsuShellThemeCatalog.TryBuildSnapshot` builds a snapshot by writing into the same token tree in
this order:

1. All mod defaults registered through `RegisterModTokens`.
2. The inheritance chain `inherits → ... → leaf theme`. For each document, `core / semantic /
   components` branches are deep-merged into the tree.
3. Inside each document, scopes are overlaid in this order: `global → shell → modSettings →
   mod:<id>`.
4. Once merging is complete, references are resolved globally; `RitsuShellThemeBuilder` then
   constructs the immutable `RitsuShellTheme` snapshot.

Merge semantics:

- Group nodes (plain JSON objects) merge recursively by key.
- Leaf tokens (`$value` / `$type` shape) replace the previous value wholesale; no field-level
  merging is attempted.
- Arrays replace wholesale.

> ⚠️ Today RitsuLib itself only consumes the `global / shell / modSettings / mod:<id>` scopes.
> If a mod needs its own scope, prefer `mod:<modId>` since it already participates in the merge
> pipeline.

---

## C# API overview

### 1. Current snapshot and event

```csharp
using STS2RitsuLib.Ui.Shell.Theme;

var theme = RitsuShellTheme.Current;        // same as RitsuShellThemeRuntime.Current
RitsuShellThemeRuntime.ThemeChanged += () => ApplyMyChrome(RitsuShellTheme.Current);
RitsuShellThemeRuntime.ApplyThemeId("warm");
RitsuShellThemeRuntime.ReapplyActiveTheme(forceReloadCatalog: true); // re-read disk
```

`RitsuShellTheme` is immutable — every theme change or mod registration produces a fresh
instance. Cache tokens only inside a `ThemeChanged` handler so they refresh automatically.

### 2. Typed access (preferred for built-in tokens)

`RitsuShellTheme` exposes strongly-typed records grouped by intent:

```csharp
var t = RitsuShellTheme.Current;

// Colors
t.Color.White
t.Color.Divider
t.Color.Shadow.Ambient

// Text
t.Text.LabelPrimary
t.Text.HoverHighlight

// Surface
t.Surface.Sidebar
t.Surface.Entry.Bg
t.Surface.Framed.Border

// Components & states
t.Component.Toggle.On.Bg
t.Component.SidebarBtn.SelectedHover.Border
t.Component.TextButton.Accent.Fg

// Metrics
t.Metric.Radius.Default
t.Metric.BorderWidth.Overlay
t.Metric.FontSize.Button
t.Metric.Sidebar.Width
t.Metric.Sidebar.ShowInlinePageCount     // bool

// Fonts
t.Font.Body
t.Font.BodyBold
t.Font.Button
```

The full record definitions live under
[`Ui/Shell/Theme/Tokens/`](../../Ui/Shell/Theme/Tokens/) — `ColorTokens.cs`, `ComponentTokens.cs`,
`MetricTokens.cs`, `FontTokens.cs`.

### 3. Path-string access (preferred for mod extensions or rarely-touched tokens)

When a token comes from a mod-supplied subtree (no typed record exists), use the dynamic API:

```csharp
var theme = RitsuShellTheme.Current;

Color  bg     = theme.GetColor("components.relicPicker.selected.bg");
float  height = theme.GetDimension("semantic.metrics.relicPicker.rowMinHeight");
int    radius = theme.GetDimensionInt("semantic.metrics.radius.default");
bool   inline = theme.GetBool("semantic.metrics.sidebar.showInlinePageCount");
Font   body   = theme.GetFontFamily("semantic.fontFamily.body");

if (theme.TryGetExtension("my_mod", out var json))
{
    // json is System.Text.Json.JsonElement, the merged extensions.my_mod subtree
}
```

Lookup rules:

- Paths are case-sensitive and use the merged JSON key names (segments joined with `.`).
- On miss / type mismatch the API returns a safe default (`Magenta`, `0`, `false`, the shared
  fallback font).
- All numeric values flow through `GetDimension*`; only the path string changes.

### 4. Mod registration API

`RitsuShellThemeRuntime` exposes a registration surface that lets mods supply defaults without
shipping their own theme files:

```csharp
public static class RitsuShellThemeRuntime
{
    public static void RegisterModTokens(string modId,
        JsonElement? defaults,
        Action<RitsuShellTheme>? onApply = null);

    public static void UnregisterModTokens(string modId);
}
```

- `defaults` — a DTFM-shaped JSON subtree (object preferred, may contain
  `core / semantic / components / extensions`). It is merged **before** any theme document, so
  matching keys in user themes always win.
- `onApply` — invoked once after every snapshot rebuild with the latest `RitsuShellTheme`.
  Registration triggers an immediate rebuild internally, so `onApply` fires once on register too.
- Unregistering rebuilds the snapshot so the defaults disappear.

---

## End-to-end example: register a relic-picker theme from a mod

Imagine a mod `my_mod` that ships a relic picker. It needs:

- Its own `components.relicPicker.{default,hover,selected}` tokens.
- A boolean toggle that players can flip via `extensions.my_mod`.

### Step 1: prepare the DTFM defaults

```csharp
using System.Text.Json;
using STS2RitsuLib.Ui.Shell.Theme;

private const string MyDefaults = """
{
  "core": {
    "color": {
      "myMod": {
        "indigo": {
          "500": { "$value": "#5C5CD6", "$type": "color" }
        }
      }
    }
  },
  "semantic": {
    "color": {
      "myMod": {
        "accent": { "$value": "{core.color.myMod.indigo.500}", "$type": "color" }
      }
    },
    "metrics": {
      "relicPicker": {
        "rowMinHeight": { "$value": 56, "$type": "dimension" }
      }
    }
  },
  "components": {
    "relicPicker": {
      "default":  { "bg": { "$value": "#1B1F2BF5", "$type": "color" } },
      "hover":    { "bg": { "$value": "#27314AFA", "$type": "color" } },
      "selected": { "bg": { "$value": "{semantic.color.myMod.accent}", "$type": "color" } }
    }
  },
  "extensions": {
    "my_mod": {
      "showHotkeyHint": true
    }
  }
}
""";
```

### Step 2: register at mod init

```csharp
public sealed class MyModBootstrap : IModInit
{
    public void OnInit()
    {
        using var doc = JsonDocument.Parse(MyDefaults);
        RitsuShellThemeRuntime.RegisterModTokens(
            modId: "my_mod",
            defaults: doc.RootElement.Clone(),
            onApply: ApplyTheme);
    }

    private static void ApplyTheme(RitsuShellTheme theme)
    {
        // Called once after every snapshot rebuild
        var bg = theme.GetColor("components.relicPicker.selected.bg");
        var height = theme.GetDimension("semantic.metrics.relicPicker.rowMinHeight");
        var showHint = theme.TryGetExtension("my_mod", out var ext) &&
                       ext.TryGetProperty("showHotkeyHint", out var p) &&
                       p.ValueKind == JsonValueKind.True;
        // ... refresh whatever UI depends on these tokens
    }
}
```

### Step 3 (optional): player override

A player can drop the same keys into `user://shell_themes/default.theme.json`:

```jsonc
{
  "id": "default",
  "components": {
    "relicPicker": {
      "selected": { "bg": { "$value": "#FF8A00", "$type": "color" } }
    }
  },
  "extensions": {
    "my_mod": { "showHotkeyHint": false }
  }
}
```

Because the merge order is `mod defaults → theme chain → scopes`, the player's value always wins
over `RegisterModTokens`.

---

## Validation and troubleshooting

- If a theme file fails to parse, `RitsuShellThemeCatalog` skips it and keeps the previous
  snapshot. Force a re-read with
  `RitsuShellThemeRuntime.ReapplyActiveTheme(forceReloadCatalog: true)`.
- Reference cycles and missing references are recorded as errors internally; missing values fall
  back to safe defaults rather than crashing.
- Always declare `$schema` at the top of theme files so the IDE can validate keys and `$type`.
- For diagnostics, inspect `RitsuShellTheme.Current.Id` to confirm which theme is active and
  `ListExtensionModIds()` to see which mod extensions are merged.

---

## Style guidelines

- **Extend `semantic` first, reference it from `components`.** Color tweaks should live in one
  place.
- **Ship sensible defaults from mods via `RegisterModTokens`.** Players should not have to edit a
  theme file to get a working initial appearance.
- **Don't mutate `RitsuShellTheme.Current`.** Snapshots are immutable — request a rebuild via a new
  theme file, scope, or `RegisterModTokens` and let `ReapplyActiveTheme` notify listeners.
- **Avoid hardcoded colors / sizes.** When the token you need doesn't exist, add it to `semantic`
  or `components` instead of inlining a literal.
