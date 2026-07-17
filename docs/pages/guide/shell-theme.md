---
title:
  en: Runtime UI And Shell Theme
  zh-CN: 运行时 UI 与 Shell 主题
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Settings Shell Theme{lang="en"}

::: en

RitsuLib settings pages use shell theme tokens for colors, typography, spacing, and component defaults. Most mods should rely on the active theme. Add a custom theme only when your settings UI needs a coherent visual identity across several custom controls.

Theme JSON files belong in your mod resources and are registered through the shell theme APIs. Keep token names semantic: describe the role, not the color.

:::

## 设置 Shell 主题{lang="zh-CN"}

::: zh-CN

RitsuLib 设置页面使用 shell theme token 控制颜色、字体、间距和控件默认样式。大多数 Mod 直接使用当前主题即可。只有当你的设置 UI 有多个自定义控件，并且需要统一视觉身份时，才添加自定义主题。

主题 JSON 放在 Mod 资源中，并通过 shell theme API 注册。Token 名称应表达用途，不要只描述颜色。

:::

## Toasts{lang="en"}

::: en

Use toast messages for short runtime feedback that should not interrupt play.

```csharp
RitsuToastService.ShowInfo("Settings saved.", "My Mod");
RitsuToastService.ShowWarning("Optional bank failed to load.", "My Mod");
RitsuToastService.ShowError("Required setup failed.", "My Mod");
```

For custom placement, image, duration, or click behavior, build a `RitsuToastRequest`.
Non-persistent toasts show a small remaining-time progress bar; persistent toasts omit it.

Use `ShowTracked` when later code needs to update or close the same toast.

```csharp
var toast = RitsuToastService.ShowTracked(
    RitsuToastRequest.Info("Loading...", "My Mod")
        .Persistent());

toast.UpdateBody("Loaded.");
toast.ResetDuration(2.0d);

// Or close it from a cancellation path:
toast.Close();
```

:::

## Toast{lang="zh-CN"}

::: zh-CN

短运行时反馈可以使用 toast，不打断玩家操作。

```csharp
RitsuToastService.ShowInfo("设置已保存。", "My Mod");
RitsuToastService.ShowWarning("可选 bank 加载失败。", "My Mod");
RitsuToastService.ShowError("必要初始化失败。", "My Mod");
```

需要自定义位置、图片、持续时间或点击行为时，构建 `RitsuToastRequest`。
非持久 toast 会显示一条剩余时间进度条；持久 toast 不显示。

后续代码需要更新或关闭同一个 toast 时，使用 `ShowTracked`。

```csharp
var toast = RitsuToastService.ShowTracked(
    RitsuToastRequest.Info("加载中...", "My Mod")
        .Persistent());

toast.UpdateBody("加载完成。");
toast.ResetDuration(2.0d);

// 或在取消路径中关闭它：
toast.Close();
```

:::

## Runtime Hotkeys{lang="en"}

::: en

Register hotkeys when the action must be available outside a settings page.

```csharp
var handle = RuntimeHotkeyService.Register(
    "Ctrl+Shift+M",
    callback: ToggleMyOverlay,
    id: "my_mod.toggle_overlay",
    title: "Toggle overlay");
```

Expose hotkeys in settings with `AddKeyBinding`, `AddMultiKeyBinding`, or `AddRuntimeHotkeySummary`.

:::

## 运行时快捷键{lang="zh-CN"}

::: zh-CN

当操作需要在设置页面之外可用时，注册运行时快捷键。

```csharp
var handle = RuntimeHotkeyService.Register(
    "Ctrl+Shift+M",
    callback: ToggleMyOverlay,
    id: "my_mod.toggle_overlay",
    title: "Toggle overlay");
```

在设置页面中展示或编辑快捷键时，使用 `AddKeyBinding`、`AddMultiKeyBinding` 或 `AddRuntimeHotkeySummary`。

:::

## Top-Bar Buttons{lang="en"}

::: en

Register top-bar buttons through the content pack:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .TopBarButtonOwned("my_panel", new ModTopBarButtonSpec(
        IconPath: "res://MyMod/images/ui/my_panel.png",
        Handler: new MyTopBarButtonHandler()))
    .Apply();
```

Owned button ids use `MY_MOD_TOPBARBUTTON_MY_PANEL` and read hover text from `static_hover_tips`.

:::

## 顶栏按钮{lang="zh-CN"}

::: zh-CN

通过 content pack 注册顶栏按钮：

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .TopBarButtonOwned("my_panel", new ModTopBarButtonSpec(
        IconPath: "res://MyMod/images/ui/my_panel.png",
        Handler: new MyTopBarButtonHandler()))
    .Apply();
```

