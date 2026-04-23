# 内容包与注册器

本文是 RitsuLib 注册体系的参考文档。

它主要解释：

- `CreateContentPack(...)` 与底层各个注册器的关系
- `Apply()` 到底做了什么
- 什么时候该用链式构建器、清单条目、直接调用注册器，或可选的 CLR 特性
- 固定模型身份与 ModelDb 集成是怎样建立在注册之上的
- 生成式占位（卡牌 / 遗物 / 药水）的 API、顺序与风险说明

---

## 注册器总览

RitsuLib 按职责拆分了几类注册器：

| 注册器 | 作用 |
|---|---|
| `ModContentRegistry` | 注册角色、Act、池内卡牌/遗物/药水、能力、球体、附魔（Enchantment）、苦难（Affliction）、成就、单例、好/坏每日修正、共享卡/遗物/药水池、事件、Ancient、怪物及生成式占位等模型 |
| `ModKeywordRegistry` | 注册可复用关键词定义 |
| `ModTimelineRegistry` | 注册 `Story` 与 `Epoch` |
| `ModUnlockRegistry` | 注册纪元门槛与进度解锁规则 |

`CreateContentPack(modId)` 就是把这四类能力打包成一个更顺手的入口。

---

## `CreateContentPack(...)`

推荐默认使用链式构建器：

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Card<MyCardPool, MyCard>()
    .Relic<MyRelicPool, MyRelic>()
    .CardKeywordOwnedByLocNamespace("brew")
    .Epoch<MyCharacterEpoch>()
    .Story<MyStory>()
    .RequireEpoch<MyLateCard, MyCharacterEpoch>()
    .Apply();
```

但需要明确的是，它不会：

- 自动反射扫描内容
- 自动替你重排注册顺序
- 取代底层注册器的存在

它只是把一系列注册步骤按加入顺序记录下来，并在 `Apply()` 时顺序执行。

---

## `ModContentPackContext`

`Apply()` 返回 `ModContentPackContext`，里面包含：

- `Content`
- `Keywords`
- `Timeline`
- `Unlocks`

也就是说，构建器可以作为主要入口，同时你在需要时仍然可以拿到原始注册器继续操作。

---

## 步骤顺序

构建器中的步骤严格按添加顺序执行。

这点在以下场景会很重要：

- 某个 `Custom(ctx => ...)` 依赖前面已经注册的内容
- 你希望日志顺序能准确反映初始化流程
- 你在同一个 chain 中混合内容注册与自定义逻辑

`CreateContentPack` 故意保持显式，它是“顺序执行的注册脚本”，而不是“自动推断依赖关系的求解器”。

---

## 构建器能做什么

构建器支持的步骤大致包括：

- 内容模型注册
- 关键词注册
- 时间线注册
- 解锁注册
- 清单式注册
- 任意自定义回调

一些不那么显眼，但很实用的入口包括：

- `Entry(IContentRegistrationEntry)`
- `Entries(IEnumerable<IContentRegistrationEntry>)`
- `Keyword(KeywordRegistrationEntry)`
- `Keywords(IEnumerable<KeywordRegistrationEntry>)`
- `Manifest(contentEntries, keywordEntries)`
- `Custom(Action<ModContentPackContext>)`
- 生成式占位：`PlaceholderCard<TPool>(...)`、`PlaceholderRelic<TPool>(...)`、`PlaceholderPotion<TPool>(...)`（详见下文「生成式占位内容」）
- 扩展的单体/池类型：`.Enchantment<T>()`、`.Affliction<T>()`、`.Achievement<T>()`、`.Singleton<T>()`、`.GoodModifier<T>()` / `.BadModifier<T>()`、`.SharedRelicPool<T>()`、`.SharedPotionPool<T>()`（详见下文「内容模型注册速查表」）

如果你希望“注册声明本身也是数据”，这些入口会很好用。

---

## 什么时候直接使用注册器

默认优先使用 `CreateContentPack(...)`。

但以下情况直接使用注册器更合适：

- 注册逻辑拆分在多个模块里
- 你希望在自己的前置库里再包装一层 API
- 你不想把所有注册都塞进一条长链
- 你要程序化生成注册项

典型写法如下：

```csharp
var content = RitsuLibFramework.GetContentRegistry("MyMod");
content.RegisterCharacter<MyCharacter>();

