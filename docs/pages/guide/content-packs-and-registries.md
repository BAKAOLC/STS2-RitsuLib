---
title:
  en: Content Packs And Registries
  zh-CN: 内容包与注册器
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Builder Surface{lang="en"}

::: en

`CreateContentPack(modId)` returns a chainable builder. Call `Apply()` once after adding every step.

| Category | Builder methods |
| --- | --- |
| Core models | `.Card<TPool,TCard>()`, `.Relic<TPool,TRelic>()`, `.Potion<TPool,TPotion>()`, `.Power<TPower>()`, `.Orb<TOrb>()` |
| Characters | `.Character<TCharacter>()`, `.Character<TCharacter>(entry => ...)`, starter card / relic / potion helpers |
| World content | `.Act<TAct>()`, `.Monster<TMonster>()`, `.ActEncounter<TAct,TEncounter>()`, `.GlobalEncounter<TEncounter>()`, `.SharedEvent<TEvent>()`, `.ActEvent<TAct,TEvent>()`, `.SharedAncient<TAncient>()`, `.ActAncient<TAct,TAncient>()` |
| Metadata | `.Achievement<T>()`, `.Enchantment<T>()`, `.Affliction<T>()`, `.Badge<T>()`, `.Singleton<T>()`, modifiers |
| Text and ids | `.CardKeywordOwnedByLocNamespace(...)`, `.KeywordOwned(...)`, `.CardTagOwned(...)` |
| UI and piles | `.CardPileOwned(...)`, `.TopBarButtonOwned(...)` |
| Timeline | `.Story<T>()`, `.Epoch<T>()`, `.StoryEpoch<TStory,TEpoch>()`, timeline slot helpers |
| Unlocks | `.RequireEpoch<TModel,TEpoch>()`, `.UnlockEpochAfterWinAs<TCharacter,TEpoch>()`, and related count / boss / elite helpers |
| Batch input | `.ContentManifest(...)`, `.KeywordManifest(...)`, `.PackManifest(...)`, `.Manifest(...)` |
| Escape hatch | `.Custom(ctx => ...)` |

The builder applies steps in the order you added them. Keep model registration before unlock rules that reference those models.

:::

## Attribute Surface{lang="en"}

::: en

Use attributes when the registration belongs next to the model type. Register the assembly first with
`ModTypeDiscoveryHub.RegisterModAssembly(modId, assembly)`.

| Category | Attributes |
| --- | --- |
| Pool content | `RegisterCard`, `RegisterRelic`, `RegisterPotion` |
| Standalone models | `RegisterCharacter`, `RegisterPower`, `RegisterOrb`, `RegisterAchievement`, `RegisterSingleton` |
| World content | `RegisterAct`, `RegisterMonster`, `RegisterGlobalEncounter`, `RegisterActEncounter` |
| Events | `RegisterSharedEvent`, `RegisterActEvent`, `RegisterSharedAncient`, `RegisterActAncient` |
| Pools | `RegisterSharedCardPool`, `RegisterSharedRelicPool`, `RegisterSharedPotionPool` |
| Keywords and ids | `RegisterOwnedKeyword`, `RegisterOwnedCardKeyword`, `RegisterOwnedCardTag` |
| Timeline | `RegisterStory`, `RegisterEpoch`, `RegisterStoryEpoch`, `AutoTimelineSlot*` |
| Unlocks | `RequireEpoch`, `RequireAllCardsInPool`, `RegisterEpochCards`, `RegisterEpochRelicsFromPool`, `UnlockEpochAfter*` |
| UI | `RegisterOwnedCardPile`, `RegisterOwnedTopBarButton` |
| Special mappings | `RegisterArchaicToothTranscendence`, `RegisterTouchOfOrobasRefinement` |

Attributes also support `Order`, and pool-backed model attributes support `StableEntryStem` / `FullPublicEntry`.
`RegisterCharacterStarterCard`, `RegisterCharacterStarterRelic`, and `RegisterCharacterStarterPotion` use `Order` for the
final starter list order as well as operation ordering.

When an attribute is placed on an abstract base class, it is inherited only if `Inherit = true` is set. Use that for broad
registrations such as “all cards derived from this base go into this pool”; keep per-card stable entry overrides on the
concrete classes.

:::

## 注解能力{lang="zh-CN"}

::: zh-CN

