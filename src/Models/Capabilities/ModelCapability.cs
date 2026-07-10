using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.DynamicVars;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Model-backed capability base class. Register subclasses as model capabilities when they need a stable
    ///     <see cref="ModelId" /> and persistence identity.
    ///     基于模型的能力基类。需要稳定 <see cref="ModelId" /> 与持久化身份时，可将子类注册为模型能力。
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
        ///     Capability-owned dynamic vars used by localized text, gameplay commands, and card preview when the
        ///     owner is a card. This set is independent from the owner's own dynamic-var set; read owner variables
        ///     through <see cref="Owner" /> when capability behavior depends on them.
        ///     能力自有动态变量；可用于本地化文本、游戏指令，以及 owner 为卡牌时的卡牌预览。该集合独立于 owner
        ///     自身的动态变量集合；能力行为依赖 owner 变量时，应通过 <see cref="Owner" /> 读取。
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
        ///     Capability-owned canonical dynamic vars. Override to register vars directly on the capability.
        ///     能力拥有的动态变量原型。重写此属性即可直接在能力本身注册变量。
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
        ///     Marks the owning capability set dirty after in-place state changes.
        ///     在原地状态变更后将所属能力集合标记为已变更。
        /// </summary>
        protected void MarkDirty()
        {
            if (Owner != null)
                ModelCapabilities.MarkDirty(Owner);
        }

        /// <summary>
        ///     Saves extra capability state in addition to the capability dynamic vars.
        ///     保存能力动态变量以外的额外能力状态。
        /// </summary>
        protected virtual JsonNode? SaveAdditionalState()
        {
            return null;
        }

        /// <summary>
        ///     Loads extra capability state in addition to the capability dynamic vars.
        ///     读取能力动态变量以外的额外能力状态。
        /// </summary>
        protected virtual void LoadAdditionalState(JsonNode? state, int schemaVersion)
        {
        }

        /// <summary>
        ///     Resets capability-owned dynamic vars to their canonical definitions.
        ///     将能力自有动态变量重置为原型定义。
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
        ///     Removes this capability from its owner collection when it is currently attached.
        ///     当前能力已附加时，将其从所属 collection 中移除。
        /// </summary>
        public bool RemoveFromOwner()
        {
            var owner = Owner;
            return owner != null && ModelCapabilities.Get(owner).Remove(this);
        }

        /// <summary>
        ///     Called when this capability is attached.
        ///     当此能力被附加时调用。
        /// </summary>
        protected virtual void OnAttach(AbstractModel owner)
        {
        }

        /// <summary>
        ///     Called when this capability is detached.
        ///     当此能力被分离时调用。
        /// </summary>
        protected virtual void OnDetach(AbstractModel owner)
        {
        }

        /// <summary>
        ///     Called only after this capability's owner has been restored as part of a saved-run load.
        ///     仅当此能力的 owner 作为跑局读档的一部分恢复后调用。
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
    ///     Typed base class for model-backed capabilities that only attach to <typeparamref name="TModel" />.
    ///     只附加到 <typeparamref name="TModel" /> 的模型能力类型化基类。
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
        ///     Called when this capability is attached to a typed owner.
        ///     当此能力附加到类型化 owner 时调用。
        /// </summary>
        protected virtual void OnAttach(TModel owner)
        {
        }

        /// <summary>
        ///     Called when this capability is detached from a typed owner.
        ///     当此能力从类型化 owner 分离时调用。
        /// </summary>
        protected virtual void OnDetach(TModel owner)
        {
        }

        /// <summary>
        ///     Called only after this capability's typed owner has been restored as part of a saved-run load.
        ///     仅当此能力的类型化 owner 作为跑局读档的一部分恢复后调用。
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
    ///     Model capability base class with a typed JSON state payload.
    ///     带类型化 JSON 状态 payload 的模型能力基类。
    /// </summary>
    public abstract class StatefulModelCapability<TState> : ModelCapability
        where TState : class, new()
    {
        /// <summary>
        ///     Mutable capability state.
        ///     可变能力状态。
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
        ///     Replaces state and marks the owning collection dirty.
        ///     替换状态并将所属 collection 标记为已变更。
        /// </summary>
        protected void SetState(TState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            State = state;
            MarkDirty();
        }

        /// <summary>
        ///     Mutates state and marks the owning collection dirty.
        ///     修改状态并将所属 collection 标记为已变更。
        /// </summary>
        protected void MutateState(Action<TState> mutate)
        {
            ArgumentNullException.ThrowIfNull(mutate);
            mutate(State);
            MarkDirty();
        }

        /// <summary>
        ///     Reads state, allowing subclasses to migrate old schema versions.
        ///     读取状态，并允许子类迁移旧 schema 版本。
        /// </summary>
        protected virtual TState ReadState(JsonNode? state, int schemaVersion)
        {
            return state?.Deserialize<TState>(ModelSavedDataJson.Options) ?? new TState();
        }
    }

    /// <summary>
    ///     Typed model capability base class with a typed JSON state payload.
    ///     带类型化 JSON 状态 payload 的类型化模型能力基类。
    /// </summary>
    public abstract class StatefulModelCapability<TModel, TState> : ModelCapability<TModel>
        where TModel : AbstractModel
        where TState : class, new()
    {
        /// <summary>
        ///     Mutable capability state.
        ///     可变能力状态。
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
        ///     Replaces state and marks the owning collection dirty.
        ///     替换状态并将所属 collection 标记为已变更。
        /// </summary>
        protected void SetState(TState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            State = state;
            MarkDirty();
        }

        /// <summary>
        ///     Mutates state and marks the owning collection dirty.
        ///     修改状态并将所属 collection 标记为已变更。
        /// </summary>
        protected void MutateState(Action<TState> mutate)
        {
            ArgumentNullException.ThrowIfNull(mutate);
            mutate(State);
            MarkDirty();
        }

        /// <summary>
        ///     Reads state, allowing subclasses to migrate old schema versions.
        ///     读取状态，并允许子类迁移旧 schema 版本。
        /// </summary>
        protected virtual TState ReadState(JsonNode? state, int schemaVersion)
        {
            return state?.Deserialize<TState>(ModelSavedDataJson.Options) ?? new TState();
        }
    }
}
