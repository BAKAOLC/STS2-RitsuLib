# LocString Placeholder Resolution

This document covers two topics: the **game-native** localization system (`LocString`, SmartFormat configuration, built-in formatters) and the **extension guide** for registering custom `IFormatter` implementations from mods.

---

## Part 1: Game-native system

> The following describes the Slay the Spire 2 engine's own localization mechanism, not RitsuLib functionality.

### Core components

- **`LocString`**: holds a localization table id, entry key, and variable dictionary; `GetFormattedText()` triggers formatting.
- **`LocManager.SmartFormat`**: retrieves the raw template from `LocTable`, selects `CultureInfo` based on whether the key is localized, then calls `SmartFormatter.Format(...)`.
- **`LocManager.LoadLocFormatters`**: constructs `SmartFormatter`, registers data sources and formatter extensions.

### Variable binding

Variables are written to `LocString` via `Add`. **Spaces in variable names are replaced with hyphens.**

```csharp
var locString = new LocString("cards", "strike");
locString.Add("damage", 6);
string result = locString.GetFormattedText();
```

### Placeholder syntax

Game localization JSON uses SmartFormat placeholders.

**Variable only** — outputs the formatted value of the variable:

```
{VariableName}
```

**With formatter** — the formatter is specified after a colon using function-call syntax. The content inside `( )` is passed to the formatter as `IFormattingInfo.FormatterOptions`:

```
{VariableName:formatterName()}
{VariableName:formatterName(options)}
```

Formatters are matched by `IFormatter.Name`. The parentheses are a required part of the invocation syntax.

**Formatters with format segments** (e.g. `show`, `choose`, `cond`) receive additional text after a second colon, split by `|`. See individual formatter notes and the advanced examples below.

**Example:**

```json
{
  "damage_text": "Deal {Damage:diff()} damage to all enemies.",
  "energy_text": "Gain {Energy:energyIcons()} this turn."
}
```

### SmartFormat built-in extensions

Standard SmartFormat extensions registered by the game (non-exhaustive):

| Type | Role |
|------|------|
| `ListFormatter` | List formatting |
| `DictionarySource` | Keyed variable lookup |
| `ValueTupleSource` | Value tuples |
| `ReflectionSource` | Reflection-based property access |
| `DefaultSource` | Fallback source |
| `PluralLocalizationFormatter` | Locale-sensitive pluralization |
| `ConditionalFormatter` | Conditional formatting |
| `ChooseFormatter` | `choose(...)` |
| `SubStringFormatter` | Substrings |
| `IsMatchFormatter` | Regex matching |
| `LocaleNumberFormatter` | Locale number formatting |
| `DefaultFormatter` | Fallback when no formatter matches |

### Game-specific formatters

The game registers the following `IFormatter` types in `MegaCrit.Sts2.Core.Localization.Formatters`:

| `IFormatter.Name` | Placeholder | `FormatterOptions` | Notes |
|-------------------|-----------|--------------------|-------|
| `abs` | `{v:abs()}` | unused | Outputs the absolute value of a number |
| `energyIcons` | `{Energy:energyIcons()}` or `{energyPrefix:energyIcons(n)}` | Required as integer icon count when `CurrentValue` is `string` | Renders a value as energy icon glyphs; see details below |
| `starIcons` | `{v:starIcons()}` | unused | Renders a value as star icon glyphs |
| `diff` | `{v:diff()}` | unused | Highlights value changes (green for upgrades); requires `DynamicVar` |
| `inverseDiff` | `{v:inverseDiff()}` | unused | Same as `diff` with inverted color direction; requires `DynamicVar` |
| `percentMore` | `{v:percentMore()}` | unused | Converts a multiplier to a percent increase, e.g. `1.25` → `25` |
| `percentLess` | `{v:percentLess()}` | unused | Converts a multiplier to a percent decrease, e.g. `0.75` → `25` |
| `show` | `{v:show:upgrade text\|normal text}` | unused (options come from the format segment split on `|`) | Conditionally shows text based on upgrade state; requires `IfUpgradedVar` |

**`energyIcons` details**

The source of the icon count depends on `CurrentValue`:

