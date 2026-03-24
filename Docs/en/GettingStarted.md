# Getting Started

This guide walks through the full setup — from declaring the dependency to registering your first content.

---

## 1. Declare the Dependency

Add `STS2-RitsuLib` to your `mod_manifest.json`:

```json
{
  "id": "MyMod",
  "name": "My Mod",
  "dependencies": ["STS2-RitsuLib"]
}
```

---

## 2. Initialize Your Mod

Use `[ModInitializer]` to declare the entry point. Obtain a logger, create a patcher, and register content:

```csharp
using STS2RitsuLib;
using STS2RitsuLib.Patching.Core;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer(nameof(Initialize))]
public static class MyMod
{
    public static Logger Logger { get; private set; } = null!;

    public static void Initialize()
    {
        Logger = RitsuLibFramework.CreateLogger("MyMod");

        var patcher = RitsuLibFramework.CreatePatcher("MyMod", "core-patches");
        patcher.RegisterPatches<MyModPatches>();
        patcher.PatchAll();

        RitsuLibFramework.CreateContentPack("MyMod")
            .Card<MyCardPool, MyCard>()
            .Relic<MyRelicPool, MyRelic>()
            .Character<MyCharacter>()
            .Apply();
    }
}
```

`CreatePatcher` takes a `patcherName` used for log identification. A mod may create multiple patchers. See [PatchingGuide.md](PatchingGuide.md) for the full patch workflow.

---

## 3. Define a Card Pool

Use `TypeListCardPoolModel` and declare all card types in the pool:

```csharp
public class MyCardPool : TypeListCardPoolModel
{
    protected override IEnumerable<Type> CardTypes =>
    [
        typeof(MyCard),
        typeof(MyOtherCard),
    ];
}
```

---

## 4. Define a Card

Inherit from `ModCardTemplate` and pass base properties in the primary constructor:

```csharp
public class MyCard : ModCardTemplate(
    baseCost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.SingleEnemy)
{
    public override string Title => "Strike";
    public override string Description => $"Deal {Damage} damage.";

    // Optional custom portrait
    public override string? CustomPortraitPath => "res://MyMod/art/strike.png";

    public override void Use(ICombatContext ctx, ICreatureState user, ICreatureState? target)
    {
        ctx.DealDamage(user, target, Damage);
    }
}
```

---

## 5. Localization Keys

The `ModelId.Entry` for any RitsuLib-registered model is derived as:

```
<MODID>_<CATEGORY>_<TYPENAME>
```

All segments are normalized to UPPER_SNAKE_CASE.

| Mod Id | C# Type | Category | Entry |
|---|---|---|---|
| `MyMod` | `MyCard` | card | `MY_MOD_CARD_MY_CARD` |
| `MyMod` | `MyRelic` | relic | `MY_MOD_RELIC_MY_RELIC` |
| `STS2-WineFox` | `WineFoxStrike` | card | `STS2_WINE_FOX_CARD_WINE_FOX_STRIKE` |

Localization file example:

```json
{
  "MY_MOD_CARD_MY_CARD.title": "Strike",
  "MY_MOD_CARD_MY_CARD.description": "Deal {damage} damage."
}
```

---

## 6. Subscribe to Lifecycle Events

```csharp
// Runs once after game is ready
RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
{
    Logger.Info("Game ready.");
});

// On every combat start
RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt =>
{
    // evt.RunState, evt.CombatState
});
```

Replayable events (`IReplayableFrameworkLifecycleEvent`) fire immediately upon late subscription if the event has already occurred.

---

## 7. Persistent Data

Use `BeginModDataRegistration` for batch key registration. Persistent entries are class-based and need both a registry key and a file name:

```csharp
public sealed class CounterData
{
    public int Value { get; set; }
}

using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    var store = RitsuLibFramework.GetDataStore("MyMod");
    store.Register<CounterData>(
        key: "my_counter",
        fileName: "counter.json",
        scope: SaveScope.Profile,
        defaultFactory: () => new CounterData());
}
```

See [PersistenceGuide.md](PersistenceGuide.md) for scopes, reload timing, and migrations.

---

## Next Steps

- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
- [Character & Unlock Scaffolding](CharacterAndUnlockScaffolding.md)
- [Card Dynamic Variables](CardDynamicVarToolkit.md)
- [Lifecycle Events](LifecycleEvents.md)
- [Patching Guide](PatchingGuide.md)
- [Persistence Guide](PersistenceGuide.md)
- [Localization & Keywords](LocalizationAndKeywords.md)
- [Framework Design](FrameworkDesign.md)
