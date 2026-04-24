using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches
{
    /// <summary>
    ///     Keeps dynamic hand-outline colors fresh by polling once per process frame while the holder is alive.
    /// </summary>
    internal sealed class NHandCardHolderDynamicOutlineTickPatch : IPatchMethod
    {
        private static readonly ConcurrentDictionary<ulong, CancellationTokenSource> TokensByHolderId = new();

        public static string PatchId => "n_hand_card_holder_dynamic_outline_tick";

        public static string Description => "Refresh dynamic hand-outline colors every process frame";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NHandCardHolder), nameof(NHandCardHolder._Ready)),
                new(typeof(NHandCardHolder), nameof(NHandCardHolder._ExitTree)),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(NHandCardHolder __instance)
            // ReSharper restore InconsistentNaming
        {
            var id = __instance.GetInstanceId();
            if (!TokensByHolderId.TryAdd(id, new()))
                return;

            var cts = TokensByHolderId[id];
            TaskHelper.RunSafely(RunDynamicRefreshLoop(__instance, id, cts.Token));
        }

        // ReSharper disable InconsistentNaming
        public static void Prefix(NHandCardHolder __instance)
            // ReSharper restore InconsistentNaming
        {
            StopLoop(__instance.GetInstanceId());
        }

        private static async Task RunDynamicRefreshLoop(NHandCardHolder holder, ulong id, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && GodotObject.IsInstanceValid(holder))
                {
                    ModCardHandOutlineRegistry.TryRefreshDynamicOutlineForHolder(holder);
                    await holder.ToSignal(holder.GetTree(), SceneTree.SignalName.ProcessFrame);
                }
            }
            finally
            {
                StopLoop(id);
            }
        }

        private static void StopLoop(ulong id)
        {
            if (!TokensByHolderId.TryRemove(id, out var cts))
                return;

            cts.Cancel();
            cts.Dispose();
        }
    }
}
