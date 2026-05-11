---
title:
  en: Timeline And Unlocks
  zh-CN: 时间线与解锁
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Add Story And Epoch Models{lang="en"}

::: en

Use `ModStoryTemplate` and `ModEpochTemplate` when the mod adds progression nodes.

```csharp
public sealed class MyStory : ModStoryTemplate
{
    protected override string StoryKey => "my_mod_story";
}

public sealed class MyEpoch : ModEpochTemplate
{
    public override string Id => "MY_MOD_FIRST_LESSON";
}
```

Write title, description, unlock text, and story title in `epochs.json`. Register them with attributes:

```csharp
[RegisterStory]
public sealed class MyStory : ModStoryTemplate
{
    protected override string StoryKey => "my_mod_story";
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(MyStory))]
[AutoTimelineSlotAfterColumn(EpochEra.Act1)]
public sealed class MyEpoch : ModEpochTemplate
{
    public override string Id => "MY_MOD_FIRST_LESSON";
}
```

Or register them with a content pack:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Story<MyStory>()
    .Epoch<MyEpoch>()
    .StoryEpoch<MyStory, MyEpoch>()
    .ModEpochAutoTimelineSlotAfterColumn<MyEpoch>(EpochEra.Act1)
    .Apply();
```

:::

## 添加 Story 与 Epoch{lang="zh-CN"}

::: zh-CN

Mod 添加进度节点时，使用 `ModStoryTemplate` 和 `ModEpochTemplate`。

```csharp
public sealed class MyStory : ModStoryTemplate
{
    protected override string StoryKey => "my_mod_story";
}

public sealed class MyEpoch : ModEpochTemplate
{
    public override string Id => "MY_MOD_FIRST_LESSON";
}
```

标题、描述、解锁文本和故事标题写在 `epochs.json`。可以用注解注册：

```csharp
[RegisterStory]
public sealed class MyStory : ModStoryTemplate
{
    protected override string StoryKey => "my_mod_story";
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(MyStory))]
[AutoTimelineSlotAfterColumn(EpochEra.Act1)]
public sealed class MyEpoch : ModEpochTemplate
{
    public override string Id => "MY_MOD_FIRST_LESSON";
}
```

也可以用 content pack 注册：

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Story<MyStory>()
    .Epoch<MyEpoch>()
    .StoryEpoch<MyStory, MyEpoch>()
    .ModEpochAutoTimelineSlotAfterColumn<MyEpoch>(EpochEra.Act1)
    .Apply();
```

:::

## Gate Content{lang="en"}

::: en

Use `.RequireEpoch<TModel, TEpoch>()` for content that should stay hidden until an epoch is obtained.

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Card<MyCardPool, MyRareCard>()
    .Epoch<MyEpoch>()
    .RequireEpoch<MyRareCard, MyEpoch>()
    .Apply();
```

For epoch templates that list their own cards or relics, use `.BindCardUnlockEpoch<TEpoch>()` or `.BindRelicUnlockEpoch<TEpoch>()`.

:::

## 锁定内容{lang="zh-CN"}

::: zh-CN

获得某个 epoch 前需要隐藏的内容，使用 `.RequireEpoch<TModel, TEpoch>()`。

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Card<MyCardPool, MyRareCard>()
    .Epoch<MyEpoch>()
    .RequireEpoch<MyRareCard, MyEpoch>()
    .Apply();
```

如果 epoch 模板自己列出了卡牌或遗物，使用 `.BindCardUnlockEpoch<TEpoch>()` 或 `.BindRelicUnlockEpoch<TEpoch>()`。

:::

## Unlock Epochs{lang="en"}

::: en

Choose the rule that matches the player action:

| Rule | Trigger |
| --- | --- |
| `.UnlockEpochAfterRunAs<TCharacter, TEpoch>()` | A run finishes with the character. |
| `.UnlockEpochAfterWinAs<TCharacter, TEpoch>()` | A run is won with the character. |
| `.UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(level)` | A qualifying ascension win. |
| `.UnlockEpochAfterRunCount<TEpoch>(count, requireVictory)` | Account-level run count. |
| `.UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(count)` | Counted elite victories. |
| `.UnlockEpochAfterBossVictories<TCharacter, TEpoch>(count)` | Counted boss victories. |
| `.UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()` | First ascension path. |
| `.RevealAscensionAfterEpoch<TCharacter, TEpoch>()` | Ascension UI reveal after an epoch. |

Keep unlock rules close to the content they gate. That makes release review much easier.

:::

## 解锁 Epoch{lang="zh-CN"}

::: zh-CN

按玩家行为选择规则：

| 规则 | 触发条件 |
| --- | --- |
| `.UnlockEpochAfterRunAs<TCharacter, TEpoch>()` | 使用该角色完成一局。 |
| `.UnlockEpochAfterWinAs<TCharacter, TEpoch>()` | 使用该角色获胜。 |
| `.UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(level)` | 达到指定进阶要求并获胜。 |
| `.UnlockEpochAfterRunCount<TEpoch>(count, requireVictory)` | 账号级 run 次数。 |
| `.UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(count)` | 精英胜利计数。 |
| `.UnlockEpochAfterBossVictories<TCharacter, TEpoch>(count)` | Boss 胜利计数。 |
| `.UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()` | 第一进阶路径。 |
| `.RevealAscensionAfterEpoch<TCharacter, TEpoch>()` | 获得 epoch 后显示进阶 UI。 |

解锁规则尽量和被它锁定的内容放在附近，发布前更容易审查。

:::

## Character Unlocks{lang="en"}

::: en

To unlock a character from a previous character's run path, use:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Epoch<MyCharacterEpoch>()
    .UnlockCharacterAfterRunAs<MyPreviousCharacter, MyCharacterEpoch>()
    .Apply();
```

Align the character's own `RequiresEpochAndTimeline` setting with the rules you register.

:::

## 角色解锁{lang="zh-CN"}

::: zh-CN

如果角色应由另一个角色的 run 路径解锁，使用：

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Epoch<MyCharacterEpoch>()
    .UnlockCharacterAfterRunAs<MyPreviousCharacter, MyCharacterEpoch>()
    .Apply();
```

角色自身的 `RequiresEpochAndTimeline` 应与你注册的规则保持一致。

:::
