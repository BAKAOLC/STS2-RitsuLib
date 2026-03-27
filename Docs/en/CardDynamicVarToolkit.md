# Card Dynamic Var Toolkit

This document describes how RitsuLib creates card dynamic variables, how tooltip binding works, and how values are injected when a card is hovered.

---

## Vanilla DynamicVar System

> The following describes the game engine’s own dynamic variable system. RitsuLib builds convenience constructors on top of it.

The game’s `DynamicVar` system lets cards carry values that can change at runtime. Each `DynamicVar` subclass may carry extra metadata for formatters (for example `DamageVar` for highlighting, `EnergyVar` for colors). For the full list of subclasses, see [LocString Placeholder Resolution](LocStringPlaceholderResolution.md).

---

## RitsuLib Capabilities

On top of the vanilla system, RitsuLib provides:

- **`ModCardVars`** — convenient variable constructors
- **`DynamicVarExtensions`** — each variable can bind its own tooltip independently
- **Automatic injection** — on card hover, all bound tooltips are appended automatically (implemented via patches; no extra setup)

---

## Variable Construction

Create variables with `ModCardVars` and add them to the card’s `DynamicVarSet`:

```csharp
public class MyCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    private static readonly DynamicVar _charges =
        ModCardVars.Int("charges", amount: 3)
            .WithSharedTooltip("my_mod_charges");

    private static readonly DynamicVar _label =
        ModCardVars.String("flavor", value: "wine");

    public override DynamicVarSet CreateDynamicVars() =>
        new DynamicVarSet().Add(_charges).Add(_label);
}
```

| Method | Description |
|---|---|
| `ModCardVars.Int(name, amount)` | Creates a numeric variable (`decimal`) |
| `ModCardVars.String(name, value)` | Creates a string variable |
| `ModCardVars.Computed(...)` | Creates a computed variable |

RitsuLib does not assign gameplay semantics to these variables. Their meaning is entirely defined by the content author.

---

## Tooltip Binding

Bind tooltips at definition time via chained extension methods:

### Shared tooltip (recommended)

Reads keys from the `static_hover_tips` table:

```csharp
var myVar = ModCardVars.Int("my_var", 2)
    .WithSharedTooltip("my_mod_my_var");
// Resolves:
//   static_hover_tips["my_mod_my_var.title"]
//   static_hover_tips["my_mod_my_var.description"]
```

### Explicit table / key

```csharp
var myVar = ModCardVars.Int("my_var", 2)
    .WithTooltip(
        titleTable: "card_keywords",
        titleKey:   "my_mod_my_var.title",
        iconPath:   "res://MyMod/art/kw.png");
```

### Custom factory

```csharp
var myVar = ModCardVars.Int("my_var", 2)
    .WithTooltip(var => new HoverTip(
        new LocString("my_table", "my_var.title"),
        new LocString("my_table", "my_var.description")));
```

---

## Localization Example

When using `WithSharedTooltip("my_mod_charges")`, provide entries in your `static_hover_tips` localization file:

```json
{
  "my_mod_charges.title": "Charges",
  "my_mod_charges.description": "Accumulated charges that deal extra damage."
}
```

RitsuLib does not ship built-in localization entries for these; if you use `WithSharedTooltip`, you must supply the strings yourself.

---

## Card Hover Injection

RitsuLib’s patches automatically append every dynamic variable in `CardModel.DynamicVars` that has a bound tooltip to the end of the hover-tip sequence. No extra configuration is required.

---

## Clone Behavior

When `DynamicVar.Clone()` runs, tooltip metadata bound on the source variable is copied to the clone. Upgraded or duplicated cards in combat therefore behave correctly without extra handling.

---

## Reading Variable Values at Runtime

Read values through `DynamicVarExtensions`:

```csharp
int charges = card.DynamicVars.GetIntOrDefault("charges");
decimal val = card.DynamicVars.GetValueOrDefault("charges");
bool active = card.DynamicVars.HasPositiveValue("charges");
```

---

## Related Documents

- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
- [Getting Started](GettingStarted.md)
- [LocString Placeholder Resolution](LocStringPlaceholderResolution.md)
