# 快速入门

本指南覆盖从声明依赖到注册第一个内容的完整流程。

---

## 1. 声明依赖

在 `mod_manifest.json` 中添加：

```json
{
  "id": "MyMod",
  "name": "My Mod",
  "dependencies": ["STS2-RitsuLib"]
}
```

---

## 2. 初始化 Mod

使用 `[ModInitializer]` 声明入口方法，在其中获取 Logger、创建 Patcher 并注册内容：

```csharp
using System.Reflection;
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
        RitsuLibFramework.EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly(), Logger);

        var patcher = RitsuLibFramework.CreatePatcher("MyMod", "core-patches");
        patcher.RegisterPatches<MyModPatches>();
        patcher.PatchAll();

        RitsuLibFramework.CreateContentPack("MyMod")
            .Character<MyCharacter>()
            .Card<MyCardPool, MyCard>()
            .Card<MyCardPool, MyOtherCard>()
            .Relic<MyRelicPool, MyRelic>()
            .Apply();
    }
}
```

链式方法、`ModContentRegistry` 与 `IContentRegistrationEntry`（附魔、成就、共享池、Manifest 等）的完整对照见 [内容包与注册器](ContentPacksAndRegistries.md)。

`CreatePatcher` 的 `patcherName` 参数用于日志标识。同一个 Mod 可以创建多个 Patcher。完整补丁写法见 [补丁系统](PatchingGuide.md)。

如果你的 Mod 使用了自定义 Godot C# 场景脚本，请把 `EnsureGodotScriptsRegistered(...)` 保留在初始化入口里。详见 [Godot 场景编写说明](GodotSceneAuthoring.md)。

---

## 3. 定义卡池

使用 `TypeListCardPoolModel` 承载池的视觉与元数据（边框、能量色等）。**属于该池的每张牌**必须在内容包里通过 `.Card<MyCardPool, MyCard>()`、`CardRegistrationEntry<…>` 或等价步骤登记，这样才会写入 `ModContentRegistry` 归属与固定 `ModelId.Entry`，并走 `ModHelper.AddModelToPool`。

基类已为 `CardTypes` 提供**默认空序列**，并已标记 `[Obsolete]`：**新 Mod 不必覆写 `CardTypes`**，也不必再写 `=> []`。与第 2 节一致，以链式 / Manifest 为卡牌清单的唯一来源即可。

```csharp
using Godot;

public class MyCardPool : TypeListCardPoolModel
{
    public override string Title => "My Pool";
    public override string EnergyColorName => "orange";
    public override string CardFrameMaterialPath => "card_frame_orange";
    public override Color DeckEntryCardColor => new("d2a15a");
    public override bool IsColorless => false;
}
```

若旧工程仍**覆写** `CardTypes` 并在其中列举类型，会收到 **CS0618**，且若同时对同一池、同一张牌做了内容包注册，`AllCards` 仍会重复拼接；此时应迁移为「仅内容包注册」或仅为该覆写添加 `#pragma warning disable CS0618`。仅 `CardTypes`、不做卡牌注册时，通常拿不到 RitsuLib 固定 Entry 与归属，不建议。

**生成式占位**：若尚未为每张牌编写 CLR 类型，但需要稳定 `ModelId` 让奖励、解锁等流程先跑通，可使用 `PlaceholderCard<TPool>(...)` 及遗物/药水对应 API。完整说明、示例与**必读警告**（存档 entry、联机 `ModelIdSerializationCache` Hash、无玩法效果等）见 [内容包与注册器](ContentPacksAndRegistries.md) 中的「生成式占位内容」一节。

---

## 4. 定义卡牌

继承 `ModCardTemplate`，在主构造函数中传入基础属性：

```csharp
public class MyCard : ModCardTemplate(
    baseCost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.SingleEnemy)
{
    public override string Title => "打击";
    public override string Description => $"造成 {Damage} 点伤害。";

    // 可选：自定义立绘路径
    public override string? CustomPortraitPath => "res://MyMod/art/strike.png";

    public override void Use(ICombatContext ctx, ICreatureState user, ICreatureState? target)
    {
        ctx.DealDamage(user, target, Damage);
    }
}
```

---

## 5. 本地化 Key

RitsuLib 注册的所有模型，其 `ModelId.Entry` 由以下规则推导（各字段规范化为全大写下划线格式）：

```
<MODID>_<CATEGORY>_<TYPENAME>
```

| Mod Id | C# 类型 | 类别 | Entry |
|---|---|---|---|
| `MyMod` | `MyCard` | card | `MY_MOD_CARD_MY_CARD` |
| `MyMod` | `MyRelic` | relic | `MY_MOD_RELIC_MY_RELIC` |
| `MyMod` | `MyCharacter` | character | `MY_MOD_CHARACTER_MY_CHARACTER` |

本地化文件示例：

```json
{
  "MY_MOD_CARD_MY_CARD.title": "打击",
  "MY_MOD_CARD_MY_CARD.description": "造成 {damage} 点伤害。"
}
```

---

## 6. 订阅生命周期事件

```csharp
// 游戏就绪后执行一次
RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
{
    Logger.Info("游戏已就绪。");
});

// 每次战斗开始时
RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt =>
{
    // evt.RunState, evt.CombatState
});
```

可重放事件（`IReplayableFrameworkLifecycleEvent`）即使在事件已发生后订阅也会立即回调，无需关心订阅时机。

---

## 7. 数据持久化

使用 `BeginModDataRegistration` 批量注册存档数据键。持久化条目以类为单位注册，同时需要注册键和文件名：

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

关于作用域、重载时机和迁移机制，可继续阅读 [持久化设计](PersistenceGuide.md)。

---

## 继续阅读

- [内容注册规则](ContentAuthoringToolkit.md)
- [角色与解锁模板](CharacterAndUnlockScaffolding.md)
- [卡牌动态变量](CardDynamicVarToolkit.md)
- [生命周期事件](LifecycleEvents.md)
- [补丁系统](PatchingGuide.md)
- [持久化设计](PersistenceGuide.md)
- [本地化与关键词](LocalizationAndKeywords.md)
- [框架设计](FrameworkDesign.md)
- [内容包与注册器](ContentPacksAndRegistries.md)
- [Godot 场景编写说明](GodotSceneAuthoring.md)
- [时间线与解锁](TimelineAndUnlocks.md)
- [资源配置与回退规则](AssetProfilesAndFallbacks.md)
- [诊断与兼容层](DiagnosticsAndCompatibility.md)
