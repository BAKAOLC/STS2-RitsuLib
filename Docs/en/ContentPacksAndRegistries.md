# Content Packs & Registries

This document is the reference for how RitsuLib registration is organized.

It covers:

- the relationship between `CreateContentPack(...)` and the underlying registries
- what `Apply()` actually does
- when to use builder steps, manifests, direct registry access, or optional CLR attributes
- how fixed model identity and ModelDb integration relate to registration
- generated placeholders for cards/relics/potions (API, ordering, and risks)

---

## Registry Map

RitsuLib keeps registration responsibilities split by concern:

| Registry | Purpose |
|---|---|
| `ModContentRegistry` | Register models: characters, acts, pool-bound cards/relics/potions, powers, orbs, enchantments, afflictions, achievements, singletons, good/bad daily modifiers, shared card/relic/potion pools, events, ancients, monsters, and generated placeholders |
| `ModKeywordRegistry` | Register reusable keyword definitions |
| `ModTimelineRegistry` | Register stories and epochs |
| `ModUnlockRegistry` | Register epoch requirements and progression rules |

`CreateContentPack(modId)` is the convenience layer that coordinates all four.

---

## `CreateContentPack(...)`

The fluent builder is the recommended entry point:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Card<MyCardPool, MyCard>()
    .Relic<MyRelicPool, MyRelic>()
    .CardKeyword("brew", locKeyPrefix: "my_mod_brew")
    .Epoch<MyCharacterEpoch>()
    .Story<MyStory>()
    .RequireEpoch<MyLateCard, MyCharacterEpoch>()
    .Apply();
```

What the builder does not do:

- it does not auto-discover content by reflection
- it does not reorder your steps for you
- it does not replace the underlying registries

It simply records registration steps and runs them in insertion order when `Apply()` is called.

---

## `ModContentPackContext`

`Apply()` returns a `ModContentPackContext` containing:

- `Content`
- `Keywords`
- `Timeline`
- `Unlocks`

That means the fluent builder can be your main registration path, while still letting you access the raw registries afterward.

---

## Step Ordering

Builder steps execute in the order you add them.

That matters when:

- your custom step expects a registry entry to already exist
- you mix builder calls with `Custom(ctx => ...)`
- you want logs to reflect a specific setup flow

`CreateContentPack` is intentionally explicit here. It is a sequenced registration script, not a dependency solver.

---

## Builder Surface

The builder supports several kinds of steps:

- content model registration
- keyword registration
- timeline registration
- unlock registration
- manifest-driven registration
- arbitrary custom callbacks

Less obvious helpers that are still useful:

- `Entry(IContentRegistrationEntry)`
- `Entries(IEnumerable<IContentRegistrationEntry>)`
- `Keyword(KeywordRegistrationEntry)`
- `Keywords(IEnumerable<KeywordRegistrationEntry>)`
- `Manifest(contentEntries, keywordEntries)`
- `Custom(Action<ModContentPackContext>)`
- generated placeholders: `PlaceholderCard<TPool>(...)`, `PlaceholderRelic<TPool>(...)`, `PlaceholderPotion<TPool>(...)` (see “Generated placeholder content” below)
- extended standalone / pool types: `.Enchantment<T>()`, `.Affliction<T>()`, `.Achievement<T>()`, `.Singleton<T>()`, `.GoodModifier<T>()` / `.BadModifier<T>()`, `.SharedRelicPool<T>()`, `.SharedPotionPool<T>()` (see “Content model registration matrix” below)

These are useful when you want registration declared as data instead of written inline in one long chain.

---

## When To Use The Raw Registries

Use `CreateContentPack(...)` by default.

Use raw registries directly when:

- registration is split across several modules
- you want to expose registration helpers from your own library layer
- you need registry access without committing to a single fluent chain
- you are generating registration entries programmatically

Typical direct access looks like:

```csharp
var content = RitsuLibFramework.GetContentRegistry("MyMod");
content.RegisterCharacter<MyCharacter>();

