using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.TopBar;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Injects <see cref="ModCardPileUiStyle.TopBarDeck" /> style buttons into <see cref="NTopBar" />
    ///     after its vanilla <c>_Ready</c> has resolved the built-in <c>%Deck</c> / <c>%Map</c> references.
    ///     在原版 <c>_Ready</c> 解析内置 <c>%Deck</c> / <c>%Map</c> 引用后，将
    ///     <see cref="ModCardPileUiStyle.TopBarDeck" /> 样式按钮注入 <see cref="NTopBar" />。
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
    ///     Forwards <see cref="NTopBar.Initialize" /> so mod TopBarDeck buttons bind to the local
    ///     <see cref="Player" /> alongside vanilla <c>Deck.Initialize(player)</c>.
    ///     转发 <see cref="NTopBar.Initialize" />，使 mod TopBarDeck 按钮与原版
    ///     <c>Deck.Initialize(player)</c> 一起绑定到本地 <see cref="Player" />。
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
