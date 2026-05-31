---
title:
  en: Creature Visuals And Animation
  zh-CN: 生物视觉与动画
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Pick The Smallest Hook{lang="en"}

::: en

Choose the lightest visual hook that matches the job:

| Need | Use |
| --- | --- |
| Swap a packaged visuals scene | `CharacterAssetProfile.Scenes.VisualsPath` or `MonsterAssetProfile.VisualsScenePath` |
| Use static frame cues | `VisualCueSet` on `CharacterAssetProfile` |
| Build the node tree in code | Override `TryCreateCreatureVisuals()` |
| Drive combat animations yourself | Override `SetupCustomCombatAnimationStateMachine(...)` |
| Drive merchant / rest-site visuals | Override `SetupCustomMerchantAnimationStateMachine(...)` or `WorldProceduralVisuals` |

Start with paths and profiles. Move to factories only when static resources cannot express the behavior.

:::

## 选择最小 Hook{lang="zh-CN"}

::: zh-CN

按需求选择最轻的视觉接入点：

| 需求 | 使用 |
| --- | --- |
| 替换打包好的视觉场景 | `CharacterAssetProfile.Scenes.VisualsPath` 或 `MonsterAssetProfile.VisualsScenePath` |
| 使用静态帧 cue | `CharacterAssetProfile` 上的 `VisualCueSet` |
| 用代码构建节点树 | 覆写 `TryCreateCreatureVisuals()` |
| 自己驱动战斗动画 | 覆写 `SetupCustomCombatAnimationStateMachine(...)` |
| 驱动商店 / 篝火视觉 | 覆写 `SetupCustomMerchantAnimationStateMachine(...)` 或使用 `WorldProceduralVisuals` |

从路径和 profile 开始。只有静态资源表达不了行为时，再使用 factory。

:::

## Visual Cues{lang="en"}

::: en

`VisualCueSet` is useful for non-Spine characters that can be described by named stills or frame sequences.

```csharp
public override CharacterAssetProfile AssetProfile => new()
{
    VisualCues = new VisualCueSet(
        texturePathByCue: new Dictionary<string, string>
        {
            ["idle"] = "res://MyMod/images/character/idle.png",
            ["hit"] = "res://MyMod/images/character/hit.png",
        })
};
```

For a still frame that should hold briefly and then let the state machine return to idle, use the builder:

```csharp
public override CharacterAssetProfile AssetProfile => new()
{
    VisualCues = VisualCueSetBuilder.Create()
        .Single("idle", "res://MyMod/images/character/idle.png")
        .Single("attack", "res://MyMod/images/character/attack.png", 0.5f)
        .Build()
};
```

Keep cue names aligned with the animation states your model or state machine will request.

:::

## Visual Cue{lang="zh-CN"}

::: zh-CN

`VisualCueSet` 适合可用命名静态图或帧序列表达的非 Spine 角色。

```csharp
public override CharacterAssetProfile AssetProfile => new()
{
    VisualCues = new VisualCueSet(
        texturePathByCue: new Dictionary<string, string>
        {
            ["idle"] = "res://MyMod/images/character/idle.png",
            ["hit"] = "res://MyMod/images/character/hit.png",
        })
};
```

如果某个静态帧需要短暂停留，然后让状态机回到 idle，可以用 builder：

```csharp
public override CharacterAssetProfile AssetProfile => new()
{
    VisualCues = VisualCueSetBuilder.Create()
        .Single("idle", "res://MyMod/images/character/idle.png")
        .Single("attack", "res://MyMod/images/character/attack.png", 0.5f)
        .Build()
};
```

cue 名称应与模型或状态机请求的动画状态一致。

:::

## State Machines{lang="en"}

::: en

Use `ModAnimStateMachines` or `ModAnimStateMachineBuilder` when you need explicit animation state transitions.

```csharp
protected override ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(
    Node visualsRoot,
    CharacterModel character)
{
    return ModAnimStateMachines.Standard(
        CompositeBackendFactory.FromNode(visualsRoot));
}
```

Return `null` when the normal vanilla animation path should run.

:::

## 状态机{lang="zh-CN"}

::: zh-CN

需要明确控制动画状态切换时，使用 `ModAnimStateMachines` 或 `ModAnimStateMachineBuilder`。

```csharp
protected override ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(
    Node visualsRoot,
    CharacterModel character)
{
    return ModAnimStateMachines.Standard(
        CompositeBackendFactory.FromNode(visualsRoot));
}
```

返回 `null` 表示继续使用普通原版动画路径。

:::

## Practical Notes{lang="en"}

::: en

- Keep visual resources in the mod PCK and reference them with `res://`.
- Register Godot scripts before any scene is instantiated.
- Use one visible fallback pose for every non-Spine creature.
- Test death, hit, attack, cast, idle, and relaxed states before release.
- For merchant and rest-site visuals, verify both normal UI entry and reload-from-save paths.

:::

## 实用注意点{lang="zh-CN"}

::: zh-CN

- 视觉资源放进 Mod PCK，并用 `res://` 引用。
- 任何场景实例化前先注册 Godot 脚本。
- 非 Spine 生物至少准备一个可见回退姿势。
- 发布前测试死亡、受击、攻击、施放、待机和 relaxed 状态。
- 商店与篝火视觉需要同时验证正常进入和读档恢复路径。

:::
