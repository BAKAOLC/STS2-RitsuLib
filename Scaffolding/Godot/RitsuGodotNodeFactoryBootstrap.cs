using STS2RitsuLib.Scaffolding.Godot.NodeFactories;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     Registers built-in <see cref="RitsuGodotNodeFactory{T}" /> instances once per process (for explicit
    ///     <see cref="RitsuGodotNodeFactories" /> calls only).
    /// </summary>
    internal static class RitsuGodotNodeFactoryBootstrap
    {
        private static int _initialized;

        /// <summary>
        ///     Idempotent; invoked during content-asset patch registration so factories exist before mods run.
        /// </summary>
        internal static void EnsureRegistered()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            _ = new RitsuNCreatureVisualsNodeFactory();
            _ = new RitsuNMerchantCharacterNodeFactory();
            _ = new RitsuNRestSiteCharacterNodeFactory();
            _ = new RitsuNode2DSceneRootFactory();
            _ = new RitsuTextureRectControlNodeFactory();
            _ = new RitsuNEnergyCounterNodeFactory();
            RitsuLibFramework.Logger.Info("[Godot] RitsuGodot node factories initialized.");
        }
    }
}
