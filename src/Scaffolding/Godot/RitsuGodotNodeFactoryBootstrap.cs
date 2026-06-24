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
        ///     Idempotent; invoked during framework bootstrap so factories exist before runtime asset hooks run.
        ///     幂等；在框架 bootstrap 期间调用，确保工厂在运行时资源 hook 执行前存在。
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
            _ = new RitsuNCardTrailVfxNodeFactory();
            RitsuLibFramework.Logger.Info("[Godot] RitsuGodot node factories initialized.");
        }
    }
}
