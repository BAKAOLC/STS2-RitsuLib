using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     Convenience <see cref="SingletonModel" /> base type that can self-subscribe to run and combat hooks.
    ///     This avoids repeating reflection-based hook registration boilerplate in each singleton model.
    ///     便捷的 <see cref="SingletonModel" /> 基类型，可自行订阅跑局或战斗 hook。
    ///     这避免了在每个单例模型中重复 hook 注册样板代码。
    /// </summary>
    public abstract class HookedSingletonModel : SingletonModel
    {
        /// <summary>
        ///     Hook stream selected for a singleton model.
        ///     单例模型选择订阅的 hook 流。
        /// </summary>
        public enum HookType
        {
            /// <summary>
            ///     Do not subscribe the singleton to run or combat hook streams.
            ///     不将单例订阅到跑局或战斗 hook 流。
            /// </summary>
            None,

            /// <summary>
            ///     Subscribe the singleton to combat-state hooks.
            ///     将单例订阅到战斗状态 hook。
            /// </summary>
            Combat,

            /// <summary>
            ///     Subscribe the singleton to run-state hooks.
            ///     将单例订阅到跑局状态 hook。
            /// </summary>
            Run,
        }

        /// <summary>
        ///     Creates the singleton instance and subscribes it to one hook stream.
        ///     创建单例实例，并将其订阅到一个 hook 流。
        /// </summary>
        /// <param name="hookType">
        ///     The hook stream to subscribe to.
        ///     要订阅的 hook 流。
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

        /// <summary>
        ///     Creates the singleton instance and optionally subscribes it to the corresponding hook stream.
        ///     创建单例实例，并可选地将其订阅到对应的 hook 流。
        /// </summary>
        /// <param name="receiveCombatHooks">
        ///     When true, subscribes the singleton to combat hook callbacks.
        ///     为 true 时，将单例订阅到战斗 hook 回调。
        /// </param>
        /// <param name="receiveRunHooks">
        ///     When true, subscribes the singleton to run hook callbacks.
        ///     为 true 时，将单例订阅到跑局 hook 回调。
        /// </param>
        [Obsolete(
            "Use the constructor receiving a HookType instead. A singleton receiving both types of hooks will receive some hooks twice.")]
        protected HookedSingletonModel(bool receiveCombatHooks, bool receiveRunHooks)
            : this(receiveCombatHooks ? HookType.Combat : receiveRunHooks ? HookType.Run : HookType.None)
        {
        }

        /// <inheritdoc />
        public override bool ShouldReceiveCombatHooks { get; }

        /// <summary>
        ///     Provides the run-scoped sub-models that should receive run-state hook callbacks for this singleton.
        ///     提供应为此单例接收跑局状态 hook 回调的跑局作用域子模型。
        /// </summary>
        /// <param name="runState">
        ///     The current run state.
        ///     当前跑局状态。
        /// </param>
        /// <returns>
        ///     The models to subscribe for run hooks.
        ///     要订阅跑局 hook 的模型。
        /// </returns>
        private IEnumerable<AbstractModel> RunSubModels(RunState runState)
        {
            return [this];
        }

        /// <summary>
        ///     Provides the combat-scoped sub-models that should receive combat-state hook callbacks for this singleton.
        ///     提供应为此单例接收战斗状态 hook 回调的战斗作用域子模型。
        /// </summary>
        /// <param name="combatState">
        ///     The current combat state.
        ///     当前战斗状态。
        /// </param>
        /// <returns>
        ///     The models to subscribe for combat hooks.
        ///     要订阅战斗 hook 的模型。
        /// </returns>
        private IEnumerable<AbstractModel> CombatSubModels(CombatState combatState)
        {
            return [this];
        }
    }
}
