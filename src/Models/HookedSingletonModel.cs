using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Convenience <see cref="SingletonModel" /> base type that can subscribe itself to run or combat hooks,
    ///         avoiding repeated reflection-based hook registration in each singleton model.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可自行订阅一局游戏或战斗钩子的便捷 <see cref="SingletonModel" /> 基类型，
    ///         避免在每个单例模型中重复编写基于反射的钩子注册代码。
    ///     </para>
    /// </summary>
    public abstract class HookedSingletonModel : SingletonModel
    {
        /// <summary>
        ///     <para xml:lang="en">Specifies the hook stream selected for a singleton model.</para>
        ///     <para xml:lang="zh-CN">指定单例模型要订阅的钩子流。</para>
        /// </summary>
        public enum HookType
        {
            /// <summary>
            ///     <para xml:lang="en">Does not subscribe the singleton to run or combat hooks.</para>
            ///     <para xml:lang="zh-CN">不将单例订阅到一局游戏或战斗钩子。</para>
            /// </summary>
            None,

            /// <summary>
            ///     <para xml:lang="en">Subscribes the singleton to combat-state hooks.</para>
            ///     <para xml:lang="zh-CN">将单例订阅到战斗状态钩子。</para>
            /// </summary>
            Combat,

            /// <summary>
            ///     <para xml:lang="en">Subscribes the singleton to run-state hooks.</para>
            ///     <para xml:lang="zh-CN">将单例订阅到局内状态钩子。</para>
            /// </summary>
            Run,
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the singleton and subscribes it to one hook stream.</para>
        ///     <para xml:lang="zh-CN">创建单例，并将其订阅到一个钩子流。</para>
        /// </summary>
        /// <param name="hookType">
        ///     <para xml:lang="en">The hook stream to subscribe to.</para>
        ///     <para xml:lang="zh-CN">要订阅的钩子流。</para>
        /// </param>
        protected HookedSingletonModel(HookType hookType)
        {
            switch (hookType)
            {
                case HookType.None:
                    break;
                case HookType.Combat:
                    ShouldReceiveCombatHooks = true;
                    ModHelper.SubscribeForCombatStateHooks(Id.Entry, CombatSubModels);
                    break;
                case HookType.Run:
                    ModHelper.SubscribeForRunStateHooks(Id.Entry, RunSubModels);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hookType), hookType, null);
            }
        }

        /// <inheritdoc />
        public override bool ShouldReceiveCombatHooks { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the run state most recently supplied by a hook subscription callback.</para>
        ///     <para xml:lang="zh-CN">获取钩子订阅回调最近提供的局内状态。</para>
        /// </summary>
        protected IRunState? CurrentRunState { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets the combat state most recently supplied by a hook subscription callback.</para>
        ///     <para xml:lang="zh-CN">获取钩子订阅回调最近提供的战斗状态。</para>
        /// </summary>
        protected CombatState? CurrentCombatState { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Updates the current run state and returns this singleton as the run-scoped hook model.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         更新当前局内状态，并将此单例作为局内作用域的钩子模型返回。
        ///     </para>
        /// </summary>
        /// <param name="runState">
        ///     <para xml:lang="en">The current run state.</para>
        ///     <para xml:lang="zh-CN">当前局内状态。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The models to subscribe for run hooks.</para>
        ///     <para xml:lang="zh-CN">要订阅局内钩子的模型。</para>
        /// </returns>
        private IEnumerable<AbstractModel> RunSubModels(RunState runState)
        {
            CurrentRunState = runState;
            CurrentCombatState = null;
            return [this];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Updates the current combat and run states and returns this singleton as the combat-scoped hook model.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         更新当前战斗与局内状态，并将此单例作为战斗作用域的钩子模型返回。
        ///     </para>
        /// </summary>
        /// <param name="combatState">
        ///     <para xml:lang="en">The current combat state.</para>
        ///     <para xml:lang="zh-CN">当前战斗状态。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The models to subscribe for combat hooks.</para>
        ///     <para xml:lang="zh-CN">要订阅战斗钩子的模型。</para>
        /// </returns>
        private IEnumerable<AbstractModel> CombatSubModels(CombatState combatState)
        {
            CurrentCombatState = combatState;
            CurrentRunState = combatState.RunState;
            return [this];
        }
    }
}
