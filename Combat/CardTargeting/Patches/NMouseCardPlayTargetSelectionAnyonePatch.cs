using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Routes mouse targeting to <c>SingleCreatureTargeting</c> for <see cref="CustomTargetType.Anyone" />,
    ///     enabling selection of any living creature across faction boundaries.
    /// </summary>
    internal sealed class NMouseCardPlayTargetSelectionAnyonePatch : IPatchMethod
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

        private static readonly MethodInfo SingleCreatureTargetingMethod =
            AccessTools.DeclaredMethod(typeof(NMouseCardPlay), "SingleCreatureTargeting",
                [typeof(TargetMode), typeof(TargetType)]);

        public static string PatchId => "card_anyone_mouse_target_selection";

        public static string Description =>
            "Route Anyone cards to SingleCreatureTargeting in NMouseCardPlay";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMouseCardPlay), "TargetSelection", [typeof(TargetMode)])];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(NMouseCardPlay __instance, TargetMode targetMode, ref Task __result)
            // ReSharper restore InconsistentNaming
        {
            var card = GetCard(__instance);
            if (card == null || card.TargetType != CustomTargetType.Anyone)
                return true;

            __result = RunAnyoneTargeting(__instance, targetMode);
            return false;
        }

        private static async Task RunAnyoneTargeting(NMouseCardPlay instance, TargetMode targetMode)
        {
            var cardNode = GetCardNode(instance);
            if (cardNode == null)
                return;

            TryShowEvokingOrbs(instance);
            cardNode.CardHighlight.AnimFlash();
            await (Task)SingleCreatureTargetingMethod.Invoke(instance, [targetMode, CustomTargetType.Anyone])!;
        }
    }
}
