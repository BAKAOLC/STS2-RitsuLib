using STS2RitsuLib.Scaffolding.Godot.NodeFactories;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers the built-in <see cref="RitsuGodotNodeFactory{T}" /> instances once per process for explicit
    ///         <see cref="RitsuGodotNodeFactories" /> calls.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         每个进程注册一次内置 <see cref="RitsuGodotNodeFactory{T}" /> 实例，供显式调用
    ///         <see cref="RitsuGodotNodeFactories" /> 时使用。
    ///     </para>
    /// </summary>
    internal static class RitsuGodotNodeFactoryBootstrap
    {
        private static readonly object SyncRoot = new();
        private static bool _initialized;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Ensures the factories are registered before runtime asset hooks run. Repeated calls have no effect.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确保在运行时资源钩子执行前注册所有工厂；重复调用不会产生额外效果。
        ///     </para>
        /// </summary>
        internal static void EnsureRegistered()
        {
            lock (SyncRoot)
            {
                if (_initialized)
                    return;

                _ = new RitsuNCreatureVisualsNodeFactory();
                _ = new RitsuNMerchantCharacterNodeFactory();
                _ = new RitsuNRestSiteCharacterNodeFactory();
                _ = new RitsuNode2DSceneRootFactory();
                _ = new RitsuTextureRectControlNodeFactory();
                _ = new RitsuNEnergyCounterNodeFactory();
                _ = new RitsuNCardTrailVfxNodeFactory();
                _initialized = true;
                RitsuLibFramework.Logger.Info("[Godot] RitsuGodot node factories initialized.");
            }
        }
    }
}
