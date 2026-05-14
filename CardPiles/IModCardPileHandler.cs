namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Optional handler contract implemented by classes that declare themselves with
    ///     <see cref="STS2RitsuLib.Interop.AutoRegistration.RegisterOwnedCardPileAttribute" />. When the
    ///     attribute sees a type implementing this interface, the auto-registration pipeline instantiates
    ///     the type once (requires a parameterless constructor) and wires its
    ///     <see cref="OnOpen" /> method into <see cref="ModCardPileSpec.OnOpen" />.
    ///     由声明 <see cref="STS2RitsuLib.Interop.AutoRegistration.RegisterOwnedCardPileAttribute" /> 的类可选实现的
    ///     handler contract。当 attribute 发现某个类型实现此接口时，auto-registration 管线会实例化
    ///     该类型一次（需要无参构造函数），并将其
    ///     <see cref="OnOpen" /> 方法接入 <see cref="ModCardPileSpec.OnOpen" />。
    /// </summary>
    /// <remarks>
    ///     The interface is entirely optional — annotated types may leave the button to open the default
    ///     <c>NCardPileScreen</c>. Handler instances are cached per registered pile, so the same instance
    ///     services every click for that pile's lifetime.
    ///     此接口完全可选；带注解类型可以让按钮打开默认
    ///     <c>NCardPileScreen</c>。handler 实例按已注册牌堆缓存，因此同一实例
    ///     会服务该牌堆生命周期内的每次点击。
    /// </remarks>
    public interface IModCardPileHandler
    {
        /// <summary>
        ///     Invoked when the pile's UI button is released. See <see cref="ModCardPileSpec.OnOpen" /> for
        ///     the full contract (empty-pile short-circuit, open-default-screen toggle, etc.).
        ///     <see cref="ModCardPileSpec.OnOpen" />。
        ///     牌堆的 UI 按钮释放时调用。完整契约（空牌堆短路、默认画面开关等）参见
        ///     <see cref="ModCardPileSpec.OnOpen" />。
        ///     <see cref="ModCardPileSpec.OnOpen" />。
        /// </summary>
        void OnOpen(ModCardPileOpenContext context);
    }
}