- `EnergyVar`: uses `PreviewValue` and an optional color prefix. Use `{Energy:energyIcons()}`.
- `CalculatedVar` or numeric type: uses the numeric value directly. Use `{Energy:energyIcons()}`.
- `string` (e.g. the `energyPrefix` variable used in fixed-cost text): count is read from `FormatterOptions` and must be an integer literal, e.g. `{energyPrefix:energyIcons(1)}`.

Rendering rule: counts 1–3 repeat the icon glyph; counts ≤0 or ≥4 output the digit followed by one icon.

**`show` details**

The format segment after `show:` is split on `|` into one or two child formats:

- `Upgraded`: renders the first segment.
- `Normal`: renders the second segment; if only one segment is provided, nothing is rendered.
- `UpgradePreview`: renders the first segment wrapped in `[green]...[/green]`.

### DynamicVar types

`DynamicVar` subclasses carry metadata consumed by formatters such as `diff` and `inverseDiff`:

| Type | Description |
|------|-------------|
| `DamageVar` | Damage value with highlight metadata |
| `BlockVar` | Block value |
| `EnergyVar` | Energy value with color information |
| `CalculatedVar` | Base class for calculated values |
| `CalculatedDamageVar` / `CalculatedBlockVar` | Calculated damage / block |
| `ExtraDamageVar` | Extra damage |
| `BoolVar` / `IntVar` / `StringVar` | Primitive types |
| `GoldVar` / `HealVar` / `HpLossVar` / `MaxHpVar` | Resource types |
| `PowerVar<T>` | Power value (generic) |
| `StarsVar` / `CardsVar` | Stars / card references |
| `IfUpgradedVar` | Upgrade UI display state |
| `ForgeVar` / `RepeatVar` / `SummonVar` | Other card variables |

### Formatting pipeline

1. `LocString.GetFormattedText()` is called
2. `LocManager.SmartFormat` retrieves the raw template from `LocTable`
3. `CultureInfo` is selected based on whether the key is localized
4. `SmartFormatter.Format` evaluates placeholders and dispatches to matching formatters
5. On failure (`FormattingException` or `ParsingErrors`): error is logged and the raw template is returned

### Advanced examples

**Conditional** (`ConditionalFormatter`)

```json
{ "text": "{HasRider:This card has a rider effect|This card has no rider}" }
```

**Choose** (`ChooseFormatter`)

```json
{ "text": "{CardType:choose(Attack|Skill|Power):Attack text|Skill text|Power text}" }
```

**Nested formatters**

```json
{
  "text": "{Violence:Deal {Damage:diff()} damage {ViolenceHits:diff()} times|Deal {Damage:diff()} damage}"
}
```

**BBCode color tags**

```json
{ "text": "Gain [gold]{Gold}[/gold] gold. Current HP: [green]{Hp}[/green]." }
```

Common tags: `[gold]`, `[green]`, `[red]`, `[blue]`.

---

## Part 2: Custom formatters (mods)

> The following describes how to register additional formatters via the RitsuLib patching system.

A `Postfix` patch on `LocManager.LoadLocFormatters` provides access to the `SmartFormatter` instance, which accepts additional `IFormatter` implementations.

**Implementing `IFormatter`:**

```csharp
public class MyCustomFormatter : IFormatter
{
    public string Name { get => "myCustom"; set { } }
    public bool CanAutoDetect { get; set; }

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        formattingInfo.Write($"Custom output: {formattingInfo.CurrentValue}");
        return true;
    }
}
```

- `Name` is the formatter identifier matched in placeholder strings (the `myCustom` in `{Var:myCustom()}`).
- Access `formattingInfo.FormatterOptions` to read any text supplied inside the parentheses.

**Registration patch:**

```csharp
public class RegisterMyFormatterPatch : IPatchMethod
{
    public static string PatchId => "register_my_formatter";
    public static string Description => "Register custom SmartFormat formatter";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
        => [new(typeof(LocManager), "LoadLocFormatters")];

    public static void Postfix(SmartFormatter ____smartFormatter)
        => ____smartFormatter.AddExtensions(new MyCustomFormatter());
}
```

Once registered, invoke the formatter in JSON as `{SomeVar:myCustom()}` or `{SomeVar:myCustom(args)}`.

---

## Related documents

- [Localization & Keywords](LocalizationAndKeywords.md)
- [Card Dynamic Variables](CardDynamicVarToolkit.md)
- [Patching Guide](PatchingGuide.md)
- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
