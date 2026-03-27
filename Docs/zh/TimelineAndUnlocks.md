# 时间线与解锁

本文是时间线注册与解锁语义的参考文档。

RitsuLib 将时间线注册和解锁规则拆成两个系统，配合使用。本文说明：

- `Story` / `Epoch` 的注册方式
- 脚手架模板的职责
- 解锁规则的判定机制
- 原版进度逻辑对 Mod 角色的局限性与 RitsuLib 的兼容桥接

---

## 两个注册器

| 注册器 | 职责 |
|---|---|
| `ModTimelineRegistry` | 注册 `StoryModel` 和 `EpochModel` |
| `ModUnlockRegistry` | 定义内容或纪元的解锁条件 |

在链式构建器里，对应：

- `.Story<TStory>()`、`.Epoch<TEpoch>()`
- `.RequireEpoch<TModel, TEpoch>()`、`.UnlockEpochAfter...()`

核心区别：

- **时间线注册**回答"这个东西是否存在"
- **解锁注册**回答"它什么时候可用"

---

## `Story` 注册

推荐使用 `ModStoryTemplate`：

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

`ModStoryTemplate` 的职责：

- 通过 `StoryKey` 自动生成规范化的故事标识
- 把 `EpochTypes` 解析成游戏需要的 `Epochs` 数组

---

## `Epoch` 注册

可以直接写原生 `EpochModel` 子类，也可以使用 RitsuLib 的脚手架模板：

| 模板 | 说明 |
|---|---|
| `CharacterUnlockEpochTemplate<TCharacter>` | 解锁角色本身的纪元 |
| `CardUnlockEpochTemplate` | 解锁额外卡牌的纪元 |
| `RelicUnlockEpochTemplate` | 解锁额外遗物的纪元 |
| `PotionUnlockEpochTemplate` | 解锁额外药水的纪元 |

这些模板主要负责：

- 生成时间线界面的解锁入队逻辑
- 通过 `ExpansionEpochTypes` 支持后续纪元展开

### 角色解锁纪元模板

`CharacterUnlockEpochTemplate<TCharacter>` 的内置行为：

- 向 `NTimelineScreen` 队列一个角色解锁
- 把待解锁角色写入进度存档
- 若配置了 `ExpansionEpochTypes`，继续把后续纪元加入时间线展开

### 卡牌/遗物/药水纪元模板

`CardUnlockEpochTemplate`、`RelicUnlockEpochTemplate`、`PotionUnlockEpochTemplate` 的工作方式相似：

- 声明要解锁的模型类型
- 模板通过 `ModelDb` 解析类型
- `UnlockText` 自动生成
- `QueueUnlocks()` 自动推入时间线界面

---

## Expansion Epochs

所有解锁纪元模板都支持：

```csharp
protected virtual IEnumerable<Type> ExpansionEpochTypes => [];
```

当前纪元完成时会自动把这些纪元作为时间线扩展加入，用于组织解锁链：

1. 先解锁角色
2. 再展开卡牌解锁
3. 再展开遗物解锁

---

## 注册时机与冻结

时间线和解锁两个注册器都会在早期初始化后冻结。原因是：

- 故事/纪元标识必须稳定
- 解锁过滤与兼容补丁需要面对最终确定的规则表

`Story`、`Epoch` 和解锁规则都应在初始化入口中注册，不要拖到运行期。

---

## 为内容设置 Epoch 门槛

当模型已注册，但应在某个纪元解锁后才出现时，使用 `RequireEpoch<TModel, TEpoch>()`。

常见用途：

- 后期卡牌在进度达成前不进入牌池
- 遗物只在特定故事分支后开放
- 共享 Ancient / 事件需要时间线进度门槛

RitsuLib 将门槛应用到多个访问入口：

- `UnlockState.Characters`
- 卡牌/遗物/药水的已解锁池查询
- 共享 Ancient 列表
- Act 生成出来的事件列表

这不是单纯 UI 过滤，而是真正影响游戏可提供内容的规则。

---

## 局后 Epoch 规则

`ModUnlockRegistry` 提供的常用便捷 API：

| 方法 | 说明 |
|---|---|
| `UnlockEpochAfterRunAs<TCharacter, TEpoch>()` | 使用指定角色完成一局后解锁 |
| `UnlockEpochAfterWinAs<TCharacter, TEpoch>()` | 使用指定角色胜利后解锁 |
| `UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(level)` | 指定进阶等级胜利后解锁 |
| `UnlockEpochAfterRunCount<TEpoch>(requiredRuns, requireVictory)` | 累计跑局次数后解锁 |

这些最终都转成 `PostRunEpochUnlockRule`。

也可以直接注册自定义规则：

```csharp
unlocks.RegisterPostRunRule(
    PostRunEpochUnlockRule.Create(
        epochId: new MyEpoch().Id,
        description: "在任意一次被放弃的 5 层进阶局后解锁",
        shouldUnlock: ctx => ctx.IsAbandoned && ctx.AscensionLevel >= 5));
```

---

## 累计进度型规则

| 方法 | 说明 |
|---|---|
| `UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(count)` | 精英击杀数 |
| `UnlockEpochAfterBossVictories<TCharacter, TEpoch>(count)` | Boss 击杀数 |
| `UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()` | 进阶 1 胜利 |
| `RevealAscensionAfterEpoch<TCharacter, TEpoch>()` | 纪元后显示进阶 |
| `UnlockCharacterAfterRunAs<TCharacter, TEpoch>()` | 使用角色后解锁角色 |

---

## 兼容补丁

> 以下解释原版进度系统对 Mod 角色的局限性，以及 RitsuLib 的桥接策略。

原版的若干进度检查是按原版角色设计的，不会自然支持 Mod 角色。RitsuLib 通过以下窄桥接补丁，让注册的解锁规则在这些检查点上生效：

- 精英击杀计数的纪元判定桥接
- Boss 击杀计数的纪元判定桥接
- 进阶 1 的纪元判定桥接
- 局后角色解锁纪元桥接
- 进阶显示解锁判定桥接

这些补丁并不重写原版进度系统，只是在原版会跳过 Mod 角色的节点上补一层桥。这也是为什么解锁注册器会显式按 `ModelId` 保存规则，而不是试图仅从时间线图推断全部进度逻辑。

---

## 推荐模式

对故事驱动型角色 Mod：

1. 在一个内容包里注册角色、池、纪元和故事
2. 用 `CharacterUnlockEpochTemplate<TCharacter>` 作为角色解锁纪元
3. 用卡牌/遗物/药水纪元模板做后续内容展开
4. 用 `RequireEpoch<TModel, TEpoch>()` 给后期内容加门槛
5. 使用少量清晰的进度规则，而不是堆叠重叠规则

---

## 构建器示例

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

## 常见错误

- 注册了纪元，却忘了注册包含这些纪元的故事
- 在时间线冻结之后才注册故事/纪元
- 给内容设置了 `RequireEpoch`，却没有任何规则能真正解锁该纪元
- 对同一个纪元叠很多重叠解锁规则，却没有明确设计理由
- 误以为原版累计进度逻辑会自动兼容 Mod 角色，而没有注册 RitsuLib 解锁规则

---

## 相关文档

- [角色与解锁脚手架](CharacterAndUnlockScaffolding.md)
- [内容包与注册器](ContentPacksAndRegistries.md)
- [诊断与兼容层](DiagnosticsAndCompatibility.md)
- [框架设计](FrameworkDesign.md)
