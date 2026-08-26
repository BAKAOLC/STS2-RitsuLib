namespace STS2RitsuLib.Interactions.RightClick.Patches
{
    internal static class ModRightClickInputConsumer
    {
        internal static bool TryDispatchAndConsumeInput(
            Func<bool> dispatch,
            Action consumeInput)
        {
            if (!dispatch())
                return false;

            consumeInput();
            return true;
        }
    }
}
