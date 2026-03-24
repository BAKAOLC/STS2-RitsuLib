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

`CreatePatcher` 的 `patcherName` 参数用于日志标识。同一个 Mod 可以创建多个 Patcher。完整补丁写法见 [PatchingGuide.md](PatchingGuide.md)。

---

## 3. 定义卡池

使用 `TypeListCardPoolModel` 并通过 `CardTypes` 列出所有属于该池的卡牌类型：

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
| `STS2-WineFox` | `WineFoxStrike` | card | `STS2_WINE_FOX_CARD_WINE_FOX_STRIKE` |

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

使用 `BeginModDataRegistration` 批量注册存档数据键。持久化条目以 class 为单位注册，同时需要注册 key 和文件名：

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

关于作用域、重载时机和迁移机制，可继续阅读 [PersistenceGuide.md](PersistenceGuide.md)。

---

## 继续阅读

- [内容注册规则](ContentAuthoringToolkit.md)
- [角色与解锁脚手架](CharacterAndUnlockScaffolding.md)
- [卡牌动态变量](CardDynamicVarToolkit.md)
- [生命周期事件](LifecycleEvents.md)
- [补丁系统](PatchingGuide.md)
- [持久化设计](PersistenceGuide.md)
- [本地化与关键词](LocalizationAndKeywords.md)
- [框架设计](FrameworkDesign.md)
