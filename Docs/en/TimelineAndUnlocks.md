# Timeline & Unlocks

This is the reference for timeline registration and unlock semantics.

RitsuLib splits timeline registration and unlock rules into two systems that are meant to work together. This document covers:

- How `Story` and `Epoch` are registered
- What the scaffold templates are responsible for
- How unlock rules are evaluated
- Limitations of vanilla progression for mod characters and RitsuLib’s compatibility bridges

---

## The Two Registries

| Registry | Role |
|---|---|
| `ModTimelineRegistry` | Registers `StoryModel` and `EpochModel` |
| `ModUnlockRegistry` | Defines unlock conditions for content or epochs |

In the fluent builder, these correspond to:

- `.Story<TStory>()`, `.Epoch<TEpoch>()`
- `.RequireEpoch<TModel, TEpoch>()`, `.UnlockEpochAfter...()`

Core distinction:

- **Timeline registration** answers “does this thing exist?”
- **Unlock registration** answers “when does it become available?”

---

## Story Registration

The recommended approach is `ModStoryTemplate`:

```csharp
public class MyStory : ModStoryTemplate
{
    protected override string StoryKey => "my-story";

    protected override IEnumerable<Type> EpochTypes =>
    [
        typeof(MyCharacterEpoch),
        typeof(MyCardEpoch),
    ];
}
```

`ModStoryTemplate` is responsible for:

- Deriving a normalized story identity from `StoryKey`
- Resolving `EpochTypes` into the `Epochs` array the game expects

---

## Epoch Registration

You can write plain `EpochModel` subclasses, or use RitsuLib scaffold templates:

| Template | Description |
|---|---|
| `CharacterUnlockEpochTemplate<TCharacter>` | Epoch that unlocks the character |
| `CardUnlockEpochTemplate` | Epoch that unlocks extra cards |
| `RelicUnlockEpochTemplate` | Epoch that unlocks extra relics |
| `PotionUnlockEpochTemplate` | Epoch that unlocks extra potions |

These templates mainly handle:

- Enqueue logic for the timeline unlock UI
- Follow-up epochs via `ExpansionEpochTypes`

### Character unlock epoch template

Built-in behavior of `CharacterUnlockEpochTemplate<TCharacter>`:

- Queues a character unlock in `NTimelineScreen`
- Writes the pending character unlock to save progress
- If `ExpansionEpochTypes` is set, queues further epochs into the timeline expansion

### Card / relic / potion epoch templates

`CardUnlockEpochTemplate`, `RelicUnlockEpochTemplate`, and `PotionUnlockEpochTemplate` work similarly:

- You declare the model types to unlock
- The template resolves types through `ModelDb`
- `UnlockText` is generated automatically
- `QueueUnlocks()` pushes into the timeline UI

---

## Expansion Epochs

All unlock epoch templates support:

```csharp
protected virtual IEnumerable<Type> ExpansionEpochTypes => [];
```

When the current epoch completes, these epochs are added automatically as timeline expansions, which helps chain unlocks:

1. Unlock the character first
2. Then reveal card unlocks
3. Then reveal relic unlocks

---

## Registration Timing and Freeze

Both the timeline and unlock registries freeze after early initialization because:

- Story and epoch identities must stay stable
- Unlock filtering and compatibility patches need a finalized rule set

Register `Story`, `Epoch`, and unlock rules from your initializer — not later at runtime.

---

## Requiring an Epoch for Content

When a model is registered but should only appear after an epoch is obtained, use `RequireEpoch<TModel, TEpoch>()`.

Typical uses:

- Late-game cards stay out of the pool until progress is met
- Relics open only after a specific story branch
- Shared ancients / events need a timeline milestone

RitsuLib applies the gate across multiple entry points:

- `UnlockState.Characters`
- Unlocked card / relic / potion pool queries
- Shared ancient lists
- Events generated for acts

This is not UI-only filtering; it changes what the game can actually offer.

---

## Post-Run Epoch Rules

Common convenience APIs on `ModUnlockRegistry`:

| Method | Description |
|---|---|
| `UnlockEpochAfterRunAs<TCharacter, TEpoch>()` | Unlock after completing a run with the given character |
| `UnlockEpochAfterWinAs<TCharacter, TEpoch>()` | Unlock after a win with that character |
| `UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(level)` | Unlock after a win at the given ascension |
| `UnlockEpochAfterRunCount<TEpoch>(requiredRuns, requireVictory)` | Unlock after enough runs |

These all compile to `PostRunEpochUnlockRule`.

You can also register a custom rule:

```csharp
unlocks.RegisterPostRunRule(
    PostRunEpochUnlockRule.Create(
        epochId: new MyEpoch().Id,
        description: "Unlock after any abandoned ascension-5 run",
        shouldUnlock: ctx => ctx.IsAbandoned && ctx.AscensionLevel >= 5));
```

---

## Counted Progression Rules

| Method | Description |
|---|---|
| `UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(count)` | Elite kill count |
| `UnlockEpochAfterBossVictories<TCharacter, TEpoch>(count)` | Boss kill count |
| `UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()` | Ascension 1 win |
| `RevealAscensionAfterEpoch<TCharacter, TEpoch>()` | Show ascension after the epoch |
| `UnlockCharacterAfterRunAs<TCharacter, TEpoch>()` | Unlock character after using that character |

---

## Compatibility Patches

> This section explains how vanilla progression limits mod characters and how RitsuLib bridges those gaps.

Several vanilla progression checks assume vanilla characters and do not naturally include mod characters. RitsuLib applies narrow bridge patches so registered unlock rules still apply at those checkpoints:

- Elite kill count → epoch checks
- Boss kill count → epoch checks
- Ascension 1 → epoch checks
- Post-run character-unlock epochs
- Ascension reveal unlock checks

These patches do not replace vanilla progression; they only add a bridge where vanilla would skip mod characters. That is why the unlock registry stores rules explicitly by `ModelId` instead of inferring all progression from the timeline graph alone.

---

## Recommended Pattern

For a story-driven character mod:

1. Register character, pools, epochs, and story in one content pack
2. Use `CharacterUnlockEpochTemplate<TCharacter>` for the character unlock epoch
3. Use card / relic / potion epoch templates for follow-up content
4. Use `RequireEpoch<TModel, TEpoch>()` for late-game gates
5. Prefer a small set of clear progression rules over many overlapping ones

---

## Builder Example

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Card<MyCardPool, MyLateCard>()
    .Relic<MyRelicPool, MyLateRelic>()
    .Epoch<MyCharacterEpoch>()
    .Epoch<MyLateContentEpoch>()
    .Story<MyStory>()
    .RequireEpoch<MyLateCard, MyLateContentEpoch>()
    .RequireEpoch<MyLateRelic, MyLateContentEpoch>()
    .UnlockEpochAfterWinAs<MyCharacter, MyCharacterEpoch>()
    .UnlockEpochAfterAscensionWin<MyCharacter, MyLateContentEpoch>(10)
    .Apply();
```

---

## Common Mistakes

- Registering epochs but forgetting the story that lists those epochs
- Registering story/epochs after the timeline has frozen
- Using `RequireEpoch` without any rule that can actually unlock that epoch
- Stacking many overlapping rules for the same epoch without a clear design
- Assuming vanilla counted progression works for mod characters without registering RitsuLib unlock rules

---

## Related Documents

- [Character & Unlock Scaffolding](CharacterAndUnlockScaffolding.md)
- [Content Packs & Registries](ContentPacksAndRegistries.md)
- [Diagnostics & Compatibility](DiagnosticsAndCompatibility.md)
- [Framework Design](FrameworkDesign.md)
