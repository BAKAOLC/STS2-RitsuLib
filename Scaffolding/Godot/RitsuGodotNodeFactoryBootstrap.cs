using STS2RitsuLib.Scaffolding.Godot.NodeFactories;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     Registers built-in <see cref="RitsuGodotNodeFactory{T}" /> instances once per process (for explicit
    ///     <see cref="RitsuGodotNodeFactories" /> calls only).
    ///     每个进程只注册一次内置 <see cref="RitsuGodotNodeFactory{T}" /> 实例（仅用于显式
    ///     <see cref="RitsuGodotNodeFactories" /> 调用）。
    /// </summary>
    internal static class RitsuGodotNodeFactoryBootstrap
    {
        private static int _initialized;

        /// <summary>
        ///     Idempotent; invoked during content-asset patch registration so factories exist before mods run.
        ///     幂等；在 content-asset 补丁注册期间调用，确保工厂在 mod 运行前存在。
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
