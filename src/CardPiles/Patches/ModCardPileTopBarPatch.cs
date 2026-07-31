using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.TopBar;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Injects <see cref="ModCardPileUiStyle.TopBarDeck" /> pile buttons after
    ///         <see cref="NTopBar" /> becomes ready.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="NTopBar" /> 就绪后注入 <see cref="ModCardPileUiStyle.TopBarDeck" /> 牌堆按钮。
    ///     </para>
    /// </summary>
    internal sealed class ModCardPileTopBarReadyPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_top_bar_ready_mod_inject";
        public static string Description => "Inject mod TopBarDeck pile buttons into NTopBar";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTopBar), nameof(NTopBar._Ready))];
        }

        public static void Postfix(NTopBar __instance)
        {
            ModCardPileInjector.InjectTopBarButtons(__instance);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Binds injected top-bar pile buttons to the local player during
    ///         <see cref="NTopBar.Initialize" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="NTopBar.Initialize" /> 期间将已注入的顶部栏牌堆按钮绑定到本地玩家。
    ///     </para>
    /// </summary>
    internal sealed class ModCardPileTopBarInitializePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_top_bar_initialize_mod_bind";

        public static string Description =>
            "Bind mod TopBarDeck pile buttons to the local player on NTopBar.Initialize";

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
                if (!button.IsActionMode)
                    button.Initialize(player);
        }
    }
}