var timeline = RitsuLibFramework.GetTimelineRegistry("MyMod");
timeline.RegisterEpoch<MyEpoch>();
```

The registries are first-class APIs, not implementation details.

---

## What The Content Registry Owns

`ModContentRegistry` is responsible for:

- recording which model types belong to which mod
- validating ownership and duplicate registration
- feeding ModelDb integration: global accessors such as `AllCharacters`, acts, powers, orbs, shared events, ancients, **shared card / relic / potion pool types**, `DebugEnchantments`, `DebugAfflictions`, `Achievements`, `GoodModifiers`, `BadModifiers`, and related enumerations are extended via patches where needed; **per-pool** cards/relics/potions are merged through `ModHelper.AddModelToPool` when each pool expands `AllCards` / `AllRelics` / `AllPotions` (a different code path than those global appenders)
- generating fixed public `ModelId.Entry` values for registered types

That owner tracking is what lets RitsuLib safely answer questions like:

- which mod registered this type?
- what should its fixed public entry be?
- should vanilla progression/compatibility logic treat this as modded content?

---

## Fixed Public Identity

For RitsuLib-registered models, public `ModelId.Entry` is forced into a stable format:

```text
<MODID>_<CATEGORY>_<TYPENAME>
```

This is applied through the ModelDb identity patch, not by changing your CLR type names at source.

Why it matters:

- localization keys become deterministic
- default asset conventions become predictable
- model ownership remains clear across patches and saves

The identity rule applies only to types explicitly registered through RitsuLib.

---

## ModelDb Integration

Registration alone is not enough; the game still needs to see the content.

RitsuLib patches ModelDb and related model access points to:

- append registered characters, acts, powers, orbs, events, ancients, shared card pools, **shared relic pools** (`AllRelicPools`), **shared potion pools** (`AllPotionPools`), **debug enchantments** (`DebugEnchantments`), **debug afflictions** (`DebugAfflictions`), **achievements** (`Achievements`), and **daily modifiers** (`GoodModifiers` / `BadModifiers`) where applicable
- attach registered cards/relics/potions to their **target pools** via `ModHelper.AddModelToPool` (concatenated when each pool materializes its `All*` sequence)
- force fixed public entries for registered model types
- inject types that live in **dynamic assemblies** (e.g. Reflection.Emit placeholders) into `ModelDb` before init completes, for every registered model category the registry tracks
- bootstrap dynamic act-content patching before caches lock in

`MutuallyExclusiveModifiers` is **not** extended automatically; mod modifiers registered as good/bad appear only in those two lists.

This is why registration must happen before the framework freeze points.

---

## Freeze Behavior

The relevant registries freeze after early initialization:

- content registration freeze
- timeline registration freeze
- unlock registration freeze

Once frozen, later registration attempts throw.

This is intentional because the framework wants:

- stable identity
- stable model lists
- deterministic unlock/filter behavior

If a mod registers content late, the safest outcome is to fail early rather than let the game build partial caches.

---

## Manifests And Entry Objects

If you want registration to be declared as data, you can package it into entry objects:

```csharp
var contentEntries = new IContentRegistrationEntry[]
{
    new CharacterRegistrationEntry<MyCharacter>(),
    new CardRegistrationEntry<MyCardPool, MyCard>(),
};

var keywordEntries = new[]
{
    KeywordRegistrationEntry.Card("brew", "my_mod_brew"),
};

RitsuLibFramework.CreateContentPack("MyMod")
    .Manifest(contentEntries, keywordEntries)
    .Apply();