当注册关系属于模型类型本身时，使用注解。先通过 `ModTypeDiscoveryHub.RegisterModAssembly(modId, assembly)` 注册程序集。

| 分类 | 注解 |
| --- | --- |
| 池内容 | `RegisterCard`、`RegisterRelic`、`RegisterPotion` |
| 独立模型 | `RegisterCharacter`、`RegisterPower`、`RegisterOrb`、`RegisterAchievement`、`RegisterSingleton` |
| 世界内容 | `RegisterAct`、`RegisterMonster`、`RegisterGlobalEncounter`、`RegisterActEncounter` |
| 事件 | `RegisterSharedEvent`、`RegisterActEvent`、`RegisterSharedAncient`、`RegisterActAncient` |
| 池 | `RegisterSharedCardPool`、`RegisterSharedRelicPool`、`RegisterSharedPotionPool` |
| 关键词与 ID | `RegisterOwnedKeyword`、`RegisterOwnedCardKeyword`、`RegisterOwnedCardTag` |
| 时间线 | `RegisterStory`、`RegisterEpoch`、`RegisterStoryEpoch`、`AutoTimelineSlot*` |
| 解锁 | `RequireEpoch`、`RequireAllCardsInPool`、`RegisterEpochCards`、`RegisterEpochRelicsFromPool`、`UnlockEpochAfter*` |
| UI | `RegisterOwnedCardPile`、`RegisterOwnedTopBarButton` |
| 特殊映射 | `RegisterArchaicToothTranscendence`、`RegisterTouchOfOrobasRefinement` |

注解也支持 `Order`；带池的模型注解支持 `StableEntryStem` / `FullPublicEntry`。
`RegisterCharacterStarterCard`、`RegisterCharacterStarterRelic`、`RegisterCharacterStarterPotion` 的 `Order` 既用于操作排序，也用于最终 starter 列表排序。

注解放在抽象基类上时，只有设置 `Inherit = true` 才会传给派生类。它适合“所有继承这个基类的卡都进同一个池”这类宽泛注册；每张卡自己的稳定 Entry 覆写应保留在具体类上。

:::

## Builder 能力{lang="zh-CN"}

::: zh-CN

`CreateContentPack(modId)` 返回链式 builder。把所有步骤加完后调用一次 `Apply()`。

| 分类 | Builder 方法 |
| --- | --- |
| 核心模型 | `.Card<TPool,TCard>()`、`.Relic<TPool,TRelic>()`、`.Potion<TPool,TPotion>()`、`.Power<TPower>()`、`.Orb<TOrb>()` |
| 角色 | `.Character<TCharacter>()`、`.Character<TCharacter>(entry => ...)`、初始卡牌 / 遗物 / 药水辅助方法 |
| 世界内容 | `.Act<TAct>()`、`.Monster<TMonster>()`、`.ActEncounter<TAct,TEncounter>()`、`.GlobalEncounter<TEncounter>()`、`.SharedEvent<TEvent>()`、`.ActEvent<TAct,TEvent>()`、`.SharedAncient<TAncient>()`、`.ActAncient<TAct,TAncient>()` |
| 元数据 | `.Achievement<T>()`、`.Enchantment<T>()`、`.Affliction<T>()`、`.Badge<T>()`、`.Singleton<T>()`、modifier 相关方法 |
| 文本和 ID | `.CardKeywordOwnedByLocNamespace(...)`、`.KeywordOwned(...)`、`.CardTagOwned(...)` |
| UI 与卡堆 | `.CardPileOwned(...)`、`.TopBarButtonOwned(...)` |
| 时间线 | `.Story<T>()`、`.Epoch<T>()`、`.StoryEpoch<TStory,TEpoch>()`、timeline slot 辅助方法 |
| 解锁 | `.RequireEpoch<TModel,TEpoch>()`、`.UnlockEpochAfterWinAs<TCharacter,TEpoch>()` 以及 run / boss / elite 计数辅助方法 |
| 批量输入 | `.ContentManifest(...)`、`.KeywordManifest(...)`、`.PackManifest(...)`、`.Manifest(...)` |
| 逃生口 | `.Custom(ctx => ...)` |

Builder 按添加顺序应用步骤。会引用模型的解锁规则应放在模型注册之后。

:::

## Direct Registries{lang="en"}

::: en

Every builder method maps to a registry call. Direct registries are useful for shared helper libraries and conditional setup.