var timeline = RitsuLibFramework.GetTimelineRegistry("MyMod");
timeline.RegisterEpoch<MyEpoch>();
```

这些注册器是一等公民 API，不是构建器背后的私有实现细节。

---

## 内容注册器的职责

`ModContentRegistry` 主要负责：

- 记录某个模型类型归属于哪个 Mod
- 校验重复注册与冲突
- 为 ModelDb 补丁与其它集成点提供数据：例如向 `AllCharacters`、Act、能力、球体、共享事件、Ancient、**共享卡池 / 遗物池 / 药水池类型**、`DebugEnchantments`、`DebugAfflictions`、`Achievements`、`GoodModifiers`、`BadModifiers` 等访问器在需要时追加已注册模型；卡牌/遗物/药水进入**具体池**则通过 `ModHelper.AddModelToPool` 在池展开 `AllCards` / `AllRelics` / `AllPotions` 时合并（与上述全局追加不是同一条实现路径）
- 为已注册类型生成固定公开 `ModelId.Entry`

这套归属跟踪很关键，因为它让 RitsuLib 可以安全回答这些问题：

- 某个类型是谁注册的？
- 它的固定公开条目标识应该是什么？
- 某些兼容逻辑是否应该把它当作 Mod 内容处理？

---

## 固定公开身份

对于通过 RitsuLib 注册的模型，公开 `ModelId.Entry` 会被强制成稳定格式：

```text
<MODID>_<CATEGORY>_<TYPENAME>
```

这不是靠改你源码里的类型名实现的，而是通过 ModelDb 身份补丁在公开入口上统一的。

这么做的意义在于：

- 本地化 Key 可预测
- 默认资源路径约定更稳定
- 补丁、存档、兼容逻辑里都更容易识别内容归属

这条规则只作用于显式通过 RitsuLib 注册的类型。

---

## ModelDb 集成

仅仅完成注册还不够，游戏本身还必须“看得到”这些内容。

RitsuLib 通过对 ModelDb 及相关访问点打补丁来完成这件事，包括：

- 追加已注册的角色、Act、能力、球体、事件、Ancient、共享卡池、**共享遗物池**（`AllRelicPools`）、**共享药水池**（`AllPotionPools`）、**调试用附魔**（`DebugEnchantments`）、**调试用苦难**（`DebugAfflictions`）、**成就**（`Achievements`）、**每日修正**（`GoodModifiers` / `BadModifiers`）等
- 将已注册卡牌/遗物/药水等与**目标池**绑定（`ModHelper.AddModelToPool`，在对应池的 `All*` 枚举中与原版生成结果拼接）
- 对已注册模型类型强制固定公开条目标识
- 在 `ModelDb` 初始化完成前，把注册器跟踪到的、位于**动态程序集**中的类型（例如 Reflection.Emit 占位）注入 `_contentById`
- 在缓存锁定前引导动态 Act 内容补丁

`MutuallyExclusiveModifiers` **不会**自动扩展；通过好/坏列表注册的 Mod 修正只会出现在上述两个列表中。

这也是为什么注册必须发生在框架冻结之前。

---

## Freeze 行为

几个关键注册器都会在早期初始化后冻结：

- 内容注册冻结
- 时间线注册冻结
- 解锁注册冻结

冻结之后再注册会直接抛异常。

这是有意为之，因为框架追求的是：

- 身份稳定
- 模型列表稳定
- 解锁/过滤行为稳定

如果某个 Mod 在太晚的时候才注册内容，最安全的结果就是尽早失败，而不是让游戏带着半成品缓存继续跑下去。

---

## Manifest 与 Entry 对象

如果你希望把注册描述成数据，可以使用注册条目对象：

```csharp
var contentEntries = new IContentRegistrationEntry[]
{
    new CharacterRegistrationEntry<MyCharacter>(),
    new CardRegistrationEntry<MyCardPool, MyCard>(),
};

var keywordEntries = new[]
{
    KeywordRegistrationEntry.OwnedCardByLocNamespace("MyMod", "brew"),
};

