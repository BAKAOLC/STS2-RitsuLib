using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Injects <see cref="ModCardPileUiStyle.ExtraHand" /> containers after
    ///         <see cref="NCombatUi" /> becomes ready.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="NCombatUi" /> 就绪后注入 <see cref="ModCardPileUiStyle.ExtraHand" /> 容器。
    ///     </para>
    /// </summary>
    internal sealed class ModCardPileCombatUiReadyPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_combat_ui_ready_extra_hand";
        public static string Description => "Inject ExtraHand mod pile containers into NCombatUi";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatUi), nameof(NCombatUi._Ready))];
        }

        public static void Postfix(NCombatUi __instance)
        {
            ModCardPileInjector.InjectExtraHandContainers(__instance);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Binds injected extra-hand containers to the local player when the combat UI is activated.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         战斗界面激活时，将已注入的额外手牌容器绑定到本地玩家。
    ///     </para>
    /// </summary>
    internal sealed class ModCardPileCombatUiActivatePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_combat_ui_activate_extra_hand";
        public static string Description => "Activate ExtraHand mod pile containers alongside NCombatUi.Activate";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatUi), nameof(NCombatUi.Activate), [typeof(CombatState)])];
        }

        public static void Postfix(NCombatUi __instance, CombatState state)
        {
            var me = LocalContext.GetMe(state);
            if (me == null)
                return;
            foreach (var hand in __instance.GetChildren().OfType<NModExtraHand>())
                hand.Initialize(me);
        }
    }
}