Owned button id 会是 `MY_MOD_TOPBARBUTTON_MY_PANEL`，hover 文本从 `static_hover_tips` 读取。

:::

## Card Piles{lang="en"}

::: en

Custom piles are useful for extra hand-like or discard-like collections. An `ExtraHand` pile uses interactive
vanilla card holders, including hover enlargement, keyword tips, controller focus, playable glow, and optional
manual play through the normal targeting, resource-spend, hook, queue, and result-pile pipeline. Its default
`VanillaHand` layout uses the same dynamic fan, scale, rotation, hover lift, and neighboring-card displacement as
the player hand. Select `Horizontal` or `Vertical` to use the configurable linear spacing and scale instead.

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .CardPileOwned("archive", new ModCardPileSpec
    {
        Style = ModCardPileUiStyle.ExtraHand,
        Anchor = ModCardPileAnchor.AtCenter(new Vector2(260f, 520f)),
        CardShouldBeVisible = true,
        ExtraHand = new ModCardPileExtraHandSpec
        {
            Direction = ModExtraHandLayoutDirection.Vertical,
            Spacing = 86f,
            CardScale = Vector2.One * 0.55f,
            HoverScale = Vector2.One,
            ShowPlayableGlow = true,
            AllowCardPlay = true,
        },
    })
    .Apply();
```

Use `LayoutResolver` for per-card transforms and `OnCardVisualCreated` / `OnCardArrived` for custom presentation
animations. `FlightTargetPositionResolver` and `FlightStartPositionResolver` customize the vanilla flight endpoints.

When an extra-hand card is manually played, RitsuLib temporarily bridges it through the backend vanilla hand so
the sealed vanilla `PlayCardAction` accepts it. A canceled target selection or queued action restores the card to
its source pile. Consequently, vanilla logic that inspects `PileType.Hand` during targeting sees the card as a hand
card, which is normally the desired semantics for a playable extra hand.

Use stable local stems. Pile ids are part of UI text, persistence state, and localization keys. Add the generated
pile id's `.title`, `.description`, and `.empty` entries to `static_hover_tips.json`.

:::

## 自定义卡堆{lang="zh-CN"}

::: zh-CN

自定义卡堆适合额外的手牌区、弃牌区或类似集合。`ExtraHand` 牌堆使用可交互的原版卡牌 holder，
支持悬停放大、关键词提示、手柄焦点、可打出发光，并可选择通过原版目标选择、资源支付、hook、
播放队列和结果牌堆流程手动打出。默认的 `VanillaHand` 布局使用与玩家手牌相同的动态扇形、缩放、
旋转、悬停抬升和邻牌让位规则；显式选择 `Horizontal` 或 `Vertical` 后则使用可配置间距和缩放的线性布局。

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .CardPileOwned("archive", new ModCardPileSpec
    {
        Style = ModCardPileUiStyle.ExtraHand,
        Anchor = ModCardPileAnchor.AtCenter(new Vector2(260f, 520f)),
        CardShouldBeVisible = true,
        ExtraHand = new ModCardPileExtraHandSpec
        {
            Direction = ModExtraHandLayoutDirection.Vertical,
            Spacing = 86f,
            CardScale = Vector2.One * 0.55f,
            HoverScale = Vector2.One,
            ShowPlayableGlow = true,
            AllowCardPlay = true,
        },
    })
    .Apply();
```

使用 `LayoutResolver` 可以逐卡指定变换，使用 `OnCardVisualCreated` / `OnCardArrived` 可以接入自定义
展示动画；`FlightTargetPositionResolver` 与 `FlightStartPositionResolver` 用于自定义原版飞行动画端点。

手动打出额外手牌卡牌时，RitsuLib 会临时将它桥接到后端原版手牌，使封闭的原版 `PlayCardAction`
接受该卡牌。取消目标选择或已排队动作时，卡牌会恢复到来源牌堆。因此，目标选择期间检查
`PileType.Hand` 的原版逻辑会将该卡视为手牌；对于“可打出的额外手牌”，这通常正是预期语义。

使用稳定 local stem。卡堆 ID 会影响 UI 文本、持久化状态与本地化 key。请在
`static_hover_tips.json` 中加入生成后牌堆 ID 对应的 `.title`、`.description` 与 `.empty` 条目。

:::
