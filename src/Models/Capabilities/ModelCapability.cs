using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.DynamicVars;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Base class for model-backed capabilities. Register subclasses as model capabilities when they need a
    ///         stable <see cref="ModelId" /> and persistence identity.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         基于模型的能力基类。子类需要稳定的 <see cref="ModelId" /> 和持久化身份时，应将其注册为模型能力。
    ///     </para>
    /// </summary>
    public abstract class ModelCapability : AbstractModel, IModelCapability, IModelCapabilityJsonState,
        IModelCapabilityCloneHandler, IModelDynamicVarContributor
    {
        private const string DynamicVarsStateKey = "dynamicVars";
        private const string AdditionalStateKey = "state";
        private DynamicVarSet? _dynamicVars;

        /// <inheritdoc />
        public override bool ShouldReceiveCombatHooks =>
            this is IModelCapabilityHookListener { ShouldReceiveOwnerHooks: true };

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets capability-owned dynamic variables used by localized text, gameplay commands, and card previews
        ///         when the owner is a card. This set is independent of the owner's dynamic variables; read owner
        ///         variables through <see cref="Owner" /> when capability behavior depends on them.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取能力自有的动态变量；这些变量可用于本地化文本、游戏指令，并在所属模型为卡牌时用于卡牌预览。
        ///         此集合独立于所属模型的动态变量；能力行为依赖所属模型变量时，应通过 <see cref="Owner" /> 读取。
        ///     </para>
        /// </summary>
        public DynamicVarSet DynamicVars
        {
            get
            {
                _dynamicVars ??= CreateDynamicVars();
                if (Owner != null)
                    _dynamicVars.InitializeWithOwner(Owner);

                return _dynamicVars;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the capability's canonical dynamic variables. Override this property to register variables
        ///         directly on the capability.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取能力的规范动态变量。重写此属性可直接在能力上注册变量。
        ///     </para>
        /// </summary>
        protected virtual IEnumerable<DynamicVar> CanonicalVars => [];

        /// <inheritdoc />
        public virtual string CapabilityId => ModelCapabilityRegistry.GetCapabilityId(GetType()) ?? Id.ToString();

        /// <inheritdoc />
        public AbstractModel? Owner { get; private set; }

        /// <inheritdoc />
        public virtual void Attach(AbstractModel owner, bool isInternal = false)
        {
            ArgumentNullException.ThrowIfNull(owner);
            Owner = owner;
            if (!isInternal)
                OnAttach(owner);
        }

        /// <inheritdoc />
        public virtual void Detach(bool isInternal = false)
        {
            var oldOwner = Owner;
            if (!isInternal && oldOwner != null)
                OnDetach(oldOwner);
            Owner = null;
        }

        /// <inheritdoc />
        public virtual IModelCapability CloneFor(AbstractModel clonedOwner)
        {
            var clone = (ModelCapability)MutableClone();
            clone.Owner = null;
            clone._dynamicVars = CloneDynamicVars(clonedOwner);
            clone.Attach(clonedOwner, true);
            return clone;
        }

        /// <inheritdoc />
        public JsonNode? SaveState()
        {
            var dynamicVarState = SaveDynamicVarState();
            var additionalState = SaveAdditionalState();
            if (dynamicVarState == null && additionalState == null)
                return null;

            var state = new JsonObject();
            if (dynamicVarState != null)
                state[DynamicVarsStateKey] = dynamicVarState;
            if (additionalState != null)
                state[AdditionalStateKey] = additionalState.DeepClone();

            return state;
        }

        /// <inheritdoc />
        public void LoadState(JsonNode? state, int schemaVersion)
        {
            if (state is not JsonObject obj)
            {
                LoadAdditionalState(null, schemaVersion);
                return;
            }

            LoadDynamicVarState(obj[DynamicVarsStateKey]);
            LoadAdditionalState(obj[AdditionalStateKey], schemaVersion);
        }

        /// <inheritdoc />
        public virtual string? LocStringVariableScope => null;

        DynamicVarSet IModelDynamicVarContributor.GetDynamicVars(AbstractModel model)
        {
            var dynamicVars = DynamicVars;
            dynamicVars.InitializeWithOwner(model);
            return dynamicVars;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Invokes <paramref name="modifier" /> and marks the owning capability set dirty even if the callback
        ///         throws.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         调用 <paramref name="modifier" />；即使回调抛出异常，也会将所属能力集合标记为脏。
        ///     </para>
        /// </summary>
        /// <param name="modifier">
        ///     <para xml:lang="en">
        ///         The mutation to apply. External callers can use it to update capability-owned
        ///         <see cref="DynamicVars" /> without separately marking the owner dirty.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         要应用的修改。外部调用方可借此更新能力自有的 <see cref="DynamicVars" />，无需另行将所属模型标记为脏。
        ///     </para>
        /// </param>
        public void Modify(Action<ModelCapability> modifier)
        {
            ArgumentNullException.ThrowIfNull(modifier);
            try
            {
                modifier(this);
            }
            finally
            {
                MarkDirty();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Marks the owning capability set dirty after an in-place state change.</para>
        ///     <para xml:lang="zh-CN">在原地修改状态后将所属能力集合标记为脏。</para>
        /// </summary>
        protected void MarkDirty()
        {
            if (Owner != null)
                ModelCapabilities.MarkDirty(Owner);
        }

        /// <summary>
        ///     <para xml:lang="en">Saves capability state in addition to its dynamic variables.</para>
        ///     <para xml:lang="zh-CN">保存能力动态变量之外的额外状态。</para>
        /// </summary>
        protected virtual JsonNode? SaveAdditionalState()
        {
            return null;
        }

        /// <summary>
        ///     <para xml:lang="en">Loads capability state in addition to its dynamic variables.</para>
        ///     <para xml:lang="zh-CN">加载能力动态变量之外的额外状态。</para>
        /// </summary>
        protected virtual void LoadAdditionalState(JsonNode? state, int schemaVersion)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Resets capability-owned dynamic variables to their canonical definitions.</para>
        ///     <para xml:lang="zh-CN">将能力自有的动态变量重置为规范定义。</para>
        /// </summary>
        protected void ResetDynamicVarsToCanonical()
        {
            _dynamicVars = CreateDynamicVars();
            MarkDirty();
        }

        internal void RecalculateDynamicVarsForUpgradeOrEnchant()
        {
            DynamicVars.RecalculateForUpgradeOrEnchant();
            MarkDirty();
        }

        internal void FinalizeDynamicVarUpgrade()
        {
            DynamicVars.FinalizeUpgrade();
        }

        internal void MarkDynamicVarsJustUpgraded()
        {
            foreach (var dynamicVar in DynamicVars.Values)
                dynamicVar.UpgradeValueBy(0m);
        }

        internal void NotifyLoadedFromSave()
        {
            OnLoadedFromSave();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes this capability from its owner's capability set when currently attached.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当前能力已附加时，将其从所属模型的能力集合中移除。
        ///     </para>
        /// </summary>
        public bool RemoveFromOwner()
        {
            var owner = Owner;
            return owner != null && ModelCapabilities.Get(owner).Remove(this);
        }

        /// <summary>
        ///     <para xml:lang="en">Called when this capability is attached.</para>
        ///     <para xml:lang="zh-CN">此能力被附加时调用。</para>
        /// </summary>
        protected virtual void OnAttach(AbstractModel owner)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Called when this capability is detached.</para>
        ///     <para xml:lang="zh-CN">此能力被分离时调用。</para>
        /// </summary>
        protected virtual void OnDetach(AbstractModel owner)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Called after this capability's owner is restored while loading a saved run.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         读取一局游戏的存档并恢复此能力的所属模型后调用。
        ///     </para>
        /// </summary>
        protected virtual void OnLoadedFromSave()
        {
        }

        private DynamicVarSet CreateDynamicVars()
        {
            var dynamicVars = new DynamicVarSet(CanonicalVars.Select(CloneDynamicVar));
            if (Owner != null)
                dynamicVars.InitializeWithOwner(Owner);

            return dynamicVars;
        }

        private JsonObject? SaveDynamicVarState()
        {
            var defaults = new DynamicVarSet(CanonicalVars.Select(CloneDynamicVar));
            var state = default(JsonObject);

            foreach (var dynamicVar in DynamicVars.Values)
            {
                defaults.TryGetValue(dynamicVar.Name, out var defaultVar);
                if (!TryCreateDynamicVarState(dynamicVar, defaultVar, out var value))
                    continue;

                state ??= new();
                state[dynamicVar.Name] = value;
            }

            return state;
        }

        private void LoadDynamicVarState(JsonNode? state)
        {
            if (state is not JsonObject obj)
                return;

            var dynamicVars = DynamicVars;
            foreach (var entry in obj)
            {
                if (entry.Value == null || !dynamicVars.TryGetValue(entry.Key, out var dynamicVar))
                    continue;

                LoadDynamicVarValue(dynamicVar, entry.Value);
            }
        }

        private DynamicVarSet? CloneDynamicVars(AbstractModel clonedOwner)
        {
            if (_dynamicVars == null)
                return null;

            var set = new DynamicVarSet(_dynamicVars.Values.Select(CloneDynamicVar));
            set.InitializeWithOwner(clonedOwner);
            return set;
        }

        private static DynamicVar CloneDynamicVar(DynamicVar dynamicVar)
        {
            var clone = dynamicVar.Clone();
            DynamicVarTooltipRegistry.CopyTo(dynamicVar, clone);
            return clone;
        }

        private static bool TryCreateDynamicVarState(
            DynamicVar dynamicVar,
            DynamicVar? defaultVar,
            out JsonNode? value)
        {
            if (dynamicVar is StringVar stringVar)
            {
                var current = stringVar.StringValue ?? "";
                var defaultValue = defaultVar is StringVar defaultString ? defaultString.StringValue ?? "" : "";
                if (string.Equals(current, defaultValue, StringComparison.Ordinal))
                {
                    value = null;
                    return false;
                }

                value = JsonValue.Create(current);
                return true;
            }

            if (dynamicVar.BaseValue == (defaultVar?.BaseValue ?? 0m))
            {
                value = null;
                return false;
            }

            value = JsonValue.Create(dynamicVar.BaseValue);
            return true;
        }

        private static void LoadDynamicVarValue(DynamicVar dynamicVar, JsonNode value)
        {
            if (dynamicVar is StringVar stringVar)
            {
                stringVar.StringValue = value.GetValue<string>() ?? "";
                return;
            }

            dynamicVar.BaseValue = value.GetValue<decimal>();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Typed base class for model-backed capabilities that attach only to <typeparamref name="TModel" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         仅可附加到 <typeparamref name="TModel" /> 的类型化模型能力基类。
    ///     </para>
    /// </summary>
    public abstract class ModelCapability<TModel> : ModelCapability, IModelCapability<TModel>
        where TModel : AbstractModel
    {
        /// <inheritdoc />
        public new TModel? Owner => (TModel?)base.Owner;

        /// <inheritdoc />
        public override void Attach(AbstractModel owner, bool isInternal = false)
        {
            ArgumentNullException.ThrowIfNull(owner);
            if (owner is not TModel)
                throw new ArgumentException(
                    $"Capability '{GetType().FullName}' can only attach to '{typeof(TModel).FullName}'.",
                    nameof(owner));

            base.Attach(owner, isInternal);
        }

        /// <summary>
        ///     <para xml:lang="en">Called when this capability is attached to a typed owner.</para>
        ///     <para xml:lang="zh-CN">此能力附加到类型化所属模型时调用。</para>
        /// </summary>
        protected virtual void OnAttach(TModel owner)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Called when this capability is detached from a typed owner.</para>
        ///     <para xml:lang="zh-CN">此能力从类型化所属模型分离时调用。</para>
        /// </summary>
        protected virtual void OnDetach(TModel owner)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Called after this capability's typed owner is restored while loading a saved run.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         读取一局游戏的存档并恢复此能力的类型化所属模型后调用。
        ///     </para>
        /// </summary>
        protected virtual void OnLoadedFromSave(TModel owner)
        {
        }

        /// <inheritdoc />
        protected sealed override void OnAttach(AbstractModel owner)
        {
            OnAttach((TModel)owner);
        }

        /// <inheritdoc />
        protected sealed override void OnDetach(AbstractModel owner)
        {
            OnDetach((TModel)owner);
        }

        /// <inheritdoc />
        protected sealed override void OnLoadedFromSave()
        {
            if (Owner is { } owner)
                OnLoadedFromSave(owner);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Base class for model capabilities with a typed JSON state payload.</para>
    ///     <para xml:lang="zh-CN">带类型化 JSON 状态负载的模型能力基类。</para>
    /// </summary>
    public abstract class StatefulModelCapability<TState> : ModelCapability
        where TState : class, new()
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the mutable capability state.</para>
        ///     <para xml:lang="zh-CN">获取可变的能力状态。</para>
        /// </summary>
        protected TState State { get; private set; } = new();

        /// <inheritdoc />
        protected override JsonNode? SaveAdditionalState()
        {
            return JsonSerializer.SerializeToNode(State, ModelSavedDataJson.Options);
        }

        /// <inheritdoc />
        protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
        {
            State = ReadState(state, schemaVersion);
        }

        /// <summary>
        ///     <para xml:lang="en">Replaces the state and marks the owning capability set dirty.</para>
        ///     <para xml:lang="zh-CN">替换状态，并将所属能力集合标记为脏。</para>
        /// </summary>
        protected void SetState(TState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            State = state;
            MarkDirty();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Invokes <paramref name="mutate" /> and marks the owning capability set dirty even if the callback
        ///         throws.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         调用 <paramref name="mutate" />；即使回调抛出异常，也会将所属能力集合标记为脏。
        ///     </para>
        /// </summary>
        protected void MutateState(Action<TState> mutate)
        {
            ArgumentNullException.ThrowIfNull(mutate);
            try
            {
                mutate(State);
            }
            finally
            {
                MarkDirty();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Reads state, allowing subclasses to migrate older schema versions.</para>
        ///     <para xml:lang="zh-CN">读取状态，并允许子类迁移旧版架构。</para>
        /// </summary>
        protected virtual TState ReadState(JsonNode? state, int schemaVersion)
        {
            return state?.Deserialize<TState>(ModelSavedDataJson.Options) ?? new TState();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Typed base class for model capabilities with a typed JSON state payload.</para>
    ///     <para xml:lang="zh-CN">带类型化 JSON 状态负载的类型化模型能力基类。</para>
    /// </summary>
    public abstract class StatefulModelCapability<TModel, TState> : ModelCapability<TModel>
        where TModel : AbstractModel
        where TState : class, new()
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the mutable capability state.</para>
        ///     <para xml:lang="zh-CN">获取可变的能力状态。</para>
        /// </summary>
        protected TState State { get; private set; } = new();

        /// <inheritdoc />
        protected override JsonNode? SaveAdditionalState()
        {
            return JsonSerializer.SerializeToNode(State, ModelSavedDataJson.Options);
        }

        /// <inheritdoc />
        protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
        {
            State = ReadState(state, schemaVersion);
        }

        /// <summary>
        ///     <para xml:lang="en">Replaces the state and marks the owning capability set dirty.</para>
        ///     <para xml:lang="zh-CN">替换状态，并将所属能力集合标记为脏。</para>
        /// </summary>
        protected void SetState(TState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            State = state;
            MarkDirty();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Invokes <paramref name="mutate" /> and marks the owning capability set dirty even if the callback
        ///         throws.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         调用 <paramref name="mutate" />；即使回调抛出异常，也会将所属能力集合标记为脏。
        ///     </para>
        /// </summary>
        protected void MutateState(Action<TState> mutate)
        {
            ArgumentNullException.ThrowIfNull(mutate);
            try
            {
                mutate(State);
            }
            finally
            {
                MarkDirty();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Reads state, allowing subclasses to migrate older schema versions.</para>
        ///     <para xml:lang="zh-CN">读取状态，并允许子类迁移旧版架构。</para>
        /// </summary>
        protected virtual TState ReadState(JsonNode? state, int schemaVersion)
        {
            return state?.Deserialize<TState>(ModelSavedDataJson.Options) ?? new TState();
        }
    }
}
