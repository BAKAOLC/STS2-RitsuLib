# 内容注册规则

本文是内容编写的总览文档，聚焦注册入口、模型身份、本地化耦合关系以及资源覆写基础规则。

更详细的注册机制见 [内容包与注册器](ContentPacksAndRegistries.md)，更详细的资源语义见 [资源配置与回退规则](AssetProfilesAndFallbacks.md)。

---

## 注册接口

| 接口 | 说明 |
|---|---|
| `RitsuLibFramework.CreateContentPack(modId)` | 推荐入口：流式内容包构建器 |
| `RitsuLibFramework.GetContentRegistry(modId)` | 底层内容注册器 |
| `RitsuLibFramework.GetKeywordRegistry(modId)` | 关键词注册器 |
| `RitsuLibFramework.GetTimelineRegistry(modId)` | Timeline（故事/纪元）注册器 |
| `RitsuLibFramework.GetUnlockRegistry(modId)` | 解锁规则注册器 |

`CreateContentPack` 是推荐用法，将以上注册器封装为流式 API，调用 `Apply()` 时按添加顺序依次执行。

本文只保留总览层内容。关于构建器完整表面、清单式注册、固定条目标识归属和冻结机制，请阅读 [内容包与注册器](ContentPacksAndRegistries.md)。

---

## 内容包构建器

所有方法都支持链式调用，下面给出一个代表性示例：

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Card<MyCardPool, MyCard>()
    .Relic<MyRelicPool, MyRelic>()
    .CardKeywordOwnedByLocNamespace("my_keyword", iconPath: "res://MyMod/art/kw.png")
    .Story<MyStory>()
    .Epoch<MyEpoch>()
    .RequireEpoch<MyCard, MyEpoch>()
    .Custom(ctx => { /* 任意注册逻辑 */ })
    .Apply();
```

`Apply()` 返回 `ModContentPackContext`，可用于进一步访问各注册器。

---

## 模型 ID 规则

通过 RitsuLib 注册的模型，其 `ModelId.Entry` 使用以下固定格式：

```
<MODID>_<CATEGORY>_<TYPENAME>
```

每个字段规范化为**全大写、以下划线分隔**的标识符。

### 示例（Mod id `MyMod`）

| C# 类型 | 类别 | ModelId.Entry |
|---|---|---|
| `MyStrike` | card | `MY_MOD_CARD_MY_STRIKE` |
| `MyStarterRelic` | relic | `MY_MOD_RELIC_MY_STARTER_RELIC` |
| `MyCharacter` | character | `MY_MOD_CHARACTER_MY_CHARACTER` |

> 同一 Mod、同一类别下两个 CLR 类型名相同的模型会产生 Entry 冲突，必须通过重命名解决。

---

## 本地化规则

游戏本地化 Key 直接基于固定 `ModelId.Entry` 编写：

```json
{
  "MY_MOD_CARD_MY_STRIKE.title": "我的打击",
  "MY_MOD_CARD_MY_STRIKE.description": "造成 {damage} 点伤害。",
  "MY_MOD_RELIC_MY_STARTER_RELIC.title": "我的起始遗物"
}
```

`RitsuLibFramework.CreateModLocalization(...)` 是独立的本地化工具，与游戏的 `LocString` 模型 Key 管线相互独立。

---

## 资源覆写规则

RitsuLib 通过接口匹配，在渲染时将默认资源替换为 Mod 提供的资源。

### 卡牌资源覆写

继承 `ModCardTemplate` 后，通过 `AssetProfile`（推荐）或单独属性覆写：

```csharp
public class MyCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    // 统一通过 AssetProfile 配置（推荐）
    public override CardAssetProfile AssetProfile => new()
    {
        PortraitPath      = "res://MyMod/art/my_card.png",
        FramePath         = "res://MyMod/art/frame.png",
        FrameMaterialPath = "res://MyMod/art/frame.material",
    };

    // 或单独覆写某一项
    public override string? CustomPortraitPath => "res://MyMod/art/my_card.png";
}
```

卡牌支持的覆写大致包括 portrait、frame、portrait border、energy icon、overlay 与 banner 相关资源。

### 其他内容资源覆写

| 内容类型 | 支持字段 |
|---|---|
| Relic | icon、icon outline、big icon |
| Power | icon、big icon |
| Orb | 图标、视觉场景 |
| Potion | image、outline |

覆写行为如下：
1. 模型必须实现对应的 override 接口（直接或通过 `Mod*Template`）
2. override 成员必须返回非空路径
3. 如果资源路径不存在，RitsuLib 会输出一次警告，并回退到原始资源

这点对角色资源尤其重要，因为原版游戏对缺失角色资源几乎没有安全兜底。

完整资源配置结构、路径工厂辅助方法、占位角色规则与诊断策略见 [资源配置与回退规则](AssetProfilesAndFallbacks.md)。

---

## 注册时机

所有内容注册必须在框架冻结内容注册之前完成（游戏早期引导阶段）。冻结后继续注册属于无效操作并可能抛出异常。

冻结时触发的事件：`ContentRegistrationClosedEvent`

---

## 兼容规则

固定 Entry 规则**只作用于**通过 RitsuLib 内容注册器显式注册的模型类型，处理点为 `ModelDb.GetEntry(Type)`。未经 RitsuLib 注册的模型不受影响。

---

## 相关文档

- [快速入门](GettingStarted.md)
- [内容包与注册器](ContentPacksAndRegistries.md)
- [角色与解锁模板](CharacterAndUnlockScaffolding.md)
- [自定义事件](CustomEvents.md)
- [卡牌动态变量](CardDynamicVarToolkit.md)
- [本地化与关键词](LocalizationAndKeywords.md)
- [框架设计](FrameworkDesign.md)
- [资源配置与回退规则](AssetProfilesAndFallbacks.md)
