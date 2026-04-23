namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Optional handler contract implemented by classes that declare themselves with
    ///     <see cref="STS2RitsuLib.Interop.AutoRegistration.RegisterOwnedCardPileAttribute" />. When the
    ///     attribute sees a type implementing this interface, the auto-registration pipeline instantiates
    ///     the type once (requires a parameterless constructor) and wires its
    ///     <see cref="OnOpen" /> method into <see cref="ModCardPileSpec.OnOpen" />.
    /// </summary>
    /// <remarks>
    ///     The interface is entirely optional — annotated types may leave the button to open the default
    ///     <c>NCardPileScreen</c>. Handler instances are cached per registered pile, so the same instance
    ///     services every click for that pile's lifetime.
    /// </remarks>
    public interface IModCardPileHandler
    {
        /// <summary>
        ///     Invoked when the pile's UI button is released. See <see cref="ModCardPileSpec.OnOpen" /> for
        ///     the full contract (empty-pile short-circuit, open-default-screen toggle, etc.).
        /// </summary>
        void OnOpen(ModCardPileOpenContext context);
    }
}