RitsuLibFramework.CreateContentPack("MyMod")
    .Manifest(contentEntries, keywordEntries)
    .Apply();
```

这对“声明式注册列表”或“跨模块复用注册清单”的场景会很方便。

也可以混用多种 `IContentRegistrationEntry`，例如：

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

## CLR 特性注册（可选）

`STS2RitsuLib.Interop.AutoRegistration` 下的特性（例如 `[RegisterSharedCardPool]`、`[RegisterCard(typeof(MyPool))]`）最终会调用与链式构建器、清单和**直接注册器**相同的底层 API。

它们在 RitsuLib 的早期 **Mod 类型发现** 阶段执行（`ModTypeDiscoveryPatch`）：内置的 `AttributeAutoRegistrationTypeDiscoveryContributor` 会扫描你已用 **`ModTypeDiscoveryHub.RegisterModAssembly(modId, Assembly.GetExecutingAssembly())`** 登记的程序集中的**具体** CLR 类型（在 `PatchAll` **之前**于 Mod 初始化器里调用）。类型必须能解析到某个 mod 身份（通常由 manifest 映射到程序集）；否则可在类型上使用 **`[RitsuLibOwnedBy("modId")]`**。

这**不代替** `CreateContentPack(...)`，只是另一种编写方式。只要注册顺序与冻结时机仍合法，可以与链式/清单混用。

### `AutoRegistrationAttribute.Inherit`

特性默认只作用于其标注的类型。**`Inherit`** 默认为 **`false`**。在**基类**上将某特性设为 **`Inherit = true`** 时，**具体子类**会按「若子类自身也写了同一条特性」的方式处理（即仍以**子类的** `Type` 调用同一套注册 API）。若子类已有**直接**声明、且会产生**相同注册签名**的特性，则不再重复应用继承来的同签名项。扫描会跳过抽象基类，仅具体类型会进入注册流程。

---

## 内容模型注册速查表

下表中每一行是一种**内容类别**。可主要用下面**三种**等价方式登记，另可加前一节所述的**可选特性路径**（另有注明的除外）：

1. **链式**：`CreateContentPack(...)` 上的 `ModContentPackBuilder` 方法  
2. **注册器**：`RitsuLibFramework.GetContentRegistry(modId)` 或 `Custom(ctx => ctx.Content...)` 上的 `ModContentRegistry` 方法  
3. **Manifest 条目**：`STS2RitsuLib.Scaffolding.Content` 中实现 `IContentRegistrationEntry` 的类型，经 `.Entry(...)`、`.Entries(...)` 或 `.Manifest(...)` 应用

| 内容 | 链式 | 注册器 | Manifest 条目 |
|---|---|---|---|
| 角色 | `.Character<T>()` | `RegisterCharacter<T>()` | `CharacterRegistrationEntry<T>` |
| Act | `.Act<T>()` | `RegisterAct<T>()` | `ActRegistrationEntry<T>` |
| 池内卡牌 | `.Card<TPool,TCard>(...)` | `RegisterCard<TPool,TCard>(...)` | `CardRegistrationEntry<TPool,TCard>` |
| 池内遗物 | `.Relic<TPool,TRelic>(...)` | `RegisterRelic<TPool,TRelic>(...)` | `RelicRegistrationEntry<TPool,TRelic>` |
| 池内药水 | `.Potion<TPool,TPotion>(...)` | `RegisterPotion<TPool,TPotion>(...)` | `PotionRegistrationEntry<TPool,TPotion>` |
| 能力 | `.Power<T>()` | `RegisterPower<T>()` | `PowerRegistrationEntry<T>` |
| 球体 | `.Orb<T>()` | `RegisterOrb<T>()` | `OrbRegistrationEntry<T>` |
| 附魔 | `.Enchantment<T>()` | `RegisterEnchantment<T>()` | `EnchantmentRegistrationEntry<T>` |
| 苦难 | `.Affliction<T>()` | `RegisterAffliction<T>()` | `AfflictionRegistrationEntry<T>` |
| 成就 | `.Achievement<T>()` | `RegisterAchievement<T>()` | `AchievementRegistrationEntry<T>` |
| 单例 | `.Singleton<T>()` | `RegisterSingleton<T>()` | `SingletonRegistrationEntry<T>` |
| 每日修正（好） | `.GoodModifier<T>()` | `RegisterGoodModifier<T>()` | `GoodModifierRegistrationEntry<T>` |
| 每日修正（坏） | `.BadModifier<T>()` | `RegisterBadModifier<T>()` | `BadModifierRegistrationEntry<T>` |
| 共享卡池 | `.SharedCardPool<T>()` | `RegisterSharedCardPool<T>()` | `SharedCardPoolRegistrationEntry<T>` |
| 共享遗物池 | `.SharedRelicPool<T>()` | `RegisterSharedRelicPool<T>()` | `SharedRelicPoolRegistrationEntry<T>` |
| 共享药水池 | `.SharedPotionPool<T>()` | `RegisterSharedPotionPool<T>()` | `SharedPotionPoolRegistrationEntry<T>` |
| 共享事件 | `.SharedEvent<T>()` | `RegisterSharedEvent<T>()` | `SharedEventRegistrationEntry<T>` |
| Act 遭遇 | `.ActEncounter<TAct,TEncounter>()` | `RegisterActEncounter<TAct,TEncounter>()` | `ActEncounterRegistrationEntry<TAct,TEncounter>` |
| Act 事件 | `.ActEvent<TAct,TEvent>()` | `RegisterActEvent<TAct,TEvent>()` | `ActEventRegistrationEntry<TAct,TEvent>` |
| 共享 Ancient | `.SharedAncient<T>()` | `RegisterSharedAncient<T>()` | `SharedAncientRegistrationEntry<T>` |
| Act Ancient | `.ActAncient<TAct,TAnc>()` | `RegisterActAncient<TAct,TAnc>()` | `ActAncientRegistrationEntry<TAct,TAncient>` |
| 怪物 | *（无链式封装）* | `RegisterMonster<T>()` | `MonsterRegistrationEntry<T>` |
| 占位卡牌/遗物/药水 | `.PlaceholderCard<...>(...)` 等 | `RegisterPlaceholderCard<...>(...)` 等 | `PlaceholderCardRegistrationEntry<...>` 等 |
| Archaic Tooth 映射 | `.ArchaicToothTranscendence<...>()` 或 `.ArchaicToothTranscendence(id, type)` | `RitsuLibFramework.RegisterArchaicToothTranscendenceMapping(...)` | `ArchaicToothTranscendenceRegistrationEntry<...>` / `ArchaicToothTranscendenceByIdRegistrationEntry` |
| Touch of Orobas 映射 | `.TouchOfOrobasRefinement<...>()` 或 `.TouchOfOrobasRefinement(id, type)` | `RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping(...)` | `TouchOfOrobasRefinementRegistrationEntry<...>` / `TouchOfOrobasRefinementByIdRegistrationEntry` |

**附魔：**可选用脚手架里的 `ModEnchantmentTemplate`、`IModEnchantmentAssetOverrides` 与 `EnchantmentIntendedIconPathPatch` 自定义图标路径；上表中的注册仍负责归属、固定 `ModelId.Entry` 以及与别类模型一致的动态程序集注入。

**单例：**本体没有可补丁的「全局单例列表」；注册仍用于归属与动态类型注入，以便 `ModelDb.Singleton<T>()` 能正确解析。

---

## 生成式占位内容

用于在**尚未为每张牌 / 每个遗物 / 每个药水编写独立 CLR 类型**时，仍能注册进池子并获得**稳定、可预测的公开 `ModelId.Entry`**（与 `ModelPublicEntryOptions.FromStem` / `FromFullPublicEntry` 一致），以便奖励表、解锁、存档引用等流程先跑通。占位模型由 RitsuLib 在运行时通过 **Reflection.Emit** 生成密封子类，逻辑上为**无效果**（卡牌 `OnPlay`、药水 `OnUse` 等为空操作）。

### API 概要

| 场景 | 推荐入口 |
|---|---|
| 链式内容包 | `PlaceholderCard<TPool>(stableEntryStem, PlaceholderCardDescriptor)`、`PlaceholderRelic<TPool>(...)`、`PlaceholderPotion<TPool>(...)` |
| 直接注册器 | `ModContentRegistry.RegisterPlaceholderCard<TPool>(...)` 等；重载可传入 `ModelPublicEntryOptions`（例如 `FromFullPublicEntry`） |
| 形状参数 | `PlaceholderCardDescriptor`、`PlaceholderRelicDescriptor`、`PlaceholderPotionDescriptor`（结构体，带默认值，按需覆盖费用、类型、稀有度、目标等） |
| 仍自带 CLR 类型时 | 保留 `PlaceholderCard<TPool, TCard>(stem)` 双泛型重载：仅为已有类型固定 entry，不生成新类型 |

框架内部的 `ModPlaceholderCardTemplate` / `ModPlaceholderRelicTemplate` / `ModPlaceholderPotionTemplate` 供生成类型继承；**一般不必在 Mod 里再继承它们**，除非你有特殊手写需求。

### 示例

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

遗物描述体中的 `MerchantCostOverride`：为 **`< 0`（默认 `-1`）** 时表示沿用稀有度默认商人价；**`≥ 0`** 时覆盖 `MerchantCost`。

### 与初始化顺序

若同时使用 `Manifest(...)` 与占位注册，请把占位步骤放在**已具备池类型等前置注册之后**（常见写法是在链上 `.Manifest(...)` 之后接 `.Custom(ctx => ...)` 调用 `RegisterPlaceholder*`），避免依赖尚未注册的池或角色。

---

### 警告（请务必阅读）

> **存档与 Entry 稳定性**  
> 占位一旦进入存档或解锁数据，其 `ModelId.Entry`（由 stem 或 `FromFullPublicEntry` 决定）即成为长期契约。**改名 / 改 stem / 改 `FromFullPublicEntry` 字符串**可能导致旧档、旧解锁引用失效。正式内容落地时，要么长期保留同一 entry，要么做迁移/兼容策略。

> **无玩法效果**  
> 占位不会替你实现伤害、抽牌、遗物触发等。仅保证模型存在、池子能展开、部分 UI/流程不因缺模型而崩溃；**平衡与体验仍可能异常**，需尽快替换为实作类型。

> **本地化与资源**  
> 占位仍使用基于 entry 的默认本地化键与资源路径约定；若未提供对应翻译或贴图，界面可能出现键名或缺图，这属于预期现象，不等于框架未注册成功。

> **联机与 `ModelIdSerializationCache.Hash`**  
> 生成类型**不会**出现在游戏原生的 `AllAbstractModelSubtypes` 扫描结果中。RitsuLib 会在 `ModelDb.Init` 前注入动态程序集中的已注册模型，并在 `ModelIdSerializationCache.Init` 之后**把 `ModelDb` 中实际存在的模型一并并入联机序列化表并重算 Hash**。  
> **后果**：加载的 Mod 组合不同 → Hash 不同 → 与未使用占位/未使用相同 Mod 列表的客户端**可能无法联机或回放一致**。这是使用动态占位时的固有风险，而非单机独有。

> **依赖 RitsuLib 版本**  
> 占位、`InjectDynamicRegisteredModels`、序列化缓存补丁等行为随 RitsuLib 演进；请为 Mod 声明合适的 `STS2-RitsuLib` 依赖版本，并在升级前置库后回归测试。

---

## 推荐注册模式

对大多数 Mod，建议这样组织：

1. 在初始化入口中创建一个内容包
2. 在其中注册所有内容、关键词、时间线节点与解锁规则
3. `Custom(...)` 保持小而显式
4. 不要把注册拖到运行期 hook 再做
5. 使用 `TypeListCardPoolModel` 时，用 `.Card<池, 牌>()` 或 `CardRegistrationEntry` 登记池内牌；**不要**覆写已过时的 `CardTypes`（基类已默认空序列，详见 [快速入门](GettingStarted.md)）

如果 Mod 很大，可以保留一个顶层构建器，再由子模块提供注册条目对象或辅助方法。

---

## 相关文档

- [内容注册规则](ContentAuthoringToolkit.md)
- [时间线与解锁](TimelineAndUnlocks.md)
- [框架设计](FrameworkDesign.md)
