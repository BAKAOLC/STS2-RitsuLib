using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     Convenience <see cref="SingletonModel" /> base type that can self-subscribe to run and combat hooks.
    ///     This avoids repeating reflection-based hook registration boilerplate in each singleton model.
    ///     便捷的 <see cref="SingletonModel" /> 基类型，可自行订阅跑局和战斗 hook。
    ///     这避免了在每个单例模型中重复基于反射的 hook 注册样板代码。
    /// </summary>
    public abstract class HookedSingletonModel : SingletonModel
    {
        private static readonly MethodInfo? SubscribeCombat =
            typeof(ModHelper).GetMethod("SubscribeForCombatStateHooks");

        private static readonly MethodInfo? SubscribeRunState =
            typeof(ModHelper).GetMethod("SubscribeForRunStateHooks");

        private static readonly Type? RunHookSubscriptionDelegateType =
            Type.GetType("MegaCrit.Sts2.Core.Modding.RunHookSubscriptionDelegate, sts2");

        private static readonly Type? CombatHookSubscriptionDelegateType =
            Type.GetType("MegaCrit.Sts2.Core.Modding.CombatHookSubscriptionDelegate, sts2");

        /// <summary>
        ///     Creates the singleton instance and optionally subscribes it to the corresponding hook streams.
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
        protected HookedSingletonModel(bool receiveCombatHooks, bool receiveRunHooks)
        {
            ShouldReceiveCombatHooks = receiveCombatHooks;

            if (!receiveCombatHooks && !receiveRunHooks)
                return;

            if (SubscribeCombat == null || SubscribeRunState == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[HookedSingletonModel] Singleton created but hook subscription is unavailable on this game branch: {GetType().FullName}");
                return;
            }

            if ((receiveRunHooks && RunHookSubscriptionDelegateType == null) ||
                (receiveCombatHooks && CombatHookSubscriptionDelegateType == null))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[HookedSingletonModel] Singleton created but hook subscription delegate type is unavailable on this game branch: {GetType().FullName}");
                return;
            }

            if (receiveRunHooks)
                SubscribeRunState.Invoke(null,
                [
                    Id.Entry,
                    Delegate.CreateDelegate(
                        RunHookSubscriptionDelegateType!,
                        this,
                        GetType().GetMethod(nameof(RunSubModels), BindingFlags.Instance | BindingFlags.NonPublic)!
                    ),
                ]);

            if (receiveCombatHooks)
                SubscribeCombat.Invoke(null,
                [
                    Id.Entry,
                    Delegate.CreateDelegate(
                        CombatHookSubscriptionDelegateType!,
                        this,
                        GetType().GetMethod(nameof(CombatSubModels), BindingFlags.Instance | BindingFlags.NonPublic)!
                    ),
                ]);
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
        protected IEnumerable<AbstractModel> RunSubModels(RunState runState)
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
        protected IEnumerable<AbstractModel> CombatSubModels(CombatState combatState)
        {
            return [this];
        }
    }
}