```csharp
var content = RitsuLibFramework.GetContentRegistry("MyMod");
content.RegisterCard<MyCardPool, MyStrike>();

var keywords = RitsuLibFramework.GetKeywordRegistry("MyMod");
keywords.RegisterCardKeywordOwnedByLocNamespace("bleeding");
```

| Registry | Access |
| --- | --- |
| Content | `RitsuLibFramework.GetContentRegistry(modId)` |
| Keywords | `RitsuLibFramework.GetKeywordRegistry(modId)` |
| Timeline | `RitsuLibFramework.GetTimelineRegistry(modId)` |
| Unlocks | `RitsuLibFramework.GetUnlockRegistry(modId)` |
| Card tags | `RitsuLibFramework.GetCardTagRegistry(modId)` |
| Card piles | `RitsuLibFramework.GetCardPileRegistry(modId)` |
| Top-bar buttons | `RitsuLibFramework.GetTopBarButtonRegistry(modId)` |
| SmartFormat | `RitsuLibFramework.GetSmartFormatRegistry(modId)` |

:::

## 直接使用注册器{lang="zh-CN"}

::: zh-CN

每个 builder 方法都对应一个注册器调用。共享辅助库或条件注册逻辑可以直接使用注册器。

```csharp
var content = RitsuLibFramework.GetContentRegistry("MyMod");
content.RegisterCard<MyCardPool, MyStrike>();

var keywords = RitsuLibFramework.GetKeywordRegistry("MyMod");
keywords.RegisterCardKeywordOwnedByLocNamespace("bleeding");
```

| 注册器 | 获取方式 |
| --- | --- |
| 内容 | `RitsuLibFramework.GetContentRegistry(modId)` |
| 关键词 | `RitsuLibFramework.GetKeywordRegistry(modId)` |
| 时间线 | `RitsuLibFramework.GetTimelineRegistry(modId)` |
| 解锁 | `RitsuLibFramework.GetUnlockRegistry(modId)` |
| 卡牌标签 | `RitsuLibFramework.GetCardTagRegistry(modId)` |
| 卡堆 | `RitsuLibFramework.GetCardPileRegistry(modId)` |
| 顶栏按钮 | `RitsuLibFramework.GetTopBarButtonRegistry(modId)` |
| SmartFormat | `RitsuLibFramework.GetSmartFormatRegistry(modId)` |

:::

## Manifests{lang="en"}

::: en

Manifest arrays are useful when content is generated or split across files.

```csharp
IContentRegistrationEntry[] content =
[
    new CardRegistrationEntry<MyCardPool, MyStrike>(),
    new RelicRegistrationEntry<MyRelicPool, MyRelic>(),
];

RitsuLibFramework.CreateContentPack("MyMod")
    .ContentManifest(content)
    .KeywordManifest(keywordEntries)
    .PackManifest(timelineAndUnlockEntries)
    .Apply();
```

Use separate manifest methods when possible. It keeps ModelDb content, keyword ids, and timeline / unlock rules easy to review.

:::

## Manifest{lang="zh-CN"}

::: zh-CN

当内容由工具生成，或拆在多个文件里时，可以使用 manifest 数组。

```csharp
IContentRegistrationEntry[] content =
[
    new CardRegistrationEntry<MyCardPool, MyStrike>(),
    new RelicRegistrationEntry<MyRelicPool, MyRelic>(),
];

RitsuLibFramework.CreateContentPack("MyMod")
    .ContentManifest(content)
    .KeywordManifest(keywordEntries)
    .PackManifest(timelineAndUnlockEntries)
    .Apply();
```

尽量使用拆开的 manifest 方法。这样 ModelDb 内容、关键词 ID、时间线 / 解锁规则更容易审查。

:::

## Registration Timing{lang="en"}

::: en

Run content registration from your mod initializer. RitsuLib freezes content registration before the game finishes model initialization. After the freeze, adding models is invalid.

Subscribe to `ContentRegistrationClosedEvent` only for diagnostics. Do not wait for it to register content.

:::

## 注册时机{lang="zh-CN"}

::: zh-CN

内容注册应在 Mod 初始化入口执行。RitsuLib 会在游戏完成模型初始化前冻结内容注册。冻结后再添加模型属于无效操作。

`ContentRegistrationClosedEvent` 只适合诊断，不要等这个事件发生后再注册内容。

:::
