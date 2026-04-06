using Godot;

namespace STS2RitsuLib.Multiplayer.ChunkedPayload
{
    /// <summary>
    ///     Bridges <see cref="ChunkedTransferOptions.CompletionDispatcher" /> to the Godot main thread using
    ///     <c>Callable.From(…).CallDeferred()</c> (same pattern as other RitsuLib code).
    /// </summary>
    public static class ChunkedPayloadGodotDispatch
    {
        /// <summary>
        ///     Returns a dispatcher that schedules work on Godot’s main thread message queue.
        /// </summary>
        public static Action<Action> ForMainThread()
        {
            return action => Callable.From(action).CallDeferred();
        }
    }
}
