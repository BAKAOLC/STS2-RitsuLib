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
        /// </summary>
        /// <param name="receiveCombatHooks">When true, subscribes the singleton to combat hook callbacks.</param>
        /// <param name="receiveRunHooks">When true, subscribes the singleton to run hook callbacks.</param>
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

        private IEnumerable<AbstractModel> RunSubModels(RunState runState)
        {
            return [this];
        }

        private IEnumerable<AbstractModel> CombatSubModels(CombatState combatState)
        {
            return [this];
        }
    }
}
