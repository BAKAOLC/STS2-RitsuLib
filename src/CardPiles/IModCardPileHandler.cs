namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines the optional open handler recognized on a type marked with
    ///         <see cref="STS2RitsuLib.Interop.AutoRegistration.RegisterOwnedCardPileAttribute" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义 <see cref="STS2RitsuLib.Interop.AutoRegistration.RegisterOwnedCardPileAttribute" />
    ///         所标记类型可实现的牌堆打开处理器。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Auto-registration creates one handler instance through a parameterless constructor and assigns
    ///         <see cref="OnOpen" /> to <see cref="ModCardPileSpec.OnOpen" />. A marked type that does not implement
    ///         this interface retains the default pile-screen behavior.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         自动注册会通过无参构造函数创建一个处理器实例，并将 <see cref="OnOpen" /> 赋给
    ///         <see cref="ModCardPileSpec.OnOpen" />。被标记但未实现此接口的类型仍使用默认牌堆界面。
    ///     </para>
    /// </remarks>
    public interface IModCardPileHandler
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Handles a nonempty pile after its UI button is released. See
        ///         <see cref="ModCardPileSpec.OnOpen" /> for the complete invocation contract.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         牌堆界面按钮释放后处理非空牌堆。完整调用约定参见 <see cref="ModCardPileSpec.OnOpen" />。
        ///     </para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The pile, player, definition, and button associated with the request.</para>
        ///     <para xml:lang="zh-CN">与请求关联的牌堆、玩家、定义和按钮。</para>
        /// </param>
        void OnOpen(ModCardPileOpenContext context);
    }
}
