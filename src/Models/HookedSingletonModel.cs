using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Model registered once and instantiated separately for each run, including loaded runs.
    ///         Register its concrete type with <see cref="Content.ModContentRegistry.RegisterSingleton{TSingleton}" />.
    ///         Canonical models are definitions, not hook listeners. Instance state is available through
    ///         <see cref="CurrentRunState" /> and <see cref="CurrentCombatState" />.
    ///         Instances use the native mutable-clone lifecycle. Override <see cref="AbstractModel.DeepCloneFields" />
    ///         to recreate mutable reference fields; persist and restore saved data separately.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册一次、每局（包括读档）分别实例化的模型。
    ///         应通过 <see cref="Content.ModContentRegistry.RegisterSingleton{TSingleton}" /> 注册具体类型。
    ///         规范模型只作为定义，不接收钩子；实例状态通过 <see cref="CurrentRunState" /> 和
    ///         <see cref="CurrentCombatState" /> 提供。
    ///         实例遵循原版可变克隆生命周期；可变引用字段应在
    ///         <see cref="AbstractModel.DeepCloneFields" /> 中重新创建，存档数据需单独保存和恢复。
    ///     </para>
    /// </summary>
#if STS2_AT_LEAST_0_113_0
    public abstract class HookedSingletonModel : RunModel
#else
    public abstract class HookedSingletonModel : SingletonModel
#endif
    {
        /// <summary>
        ///     <para xml:lang="en">Selects a legacy hook stream on STS2 0.111. STS2 0.113 uses native run-model hooks.</para>
        ///     <para xml:lang="zh-CN">选择 STS2 0.111 的旧版钩子流；0.113 使用原生局内模型钩子。</para>
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
        ///     <para xml:lang="en">Selects the hook stream without subscribing or binding a run.</para>
        ///     <para xml:lang="zh-CN">选择钩子流；构造时不订阅钩子，也不绑定对局。</para>
        /// </summary>
        /// <param name="hookType">
        ///     <para xml:lang="en">The legacy hook stream. Ignored on STS2 0.113, where all native model hooks apply.</para>
        ///     <para xml:lang="zh-CN">旧版钩子流；STS2 0.113 忽略此选项，直接参与全部原生模型钩子。</para>
        /// </param>
        protected HookedSingletonModel(HookType hookType)
        {
            if (!Enum.IsDefined(hookType))
                throw new ArgumentOutOfRangeException(nameof(hookType), hookType, null);

#if !STS2_AT_LEAST_0_113_0
            SelectedHookType = hookType;
#endif
        }

#if !STS2_AT_LEAST_0_113_0
        /// <inheritdoc />
        public override bool ShouldReceiveCombatHooks => SelectedHookType == HookType.Combat;
        internal HookType SelectedHookType { get; }
#endif

        /// <summary>
        ///     <para xml:lang="en">Gets the owning run, or null for a canonical or released instance.</para>
        ///     <para xml:lang="zh-CN">获取所属对局；规范实例或已释放实例返回 null。</para>
        /// </summary>
#if STS2_AT_LEAST_0_113_0
        protected IRunState? CurrentRunState => IsCanonical ? null : RunState;
#else
        protected IRunState? CurrentRunState { get; private set; }
#endif

        /// <summary>
        ///     <para xml:lang="en">Gets the current combat, or null outside combat.</para>
        ///     <para xml:lang="zh-CN">获取当前战斗；不在战斗中时返回 null。</para>
        /// </summary>
#if STS2_AT_LEAST_0_113_0
        protected CombatState? CurrentCombatState => CombatState;
#else
        protected CombatState? CurrentCombatState { get; private set; }
#endif

#if !STS2_AT_LEAST_0_113_0
        /// <summary>
        ///     <para xml:lang="en">Called once after a new run is initialized. Not called for loaded runs.</para>
        ///     <para xml:lang="zh-CN">新局初始化后调用一次；读档时不调用。</para>
        /// </summary>
        public virtual void AfterRunCreated()
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Called once after a saved run is initialized. Restore persisted data explicitly.</para>
        ///     <para xml:lang="zh-CN">存档对局初始化后调用一次；持久数据需显式恢复。</para>
        /// </summary>
        public virtual void AfterRunLoaded()
        {
        }
#endif

#if !STS2_AT_LEAST_0_113_0
        internal void BindRun(RunState runState)
        {
            CurrentRunState = runState;
        }
#endif

        internal void BindCombat(CombatState? combatState)
        {
#if STS2_AT_LEAST_0_113_0
            if (combatState == null)
                OnCombatFinished();
#else
            CurrentCombatState = combatState;
#endif
        }

        internal void ReleaseRun()
        {
            BindCombat(null);
#if STS2_AT_LEAST_0_113_0
            RunState = null!;
#else
            CurrentRunState = null;
#endif
        }
    }
}
