# LocString Placeholder Resolution

The game's localization system uses a sophisticated placeholder resolution mechanism to dynamically insert values into localized text. This document explains how the system works and how to extend it with custom formatters.

---

## Overview

LocString is the core localization type used throughout the game. It holds a reference to a localization table and key, along with a dictionary of variables that can be inserted into the text at runtime.

The actual placeholder resolution is handled by the `SmartFormat` library, which is configured with custom formatters specific to Slay the Spire 2's needs.

---

## Basic Placeholder Syntax

Placeholders in localized text follow the SmartFormat syntax:

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

---

## Variable Storage

Variables are stored in a dictionary within the LocString instance:

```csharp
var locString = new LocString("cards", "strike");
locString.Add("damage", 6);
locString.Add("target", "enemy");
string result = locString.GetFormattedText();
```

The `Add` method stores values with their names. Variable names are normalized by replacing spaces with hyphens.

---

## Custom Formatters

The game registers several custom formatters in `LocManager.LoadLocFormatters`:

### Built-in SmartFormat Formatters

These are standard SmartFormat extensions:

- **ListFormatter** - Handles list formatting
- **DictionarySource** - Reads from dictionaries
- **ValueTupleSource** - Handles value tuples
- **ReflectionSource** - Uses reflection for property access
- **DefaultSource** - Default source handler
- **PluralLocalizationFormatter** - Pluralization based on locale
- **ConditionalFormatter** - Conditional formatting
- **ChooseFormatter** - Choice formatting
- **SubStringFormatter** - Substring extraction
- **IsMatchFormatter** - Regex matching
- **DefaultFormatter** - Default formatting handler

### Game-Specific Formatters

These are custom formatters for Slay the Spire 2:

#### AbsoluteValueFormatter (`abs`)
Formats numeric values as their absolute values.

```json
{
  "text": "Lose {damage:abs} HP."
}
```

#### EnergyIconsFormatter (`energyIcons`)
Converts energy values to energy icon images.

```json
{
  "text": "Gain {energy:energyIcons} this turn."
}
```

- Values 1-3 are displayed as individual icons
- Values ≥4 show the number followed by a single icon
- Uses character-specific energy icon colors when available

#### StarIconsFormatter (`starIcons`)
Converts numeric values to star icon images.

```json
{
  "text": "Upgrade {count:starIcons} cards."
}
```

#### HighlightDifferencesFormatter (`diff`)
Highlights value changes with color coding (typically green for upgrades).

```json
{
  "text": "Damage: {damage:diff}"
}
```

#### HighlightDifferencesInverseFormatter (`inverseDiff`)
Highlights value changes with inverse color coding.

```json
{
  "text": "Cost: {cost:inverseDiff}"
}
```

#### PercentMoreFormatter (`percentMore`)
Converts a multiplier to a percentage increase.

```json
{
  "text": "Deal {multiplier:percentMore}% more damage."
}
```

For a value of 1.25, this outputs "25".

#### PercentLessFormatter (`percentLess`)
Converts a multiplier to a percentage decrease.

```json
{
  "text": "Costs {discount:percentLess}% less."
}
```

For a value of 0.75, this outputs "25".

#### ShowIfUpgradedFormatter (`show`)
Conditionally displays content based on upgrade state. Uses pipe `|` as delimiter.

```json
{
  "text": "{var:show:Upgrade text|Normal text}"
}
```

- When upgraded: shows the first segment (before `|`)
- When normal: shows the second segment (after `|`)
- When previewing upgrade: shows first segment in green

---

## Dynamic Variables

The game uses specialized variable types (`DynamicVar` subclasses) that carry additional metadata for formatting:

- **DamageVar** - Damage values with highlighting
- **BlockVar** - Block values
- **EnergyVar** - Energy values with color info
- **CalculatedVar** - Calculated values
- **CalculatedDamageVar** - Calculated damage
- **CalculatedBlockVar** - Calculated block
- **BoolVar** - Boolean values
- **IntVar** - Integer values
- **StringVar** - String values
- **GoldVar** - Gold amounts
- **HealVar** - Healing amounts
- **MaxHpVar** - Max HP values
- **PowerVar** - Power values
- **StarsVar** - Star counts
- **CardsVar** - Card references
- **IfUpgradedVar** - Upgrade state indicator

These DynamicVar types enable formatters to access additional context beyond simple values.

---

## Formatting Pipeline

1. `LocString.GetFormattedText()` is called
2. `LocManager.SmartFormat()` retrieves the raw text from the localization table
3. The appropriate `CultureInfo` is selected based on whether the key is localized
4. `SmartFormatter.Format()` processes the text with variables
5. Custom formatters are applied as specified in the format strings
6. If formatting fails, the raw text is returned and an error is logged

---

## How to Add Custom Formatters

To add a new custom formatter:

1. Create a class implementing `IFormatter` from `SmartFormat.Core.Extensions`
2. Set the `Name` property to the formatter's identifier
3. Implement `TryEvaluateFormat` to handle the formatting logic
4. Register the formatter in `LocManager.LoadLocFormatters`

Example:

```csharp
public class MyCustomFormatter : IFormatter
{
    public string Name
    {
        get => "myCustom";
        set => throw new NotImplementedException();
    }

    public bool CanAutoDetect { get; set; }

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var value = formattingInfo.CurrentValue;
        // Process value and write output
        formattingInfo.Write($"Processed: {value}");
        return true;
    }
}
```

