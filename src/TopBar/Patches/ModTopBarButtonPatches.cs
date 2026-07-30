using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.TopBar.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Creates registered action buttons after the vanilla deck slot becomes available in
    ///         <see cref="NTopBar._Ready" />, then places them in their defined sort order.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         原版牌组槽位在 <see cref="NTopBar._Ready" /> 中可用后，创建已注册的操作按钮，并按规定的排序值放置。
    ///     </para>
    /// </summary>
    internal sealed class ModTopBarActionButtonReadyPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_top_bar_ready_action_inject";
        public static string Description => "Inject mod action buttons into NTopBar";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTopBar), nameof(NTopBar._Ready))];
        }

        public static void Postfix(NTopBar __instance)
        {
            var definitions = ModTopBarButtonRegistry.GetDefinitionsSnapshot();
            if (definitions.Length == 0)
                return;

            for (var i = definitions.Length - 1; i >= 0; i--)
            {
                var definition = definitions[i];
                var button = NModCardPileButton.CreateAction(definition);
                __instance.AddChildSafely(button);
                ModTopBarLayout.Place(__instance, button, definition.Offset);
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Binds injected action buttons to the local <see cref="Player" /> after
    ///         <see cref="NTopBar.Initialize" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="NTopBar.Initialize" /> 后，将已注入的操作按钮绑定到本地
    ///         <see cref="Player" />。
    ///     </para>
    /// </summary>
    internal sealed class ModTopBarActionButtonInitializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_top_bar_initialize_action_bind";

        public static string Description =>
            "Bind mod action buttons to the local player on NTopBar.Initialize";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NTopBar), nameof(NTopBar.Initialize), [typeof(IRunState)]),
            ];
        }

        public static void Postfix(NTopBar __instance, IRunState runState)
        {
            var player = LocalContext.GetMe(runState);
            if (player == null)
                return;
            var container = ModTopBarLayout.GetRightAlignedContainer(__instance);
            if (container == null)
                return;
            foreach (var button in container.GetChildren().OfType<NModCardPileButton>())
                if (button.IsActionMode)
                    button.Initialize(player);
        }
    }
}
