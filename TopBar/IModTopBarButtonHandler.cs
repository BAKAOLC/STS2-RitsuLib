namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Optional counterpart of <see cref="ModTopBarButtonSpec.OnClick" /> /
    ///     <see cref="ModTopBarButtonSpec.VisibleWhen" /> for classes tagged with
    ///     <see cref="Interop.AutoRegistration.RegisterOwnedTopBarButtonAttribute" />. The
    ///     auto-registration pipeline instantiates the annotated type (parameterless ctor) and wires
    ///     <see cref="OnClick" /> / <see cref="IsVisible" /> into the produced spec.
    ///     <see cref="ModTopBarButtonSpec.OnClick" /> /
    ///     <see cref="ModTopBarButtonSpec.VisibleWhen" /> 的可选对应接口，用于带有
    ///     <see cref="Interop.AutoRegistration.RegisterOwnedTopBarButtonAttribute" /> 标记的类。
    ///     自动注册管线会实例化带注解的类型（无参构造函数），并将
    ///     <see cref="OnClick" /> / <see cref="IsVisible" /> 接入生成的 spec。
    /// </summary>
    public interface IModTopBarButtonHandler
    {
        /// <summary>
        ///     Invoked when the button is released (after the click tween starts).
        ///     按钮释放时调用（点击 tween 开始之后）。
        /// </summary>
        void OnClick(ModTopBarButtonContext context);

        /// <summary>
        ///     Returns true when the button should be visible for the current player. Called from the
        ///     button's <c>_Process</c>, so keep it cheap; returning true by default keeps the button
        ///     always visible (matching the <see cref="ModTopBarButtonSpec.VisibleWhen" />
        ///     <c>== null</c> behaviour).
        ///     当按钮应对当前玩家可见时返回 true。从按钮的
        ///     <c>_Process</c> 调用，因此应保持轻量；默认返回 true 会让按钮
        ///     始终可见（对应 <see cref="ModTopBarButtonSpec.VisibleWhen" />
        ///     <c>== null</c> 行为）。
        /// </summary>
        bool IsVisible(ModTopBarButtonContext context)
        {
            return true;
        }

        /// <summary>
        ///     Returns true when the associated screen / mode is currently "open", letting the button rock
        ///     in the vanilla top-bar-button style. Defaults to false (stateless button). Typical usage is
        ///     <c>ctx =&gt; ModScreenService.CurrentCapstoneScreen is MyScreen</c>.
        ///     当关联屏幕/模式当前为“打开”时返回 true，使按钮按原版顶部栏按钮样式摇动。
        ///     默认为 false（无状态按钮）。典型用法是
        /// </summary>
        bool IsOpen(ModTopBarButtonContext context)
        {
            return false;
        }

        /// <summary>
        ///     Returns the current count to display in the button's count badge. Defaults to -1, which
        ///     the auto-registration pipeline treats as "no count provider" — the badge stays hidden and
        ///     the button looks like a plain icon. Override this when the button tracks something the
        ///     player cares about a running total of (e.g. unlocked recipes).
        ///     返回要显示在按钮计数徽章中的当前计数。默认为 -1，
        ///     自动注册管线会将其视为“无计数提供器”，徽章保持隐藏，
        ///     按钮看起来像普通图标。当按钮跟踪玩家关心的运行总数时重写此项
        ///     （例如已解锁配方）。
        ///     （例如已解锁配方）。
        /// </summary>
        int GetCount(ModTopBarButtonContext context)
        {
            return -1;
        }
    }
}
