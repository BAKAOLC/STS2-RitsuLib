using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    internal sealed class CardVisualRefreshPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<NCard, CardVisualState> States = new();

        public static string PatchId => "content_asset_override_card_visual_refresh";

        public static string Description =>
            "Apply card artwork consistently after full and incremental visual refreshes";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCard), "Reload"),
                new(typeof(NCard), "ReloadOverlay"),
#if STS2_AT_LEAST_0_108_0
                new(typeof(NCard), "UpdatePortrait"),
#endif
            ];
        }

        public static bool Prefix(NCard __instance, out CardVisualState? __state)
        {
            __state = null;
            if (!GodotObject.IsInstanceValid(__instance) || !__instance.IsNodeReady())
                return true;
            var state = States.GetValue(__instance, static card => new(card));
            if (!state.Enter())
                return false;
            __state = state;
            return true;
        }

        public static void Postfix(ref CardVisualState? __state)
        {
            var state = __state;
            __state = null;
            state?.Leave(true);
        }

        public static void Finalizer(CardVisualState? __state)
        {
            __state?.Leave(false);
        }

        internal static void Reset(NCard card)
        {
            if (!States.TryGetValue(card, out var state))
                return;
            state.Reset();
            States.Remove(card);
        }
    }

    internal sealed class CardVisualPoolResetPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_card_visual_pool_reset";
        public static string Description => "Release card artwork overrides before pooled card views are reused";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCard), nameof(NCard.OnFreedToPool)),
                new(typeof(NCard), nameof(NCard.OnReturnedFromPool)),
            ];
        }

        public static void Prefix(NCard __instance)
        {
            CardVisualRefreshPatch.Reset(__instance);
        }
    }
}
