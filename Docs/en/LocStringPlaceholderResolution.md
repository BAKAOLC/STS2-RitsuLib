# LocString Placeholder Resolution

This document has two parts:

1. **Game-native** placeholder resolution — how runtime text formatting works in the engine
2. **Extension guide** — how to register custom formatters via patches

---

## Part 1: Game-Native Placeholder System

> The following describes the Slay the Spire 2 engine's own localization resolution mechanism, not RitsuLib functionality.

### LocString Basics

`LocString` is the core localization type. It holds a reference to a localization table and key, plus a variable dictionary. When `GetFormattedText()` is called at runtime, variables are inserted into the text.

The actual placeholder resolution is handled by the `SmartFormat` library, configured with custom formatters registered in `LocManager.LoadLocFormatters`.

### Placeholder Syntax

Placeholders follow SmartFormat syntax:

- Simple variable: `{variableName}`
- Formatted variable: `{variableName:formatterName}`
- Formatted with options: `{variableName:formatterName:options}`

Example:

```json
{
  "damage_text": "Deal {damage} damage to all enemies.",
  "energy_text": "Gain {energy:energyIcons} this turn."
}
```

### Variable Storage

Variables are stored in a dictionary within the LocString instance:

```csharp
var locString = new LocString("cards", "strike");
locString.Add("damage", 6);
locString.Add("target", "enemy");
string result = locString.GetFormattedText();
```

The `Add` method stores named values. Spaces in variable names are replaced with hyphens.

### Built-in SmartFormat Formatters

Standard SmartFormat extensions registered by the game:

| Formatter | Description |
|---|---|
| `ListFormatter` | List formatting |
| `DictionarySource` | Dictionary reading |
| `ValueTupleSource` | Value tuple handling |
| `ReflectionSource` | Reflection-based property access |
| `DefaultSource` | Default source handler |
| `PluralLocalizationFormatter` | Locale-based pluralization |
| `ConditionalFormatter` | Conditional formatting |
| `ChooseFormatter` | Choice formatting |
| `SubStringFormatter` | Substring extraction |
| `IsMatchFormatter` | Regex matching |
| `DefaultFormatter` | Default formatting handler |

### Game-Specific Formatters

Custom formatters registered by Slay the Spire 2:

#### `abs` — AbsoluteValueFormatter

Formats numeric values as their absolute values.

```json
{ "text": "Lose {damage:abs} HP." }
```

#### `energyIcons` — EnergyIconsFormatter

Converts energy values to energy icon images.

```json
{ "text": "Gain {energy:energyIcons} this turn." }
```

- Values 1–3: displayed as individual icons
- Values ≥ 4: number followed by a single icon
- Uses character-specific energy icon colors when available

#### `starIcons` — StarIconsFormatter

Converts numeric values to star icon images.

```json
{ "text": "Upgrade {count:starIcons} cards." }
```

#### `diff` — HighlightDifferencesFormatter

Highlights value changes with color coding (typically green for upgrades).

```json
{ "text": "Damage: {damage:diff}" }
```

#### `inverseDiff` — HighlightDifferencesInverseFormatter

Highlights value changes with inverse color coding.

```json
{ "text": "Cost: {cost:inverseDiff}" }
```

#### `percentMore` — PercentMoreFormatter

Converts a multiplier to a percentage increase. For value `1.25`, outputs `25`.

```json
{ "text": "Deal {multiplier:percentMore}% more damage." }
```

#### `percentLess` — PercentLessFormatter

Converts a multiplier to a percentage decrease. For value `0.75`, outputs `25`.

```json
{ "text": "Costs {discount:percentLess}% less." }
```

#### `show` — ShowIfUpgradedFormatter

Conditionally displays content based on upgrade state, using pipe `|` as delimiter.

```json
{ "text": "{var:show:Upgrade text|Normal text}" }
```

- When upgraded: shows content before `|`
- When normal: shows content after `|`
- When previewing upgrade: shows upgrade text in green

