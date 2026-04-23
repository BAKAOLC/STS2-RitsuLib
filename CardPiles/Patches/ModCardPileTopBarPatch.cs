using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Injects <see cref="ModCardPileUiStyle.TopBarDeck" /> style buttons into <see cref="NTopBar" />
    ///     after its vanilla <c>_Ready</c> has resolved the built-in <c>%Deck</c> / <c>%Map</c> references.
    /// </summary>
    public sealed class ModCardPileTopBarReadyPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_top_bar_ready_mod_inject";

        /// <inheritdoc />
        public static string Description => "Inject mod TopBarDeck pile buttons into NTopBar";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTopBar), nameof(NTopBar._Ready))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>Wires mod top-bar buttons after vanilla resolves its built-in children.</summary>
        public static void Postfix(NTopBar __instance)
        {
            ModCardPileInjector.InjectTopBarButtons(__instance);
        }
        // ReSharper restore InconsistentNaming
    }

    /// <summary>
    ///     Forwards <see cref="NTopBar.Initialize" /> so mod TopBarDeck buttons bind to the local
    ///     <see cref="Player" /> alongside vanilla <c>Deck.Initialize(player)</c>.
    /// </summary>
    public sealed class ModCardPileTopBarInitializePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_top_bar_initialize_mod_bind";

        /// <inheritdoc />
        public static string Description =>
            "Bind mod TopBarDeck pile buttons to the local player on NTopBar.Initialize";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NTopBar), nameof(NTopBar.Initialize), [typeof(IRunState)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>Binds each injected TopBar mod button to the local <see cref="Player" />.</summary>
        public static void Postfix(NTopBar __instance, IRunState runState)
        {
            var player = LocalContext.GetMe(runState);
            if (player == null)
                return;
            var container = TopBar.ModTopBarLayout.GetRightAlignedContainer(__instance);
            if (container == null)
                return;
            foreach (var button in container.GetChildren().OfType<NModCardPileButton>())
                if (!button.IsActionMode)
                    button.Initialize(player);
        }
        // ReSharper restore InconsistentNaming
    }
}