```

This is useful when you want a declarative registration list or want to share registration bundles across modules.

You can mix entry types freely—for example:

```csharp
var contentEntries = new IContentRegistrationEntry[]
{
    new CharacterRegistrationEntry<MyCharacter>(),
    new CardRegistrationEntry<MyCardPool, MyCard>(),
    new EnchantmentRegistrationEntry<MyEnchantment>(),
    new PowerRegistrationEntry<MyPower>(),
    new SharedRelicPoolRegistrationEntry<MyModSharedRelicPool>(),
};
```

---

## Attribute-based registration (optional)

CLR attributes in `STS2RitsuLib.Interop.AutoRegistration` (for example `[RegisterSharedCardPool]`, `[RegisterCard(typeof(MyPool))]`) ultimately call the **same registry APIs** as the fluent builder, direct registries, and manifest entries.

RitsuLib runs them during the early **mod type discovery** pass (`ModTypeDiscoveryPatch`). The built-in `AttributeAutoRegistrationTypeDiscoveryContributor` scans **concrete** CLR types in assemblies you register with **`ModTypeDiscoveryHub.RegisterModAssembly(modId, Assembly.GetExecutingAssembly())`** from your mod initializer **before** `PatchAll`. A type must resolve to a mod id (usually via the manifest-mapped assembly); if not, annotate the type with **`[RitsuLibOwnedBy("modId")]`**.

This does **not** replace `CreateContentPack(...)`; it is an alternative authoring style. Mixing approaches is acceptable when ordering and freeze rules remain valid.

### `Inherit` on `AutoRegistrationAttribute`

Attributes apply to the type they annotate. **`Inherit`** defaults to **`false`**. When **`Inherit = true`** on an attribute declared on a **base class**, **concrete derived types** are handled as if the same attribute were declared on each subclass (the registry still receives the **subclass** `Type`). If a subclass already has a **direct** attribute that would produce the **same registration signature**, the inherited duplicate is skipped. Abstract base types are skipped by the scan; only concrete types are registered.

---

## Content model registration matrix

Every row below is **one conceptual kind of content**. You can register it in **three** primary equivalent ways below, plus the optional attribute path in the previous section (unless noted):

1. **Fluent** — `ModContentPackBuilder` method on `CreateContentPack(...)`  
2. **Registry** — `ModContentRegistry` method from `RitsuLibFramework.GetContentRegistry(modId)` or `ctx.Content` in `Custom(...)`  
3. **Manifest entry** — a type implementing `IContentRegistrationEntry` in `STS2RitsuLib.Scaffolding.Content` (use `.Entry(...)`, `.Entries(...)`, or `.Manifest(...)`)

| Content | Fluent | Registry | Manifest entry |
|---|---|---|---|
| Character | `.Character<T>()` | `RegisterCharacter<T>()` | `CharacterRegistrationEntry<T>` |
| Act | `.Act<T>()` | `RegisterAct<T>()` | `ActRegistrationEntry<T>` |
| Card in pool | `.Card<TPool,TCard>(...)` | `RegisterCard<TPool,TCard>(...)` | `CardRegistrationEntry<TPool,TCard>` |
| Relic in pool | `.Relic<TPool,TRelic>(...)` | `RegisterRelic<TPool,TRelic>(...)` | `RelicRegistrationEntry<TPool,TRelic>` |
| Potion in pool | `.Potion<TPool,TPotion>(...)` | `RegisterPotion<TPool,TPotion>(...)` | `PotionRegistrationEntry<TPool,TPotion>` |
| Power | `.Power<T>()` | `RegisterPower<T>()` | `PowerRegistrationEntry<T>` |
| Orb | `.Orb<T>()` | `RegisterOrb<T>()` | `OrbRegistrationEntry<T>` |
| Enchantment | `.Enchantment<T>()` | `RegisterEnchantment<T>()` | `EnchantmentRegistrationEntry<T>` |
| Affliction | `.Affliction<T>()` | `RegisterAffliction<T>()` | `AfflictionRegistrationEntry<T>` |
| Achievement | `.Achievement<T>()` | `RegisterAchievement<T>()` | `AchievementRegistrationEntry<T>` |
| Singleton | `.Singleton<T>()` | `RegisterSingleton<T>()` | `SingletonRegistrationEntry<T>` |
| Daily modifier (good) | `.GoodModifier<T>()` | `RegisterGoodModifier<T>()` | `GoodModifierRegistrationEntry<T>` |
| Daily modifier (bad) | `.BadModifier<T>()` | `RegisterBadModifier<T>()` | `BadModifierRegistrationEntry<T>` |
| Shared card pool | `.SharedCardPool<T>()` | `RegisterSharedCardPool<T>()` | `SharedCardPoolRegistrationEntry<T>` |
| Shared relic pool | `.SharedRelicPool<T>()` | `RegisterSharedRelicPool<T>()` | `SharedRelicPoolRegistrationEntry<T>` |
| Shared potion pool | `.SharedPotionPool<T>()` | `RegisterSharedPotionPool<T>()` | `SharedPotionPoolRegistrationEntry<T>` |
| Shared event | `.SharedEvent<T>()` | `RegisterSharedEvent<T>()` | `SharedEventRegistrationEntry<T>` |
| Act encounter | `.ActEncounter<TAct,TEncounter>()` | `RegisterActEncounter<TAct,TEncounter>()` | `ActEncounterRegistrationEntry<TAct,TEncounter>` |
| Act event | `.ActEvent<TAct,TEvent>()` | `RegisterActEvent<TAct,TEvent>()` | `ActEventRegistrationEntry<TAct,TEvent>` |
| Shared ancient | `.SharedAncient<T>()` | `RegisterSharedAncient<T>()` | `SharedAncientRegistrationEntry<T>` |
| Act ancient | `.ActAncient<TAct,TAnc>()` | `RegisterActAncient<TAct,TAnc>()` | `ActAncientRegistrationEntry<TAct,TAncient>` |
| Monster | *(no fluent helper)* | `RegisterMonster<T>()` | `MonsterRegistrationEntry<T>` |
| Placeholder card / relic / potion | `.PlaceholderCard<...>(...)` etc. | `RegisterPlaceholderCard<...>(...)` etc. | `PlaceholderCardRegistrationEntry<...>` etc. |
| Archaic Tooth mapping | `.ArchaicToothTranscendence<...>()` or `.ArchaicToothTranscendence(id, type)` | `RitsuLibFramework.RegisterArchaicToothTranscendenceMapping(...)` | `ArchaicToothTranscendenceRegistrationEntry<...>` / `ArchaicToothTranscendenceByIdRegistrationEntry` |
| Touch of Orobas mapping | `.TouchOfOrobasRefinement<...>()` or `.TouchOfOrobasRefinement(id, type)` | `RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping(...)` | `TouchOfOrobasRefinementRegistrationEntry<...>` / `TouchOfOrobasRefinementByIdRegistrationEntry` |

**Enchantments:** optional authoring baseline `ModEnchantmentTemplate` plus `IModEnchantmentAssetOverrides` / `EnchantmentIntendedIconPathPatch` (see scaffolding content patches) for custom icon paths; registration in this table is still required for ownership, fixed `ModelId.Entry`, and dynamic-assembly injection like other model kinds.

**Singletons:** there is no global `ModelDb` list to patch; registration still records ownership and injects dynamic types so `ModelDb.Singleton<T>()` resolves correctly.

---

## Generated placeholder content

Use this when you want pool entries and a **stable public `ModelId.Entry`** (via `ModelPublicEntryOptions.FromStem` / `FromFullPublicEntry`) **without authoring one CLR type per card/relic/potion**—for example so reward tables, unlocks, or saves can reference IDs while content is still WIP. RitsuLib generates sealed subclasses at runtime with **Reflection.Emit**; gameplay is intentionally **no-op** (empty `OnPlay` / `OnUse`, etc.).

### API summary

| Use case | Entry point |
|---|---|
| Fluent pack | `PlaceholderCard<TPool>(stableEntryStem, PlaceholderCardDescriptor)`, `PlaceholderRelic<TPool>(...)`, `PlaceholderPotion<TPool>(...)` |
| Registry | `ModContentRegistry.RegisterPlaceholderCard<TPool>(...)` (overloads accept `ModelPublicEntryOptions`, e.g. `FromFullPublicEntry`) |
| Shape | `PlaceholderCardDescriptor`, `PlaceholderRelicDescriptor`, `PlaceholderPotionDescriptor` (structs with defaults) |
| You already have a type | Two-type overload `PlaceholderCard<TPool, TCard>(stem)` only pins the entry for an existing class |

`ModPlaceholderCardTemplate` / `ModPlaceholderRelicTemplate` / `ModPlaceholderPotionTemplate` are bases for emitted types; **mods normally should not subclass them** unless you have an advanced reason.

### Example

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;

RitsuLibFramework.CreateContentPack("MyMod")
    .Manifest(contentEntries, keywordEntries)
    .Custom(ctx =>
    {
        ctx.Content.RegisterPlaceholderCard<MyCardPool>("wip_reward_attack",
            new PlaceholderCardDescriptor(
                BaseCost: 1,
                Type: CardType.Attack,
                Rarity: CardRarity.Common,
                Target: TargetType.AnyEnemy));
    })
    .Apply();
```

