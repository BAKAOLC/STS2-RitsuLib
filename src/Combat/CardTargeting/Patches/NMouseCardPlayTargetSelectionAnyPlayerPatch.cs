using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes <see cref="TargetType.AnyPlayer" /> through mouse single-creature selection. Vanilla routes it
    ///         through multi-creature targeting, which neither shows an arrow nor selects a target.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="TargetType.AnyPlayer" /> 路由到鼠标单体选目标流程。原版会将其路由到群体目标流程，
    ///         因而既不显示箭头，也不会选择目标。
    ///     </para>
    /// </summary>
    internal sealed class NMouseCardPlayTargetSelectionAnyPlayerPatch : IPatchMethod
    {
        private static readonly Func<NCardPlay, CardModel?> GetCard =
            AccessTools.MethodDelegate<Func<NCardPlay, CardModel?>>(
                AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "Card"));

        private static readonly Func<NCardPlay, NCard?> GetCardNode =
            AccessTools.MethodDelegate<Func<NCardPlay, NCard?>>(
                AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "CardNode"));

        private static readonly Action<NCardPlay> TryShowEvokingOrbs =
            AccessTools.MethodDelegate<Action<NCardPlay>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "TryShowEvokingOrbs"));

        private static readonly Func<NMouseCardPlay, TargetMode, TargetType, Task> SingleCreatureTargeting =
            AccessTools.MethodDelegate<Func<NMouseCardPlay, TargetMode, TargetType, Task>>(
                AccessTools.DeclaredMethod(typeof(NMouseCardPlay), "SingleCreatureTargeting",
                    [typeof(TargetMode), typeof(TargetType)]));

        public static string PatchId => "card_any_player_mouse_target_selection";

        public static string Description =>
            "Route AnyPlayer cards to SingleCreatureTargeting in NMouseCardPlay";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMouseCardPlay), "TargetSelection", [typeof(TargetMode)])];
        }

        public static bool Prefix(NMouseCardPlay __instance, TargetMode targetMode, ref Task __result)
        {
            var card = GetCard(__instance);
            if (!AnyPlayerCardTargetingHelper.IsAnyPlayerMultiplayer(card))
                return true;

            __result = RunAnyPlayerTargeting(__instance, targetMode);
            return false;
        }

        private static async Task RunAnyPlayerTargeting(NMouseCardPlay instance, TargetMode targetMode)
        {
            var cardNode = GetCardNode(instance);
            if (cardNode == null) return;

            TryShowEvokingOrbs(instance);
            cardNode.CardHighlight.AnimFlash();
            await SingleCreatureTargeting(instance, targetMode, TargetType.AnyPlayer);
        }
    }
}
