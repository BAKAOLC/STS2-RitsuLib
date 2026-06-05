using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Injects <see cref="ModCardPileUiStyle.ExtraHand" /> containers (<see cref="Nodes.NModExtraHand" />)
    ///     into <see cref="NCombatUi" /> on ready so they live alongside the vanilla player hand.
    ///     在 ready 时将 <see cref="ModCardPileUiStyle.ExtraHand" /> 容器（<see cref="Nodes.NModExtraHand" />）
    ///     注入 <see cref="NCombatUi" />，使它们与原版玩家 hand 并存。
    /// </summary>
    public sealed class ModCardPileCombatUiReadyPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_combat_ui_ready_extra_hand";

        /// <inheritdoc />
        public static string Description => "Inject ExtraHand mod pile containers into NCombatUi";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatUi), nameof(NCombatUi._Ready))];
        }

        /// <summary>
        ///     Wires up ExtraHand containers after vanilla resolves its child references.
        ///     在原版解析完子节点引用后接入 ExtraHand 容器。
        /// </summary>
        public static void Postfix(NCombatUi __instance)
        {
            ModCardPileInjector.InjectExtraHandContainers(__instance);
        }
    }

    /// <summary>
    ///     Activates ExtraHand containers with the current combat state so they bind to the local player
    ///     and begin listening to <c>CardPile.CardAdded</c> / <c>CardPile.CardRemoved</c>.
    ///     <c>CardPile.CardAdded</c> / <c>CardPile.CardRemoved</c>。
    ///     使用当前战斗状态激活 ExtraHand 容器，使它们绑定到本地玩家，并开始监听
    ///     <c>CardPile.CardAdded</c>
    ///     <c>CardPile.CardRemoved</c>。
    ///     <c>CardPile.CardAdded</c>
    ///     <c>CardPile.CardRemoved</c>。
    /// </summary>
    public sealed class ModCardPileCombatUiActivatePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_combat_ui_activate_extra_hand";

        /// <inheritdoc />
        public static string Description => "Activate ExtraHand mod pile containers alongside NCombatUi.Activate";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatUi), nameof(NCombatUi.Activate), [typeof(CombatState)])];
        }

        /// <summary>
        ///     Binds each injected ExtraHand container to the local player.
        ///     将每个已注入的 ExtraHand 容器绑定到本地玩家。
        /// </summary>
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