For relics, `PlaceholderRelicDescriptor.MerchantCostOverride`: **`< 0` (default `-1`)** keeps rarity-based shop pricing; **`≥ 0`** overrides `MerchantCost`.

### Ordering

If you combine `Manifest(...)` with placeholders, register placeholders **after** prerequisites exist (typical pattern: `.Manifest(...)` then `.Custom(ctx => ...)` calling `RegisterPlaceholder*`), so pools and other types are already registered.

---

### Warnings (read carefully)

> **Saves and entry stability**  
> Once a placeholder id appears in saves or unlock data, its `ModelId.Entry` (from the stem or `FromFullPublicEntry`) is a long-lived contract. **Renaming stems or full-entry strings** can break old saves or unlock references. When shipping real content, keep the same entry or plan a migration.

> **No gameplay effects**  
> Placeholders do not implement damage, draw, relic triggers, etc. They prevent missing-model failures in some paths; **balance and UX can still be wrong** until you replace them with real types.

> **Localization and assets**  
> Placeholders still follow default loc-key and asset conventions from the entry. Missing translations or art may show raw keys or blanks—that is expected and does not mean registration failed.

> **Multiplayer and `ModelIdSerializationCache.Hash`**  
> Emitted types are **not** returned by the game’s vanilla `AllAbstractModelSubtypes` scan. RitsuLib injects dynamic-assembly models before `ModelDb.Init` and, after `ModelIdSerializationCache.Init`, **merges every model present in `ModelDb` into the net-ID tables and recomputes the hash** (same algorithm shape as vanilla).  
> **Consequence**: different loaded mod sets → different hashes → clients **may not match** for multiplayer or replays. This is inherent to dynamic placeholders, not only a single-player concern.

> **RitsuLib version coupling**  
> Placeholder generation, `InjectDynamicRegisteredModels`, and serialization-cache integration follow the framework version you ship. Pin a compatible `STS2-RitsuLib` dependency and retest after upgrading the library.

---

## Recommended Registration Pattern

For most mods:

1. create one content pack in the mod initializer
2. register all content, keywords, timeline nodes, and unlock rules there
3. keep `Custom(...)` steps small and explicit
4. avoid late registration from gameplay hooks
5. with `TypeListCardPoolModel`, register pool cards via `.Card<Pool, Card>()` or `CardRegistrationEntry`; **do not** override the obsolete `CardTypes` hook (the base already defaults to empty—see [Getting Started](GettingStarted.md))

If the mod grows large, keep the builder at the top level and feed it entry objects or helper methods from submodules.

---

## Related Documents

- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
- [Timeline & Unlocks](TimelineAndUnlocks.md)
- [Framework Design](FrameworkDesign.md)