Then register it in `LocManager.LoadLocFormatters`:

```csharp
_smartFormatter.AddExtensions(new MyCustomFormatter());
```

---

## Error Handling

When formatting fails:

1. The exception is caught (FormattingException or ParsingErrors)
2. An error message is logged with table, key, and variables
3. A Sentry event is captured with a fingerprint based on the error pattern
4. The raw text (unformatted) is returned as fallback

This ensures that localization errors don't crash the game.

---

## Advanced Syntax

The game supports complex nested formatting patterns for cards with multiple effects:

### Conditional Formatting

Use boolean variables to conditionally display text:

```json
{
  "text": "{HasRider:This card has a rider effect|This card has no rider}"
}
```

- If `HasRider` is true: shows "This card has a rider effect"
- If `HasRider` is false: shows "This card has no rider"

### Choice Formatting

Use `choose` to select text based on variable values:

```json
{
  "text": "{CardType:choose(Attack|Skill|Power):Attack text|Skill text|Power text}"
}
```

- If `CardType` is "Attack": shows "Attack text"
- If `CardType` is "Skill": shows "Skill text"
- If `CardType` is "Power": shows "Power text"

### Pipe Delimiter

The pipe `|` character separates options in conditional and choice formatters:

```json
{
  "text": "{condition:True text|False text}"
}
```

### Nested Formatters

Formatters can be nested for complex logic:

```json
{
  "text": "{Violence:Deal {Damage:diff()} damage {ViolenceHits:diff()} times|Deal {Damage:diff()} damage}"
}
```

This shows damage with hit count when `Violence` is true, otherwise just damage.

### BBCode Color Tags

Use BBCode-style tags for colored text:

```json
{
  "text": "Gain [gold]Block[/gold] equal to [green]{value}[/green]"
}
```

Common color tags:
- `[gold]...[/gold]` - Gold/yellow highlighting
- `[green]...[/green]` - Green highlighting (buffs)
- `[red]...[/red]` - Red highlighting (debuffs)

---

## Mad Science Card Example

The "Mad Science" card demonstrates complex placeholder resolution:

```json
{
  "MAD_SCIENCE.description": "{CardType:choose(Attack|Skill|Power):Deal {Damage:diff()} damage{Violence: {ViolenceHits:diff()} times|}.|Gain {Block:diff()} [gold]Block[/gold].|}{HasRider:{Sapping:\nApply {SappingWeak:diff()} [gold]Weak[/gold].\nApply {SappingVulnerable:diff()} [gold]Vulnerable[/gold].|}{Choking:\nWhenever you play a card this turn, the enemy loses {ChokingDamage:diff()} HP.|}{Energized:\nGain {EnergizedEnergy:energyIcons()}.|}{Wisdom:\nDraw {WisdomCards:diff()} cards.|}{Chaos:\nAdd a random card into your [gold]Hand[/gold]. It costs 0 {energyPrefix:energyIcons(1)} this turn.|}{Expertise:Gain {ExpertiseStrength:diff()} [gold]Strength[/gold].\nGain {ExpertiseDexterity:diff()} [gold]Dexterity[/gold].|}{Curious:Powers cost {CuriousReduction:diff()} {energyPrefix:energyIcons(1)} less.|}{Improvement:At the end of combat, [gold]Upgrade[/gold] a random card.|}|{CardType:choose(Attack|Skill|Power):\n???|\n???|???}}"
}
```

### Analysis

This card shows:
1. **Base effect** based on `CardType` (Attack/Skill/Power)
   - Attack: Deal damage, optionally multiple times with Violence effect
   - Skill: Gain Block
   - Power: Base effect is empty (determined by rider effects)
2. **Optional rider effects** based on `HasRider` boolean
3. **Multiple rider types**:
   - **Sapping**: Apply Weak and Vulnerable
   - **Choking**: Enemy loses HP whenever you play a card
   - **Energized**: Gain energy icons
   - **Wisdom**: Draw cards
   - **Chaos**: Add a random card with 0 cost
   - **Expertise**: Gain Strength and Dexterity
   - **Curious**: Reduce Power card costs
   - **Improvement**: Upgrade a random card at end of combat
4. **Nested diff formatters** for value highlighting
5. **Energy icons** with `energyIcons()` formatter
6. **BBCode colors** for keywords

### How Variables Are Added

In `MadScience.AddExtraArgsToDescription`:

```csharp
protected override void AddExtraArgsToDescription(LocString description)
{
    description.Add("CardType", TinkerTimeType.ToString());
    description.Add("HasRider", TinkerTimeRider != TinkerTime.RiderEffect.None);
    
    // Add boolean for each rider type
    TinkerTime.RiderEffect[] values = Enum.GetValues<TinkerTime.RiderEffect>();
    for (int i = 0; i < values.Length; i++)
    {
        TinkerTime.RiderEffect riderEffect = values[i];
        description.Add(riderEffect.ToString(), TinkerTimeRider == riderEffect);
    }
}
```

This adds:
- `CardType` - String variable for the card type
- `HasRider` - Boolean indicating if any rider is active
- `Sapping`, `Choking`, `Energized`, `Wisdom`, `Chaos`, `Expertise`, `Curious`, `Improvement` - Booleans for each rider type

---

## Related Documents

- [Localization & Keywords](LocalizationAndKeywords.md)
- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
- [Card Dynamic Variables](CardDynamicVarToolkit.md)