### DynamicVar Types

The game uses `DynamicVar` subclasses that carry extra metadata for formatters:

| Type | Description |
|---|---|
| `DamageVar` | Damage values with highlighting |
| `BlockVar` | Block values |
| `EnergyVar` | Energy values with color info |
| `CalculatedVar` | Calculated values (intermediate base class) |
| `CalculatedDamageVar` | Calculated damage |
| `CalculatedBlockVar` | Calculated block |
| `ExtraDamageVar` | Extra damage values |
| `BoolVar` | Boolean values |
| `IntVar` | Integer values |
| `StringVar` | String values |
| `GoldVar` | Gold amounts |
| `HealVar` | Healing amounts |
| `HpLossVar` | HP loss |
| `MaxHpVar` | Max HP values |
| `PowerVar<T>` | Power values (generic) |
| `StarsVar` | Star counts |
| `CardsVar` | Card references |
| `IfUpgradedVar` | Upgrade state indicator |
| `ForgeVar` | Forge values |
| `RepeatVar` | Repeat counts |
| `SummonVar` | Summon values |

### Formatting Pipeline

1. `LocString.GetFormattedText()` is called
2. `LocManager.SmartFormat()` retrieves raw text from the localization table
3. Appropriate `CultureInfo` is selected based on whether the key is localized
4. `SmartFormatter.Format()` processes text with variables
5. Custom formatters are applied as specified in format strings
6. If formatting fails, raw text is returned and an error is logged

### Error Handling

When formatting fails:

1. Exception is caught (`FormattingException` or `ParsingErrors`)
2. Error message is logged with table, key, and variables
3. Sentry event fingerprint is created based on the error pattern
4. Raw text is returned as fallback

This ensures localization errors don't crash the game.

### Advanced Syntax

The game supports complex nested formatting patterns:

#### Conditional Formatting

```json
{ "text": "{HasRider:This card has a rider effect|This card has no rider}" }
```

#### Choice Formatting

```json
{ "text": "{CardType:choose(Attack|Skill|Power):Attack text|Skill text|Power text}" }
```

#### Nested Formatters

```json
{
  "text": "{Violence:Deal {Damage:diff()} damage {ViolenceHits:diff()} times|Deal {Damage:diff()} damage}"
}
```

#### BBCode Color Tags

```json
{ "text": "Gain [gold]Block[/gold] equal to [green]{value}[/green]" }
```

Common color tags:

- `[gold]...[/gold]` — Gold highlighting
- `[green]...[/green]` — Green highlighting (buffs)
- `[red]...[/red]` — Red highlighting (debuffs)

---

## Part 2: Adding Custom Formatters for Mods

> The following describes how to extend the game's formatter registration via patches, using the RitsuLib patching system.

### Steps

1. Create a class implementing `SmartFormat.Core.Extensions.IFormatter`
2. Set `Name` to the formatter's identifier
3. Implement `TryEvaluateFormat` for formatting logic
4. Register the formatter via a patch on `LocManager.LoadLocFormatters`

Example:

```csharp
public class MyCustomFormatter : IFormatter
{
    public string Name { get => "myCustom"; set { } }
    public bool CanAutoDetect { get; set; }

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var value = formattingInfo.CurrentValue;
        formattingInfo.Write($"Processed: {value}");
        return true;
    }
}
```

Registration patch example (using RitsuLib patching system):

```csharp
public class RegisterMyFormatterPatch : IPatchMethod
{
    public static string PatchId => "register_my_formatter";
    public static string Description => "Register custom SmartFormat formatter";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(LocManager), "LoadLocFormatters")];
    }

    public static void Postfix(SmartFormatter ____smartFormatter)
    {
        ____smartFormatter.AddExtensions(new MyCustomFormatter());
    }
}
```

---

## Related Documents

- [Localization & Keywords](LocalizationAndKeywords.md)
- [Card Dynamic Variables](CardDynamicVarToolkit.md)
- [Patching Guide](PatchingGuide.md)
- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
