using System.Globalization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Models.Identity;
using STS2RitsuLib.Networking.ManagedActions;

namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     Registry and dispatcher for model right-click interactions.
    ///     模型右键交互的注册表与分发器。
    /// </summary>
    public static class ModRightClickRegistry
    {
        private const string ActionModuleId = "ritsulib";
        private const string CombatActionKey = "model_right_click_combat";
        private const string NonCombatActionKey = "model_right_click_noncombat";
        private const int InitialOffset = 0;
        private const int InterfaceBindingPriority = int.MinValue;
        private const string CapabilityBindingSeparator = "\u001F";
        private const string RightClickPreflightSurface = "right-click preflight";
        private const string RightClickExecuteSurface = "right-click execute";

        private static readonly Lock Gate = new();
        private static long _nextBindingSequence;

        private static readonly List<IModRightClickHandler> Handlers =
        [
            new BuiltInModelRightClickHandler(),
        ];

        private static readonly List<RegisteredRightClickBinding> Bindings = [];

        private static readonly ModRightClickBindingId InterfaceBindingId =
            new(ModContentRegistry.GetQualifiedRightClickId(Const.ModId, "model_interface"));

        private static readonly ModRightClickBindingId CapabilityBindingId =
            new(ModContentRegistry.GetQualifiedRightClickId(Const.ModId, "model_capability"));

        private static readonly RitsuLibManagedNetActionDescriptor<ModRightClickSyncPayload> CombatActionDescriptor =
            new(
                ActionModuleId,
                CombatActionKey,
                SerializePayload,
                DeserializePayload,
                ExecuteManaged,
                GameActionType.CombatPlayPhaseOnly);

        private static readonly RitsuLibManagedNetActionDescriptor<ModRightClickSyncPayload> NonCombatActionDescriptor =
            new(
                ActionModuleId,
                NonCombatActionKey,
                SerializePayload,
                DeserializePayload,
                ExecuteManaged,
                GameActionType.NonCombat);

        /// <summary>
        ///     Registers a custom right-click handler. Higher priority handlers run first.
        ///     注册自定义右键 handler；优先级越高越先运行。
        /// </summary>
        public static void Register(IModRightClickHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            lock (Gate)
            {
                if (Handlers.Contains(handler))
                    return;

                Handlers.Add(handler);
                Handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }

        /// <summary>
        ///     Registers a synced right-click binding for models of type <typeparamref name="TModel" />.
        ///     为 <typeparamref name="TModel" /> 类型的模型注册同步右键绑定。
        /// </summary>
        /// <param name="modId">Owning mod id. 所属 mod id。</param>
        /// <param name="localStem">Local binding id stem. 本地 binding id stem。</param>
        /// <param name="canHandle">
        ///     Execution-time guard. It runs after the synced action resolves the model on each peer. Do not use this
        ///     delegate for local-only UI filtering.
        ///     执行期判定：同步动作在各端解析模型后调用。不要将它用于仅本地 UI 过滤。
        /// </param>
        /// <param name="execute">Synced right-click behavior. 同步右键行为。</param>
        /// <param name="priority">Binding priority; higher values run first. 优先级越高越先运行。</param>
        /// <returns>
        ///     A disposable registration handle.
        ///     可释放的注册句柄。
        /// </returns>
        public static IDisposable Register<TModel>(
            string modId,
            string localStem,
            Func<ModRightClickContext, bool> canHandle,
            Func<ModRightClickExecutionContext, Task> execute,
            int priority = 0)
            where TModel : AbstractModel
        {
            ArgumentNullException.ThrowIfNull(canHandle);

            return Register<TModel>(
                modId,
                localStem,
                execute,
                priority,
                null,
                context => canHandle(new(context.Player, context.Model, context.Trigger)));
        }

        /// <summary>
        ///     Registers a synced right-click binding for models of type <typeparamref name="TModel" />.
        ///     为 <typeparamref name="TModel" /> 类型的模型注册同步右键绑定。
        /// </summary>
        /// <param name="modId">Owning mod id. 所属 mod id。</param>
        /// <param name="localStem">Local binding id stem. 本地 binding id stem。</param>
        /// <param name="execute">Synced right-click behavior. 同步右键行为。</param>
        /// <param name="priority">Binding priority; higher values run first. 优先级越高越先运行。</param>
        /// <param name="canHandleLocal">
        ///     Optional local-only fast filter. Use only stable, local UI facts here; mutable gameplay state should be
        ///     checked in <paramref name="canExecute" /> or <paramref name="execute" />.
        ///     可选的仅本地快速过滤。这里只应使用稳定的本地 UI 信息；可变游戏状态应在
        ///     <paramref name="canExecute" /> 或 <paramref name="execute" /> 中检查。
        /// </param>
        /// <param name="canExecute">
        ///     Optional execution-time guard. It runs after the synced action resolves the model on each peer.
        ///     可选执行期判定：同步动作在各端解析模型后调用。
        /// </param>
        /// <returns>
        ///     A disposable registration handle.
        ///     可释放的注册句柄。
        /// </returns>
        public static IDisposable Register<TModel>(
            string modId,
            string localStem,
            Func<ModRightClickExecutionContext, Task> execute,
            int priority = 0,
            Func<ModRightClickContext, bool>? canHandleLocal = null,
            Func<ModRightClickExecutionContext, bool>? canExecute = null)
            where TModel : AbstractModel
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);
            ArgumentNullException.ThrowIfNull(execute);

            var id = new ModRightClickBindingId(ModContentRegistry.GetQualifiedRightClickId(modId, localStem));
            var binding = new RegisteredRightClickBinding(
                id,
                typeof(TModel),
                canHandleLocal,
                canExecute,
                execute,
                priority,
                Interlocked.Increment(ref _nextBindingSequence));

            lock (Gate)
            {
                if (Bindings.Any(existing => existing.Id == id))
                    throw new InvalidOperationException($"Right-click binding is already registered: {id}");

                Bindings.Add(binding);
                SortBindings();
            }

            return binding;
        }

        /// <summary>
        ///     Attempts to dispatch a local right-click request.
        ///     尝试分发一个本地右键请求。
        /// </summary>
        public static bool TryDispatch(ModRightClickContext context)
        {
            IModRightClickHandler[] handlers;
            lock (Gate)
            {
                handlers = [.. Handlers];
            }

            return handlers.Any(handler => handler.TryHandle(context));
        }

        internal static void RegisterBuiltInSyncDescriptors()
        {
            RitsuLibManagedNetActions.Register(CombatActionDescriptor);
            RitsuLibManagedNetActions.Register(NonCombatActionDescriptor);
        }

        private static bool TryRequestSyncedModelAction(
            ModRightClickContext context,
            IReadOnlyList<ModRightClickBindingId> bindingIds)
        {
            if (!TryCreatePayload(context, out var payload))
                return false;

            RegisterBuiltInSyncDescriptors();
            payload = payload with { BindingIds = [.. bindingIds] };
            var descriptor = CombatManager.Instance.IsInProgress
                ? CombatActionDescriptor
                : NonCombatActionDescriptor;
            return RitsuLibManagedNetActions.Request(
                RunManager.Instance,
                descriptor,
                payload,
                context.Player.NetId);
        }

        private static bool TryCreatePayload(ModRightClickContext context, out ModRightClickSyncPayload payload)
        {
            payload = default;
            if (!TryGetModelKind(context.Model, context.Player, out var kind))
                return false;
            if (!ModModelIdentityRegistry.TryGetToken(context.Model, out var token))
                return false;

            payload = new(
                context.Player.NetId,
                kind,
                token,
                context.Trigger,
                []);
            return true;
        }

        private static bool TryGetModelKind(
            AbstractModel model,
            Player player,
            out ModRightClickModelKind kind)
        {
            kind = default;
            switch (model)
            {
                case CardModel card when card.Owner == player:
                    kind = ModRightClickModelKind.Card;
                    return true;

                case RelicModel relic when relic.Owner == player:
                    kind = ModRightClickModelKind.Relic;
                    return true;

                case PowerModel power when IsPowerReachableForPlayer(power, player):
                    kind = ModRightClickModelKind.Power;
                    return true;

                case PotionModel potion when potion.Owner == player:
                    kind = ModRightClickModelKind.Potion;
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsPowerReachableForPlayer(PowerModel power, Player player)
        {
            return power.Owner.Player == player ||
                   power.Owner.PetOwner == player ||
                   power.Owner.IsEnemy;
        }

        private static byte[] SerializePayload(ModRightClickSyncPayload payload)
        {
            var writer = new PacketWriter { WarnOnGrow = false };
            writer.WriteULong(payload.OwnerNetId);
            writer.WriteEnum(payload.Kind);
            writer.WriteFullModelId(payload.Token.ModelId);
            writer.WriteUInt(payload.Token.Identity.Value);

            writer.WriteBool(payload.Trigger.IsController);
            writer.WriteBool(payload.Trigger.Metadata != null);
            if (payload.Trigger.Metadata != null)
                writer.WriteString(payload.Trigger.Metadata);

            SerializeBindingIds(writer, payload.BindingIds);
            writer.ZeroByteRemainder();
            return [.. writer.Buffer.AsSpan(InitialOffset, writer.BytePosition)];
        }

        private static ModRightClickSyncPayload DeserializePayload(ReadOnlySpan<byte> bytes)
        {
            var reader = new PacketReader();
            reader.Reset([.. bytes]);
            var ownerNetId = reader.ReadULong();
            var kind = reader.ReadEnum<ModRightClickModelKind>();
            var modelId = reader.ReadFullModelId();
            var identity = new ModModelIdentity(reader.ReadUInt());

            var isController = reader.ReadBool();
            var metadata = reader.ReadBool() ? reader.ReadString() : null;
            var bindingIds = DeserializeBindingIds(reader);
            if (bindingIds.Count == 0)
                bindingIds = [InterfaceBindingId];
            return new(
                ownerNetId,
                kind,
                new(identity, modelId),
                new(isController, metadata),
                bindingIds);
        }

        private static async Task ExecuteManaged(
            RitsuLibManagedNetActionContext<ModRightClickSyncPayload> context)
        {
            if (context.Message.OwnerNetId != context.Player.NetId)
                return;

            await ExecutePayload(context.Message, context.PlayerChoiceContext, context.Action);
        }

        private static async Task ExecutePayload(
            ModRightClickSyncPayload payload,
            GameActionPlayerChoiceContext? playerChoiceContext,
            GameAction? action)
        {
            if (!TryGetPlayer(payload.OwnerNetId, out var player))
                return;
            if (!TryResolveModel(player, payload, out var model))
                return;

            var executionContext = new ModRightClickExecutionContext(
                player,
                model,
                payload.Trigger,
                playerChoiceContext,
                action);
            var completed = false;
            foreach (var bindingId in payload.BindingIds)
                try
                {
                    if (await TryExecuteBinding(bindingId, model, executionContext))
                        completed = true;
                    if (!IsModelStillInState(model))
                        break;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RightClick] Binding execution failed. BindingId='{bindingId}' " +
                        $"ModelId='{model.Id}' OwnerType='{model.GetType().FullName}' Error='{ex.Message}'");
                }

            if (completed)
                model.InvokeExecutionFinished();
        }

        private static bool IsModelStillInState(AbstractModel model)
        {
            try
            {
                return model switch
                {
                    CardModel card => !card.HasBeenRemovedFromState,
                    RelicModel relic => !relic.HasBeenRemovedFromState && relic.Owner.Relics.Contains(relic),
                    PowerModel power => power.Owner.Powers.Contains(power),
                    PotionModel potion => !potion.HasBeenRemovedFromState && potion.Owner.Potions.Contains(potion),
                    _ => true,
                };
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RightClick] Failed to check model state before ExecutionFinished. " +
                    $"ModelId='{model.Id}' OwnerType='{model.GetType().FullName}' Error='{ex.Message}'");
                return false;
            }
        }

        private static bool TryGetPlayer(ulong ownerNetId, out Player player)
        {
            player = RunManager.Instance.DebugOnlyGetState()
                ?.Players
                .FirstOrDefault(p => p.NetId == ownerNetId)!;
            return player != null;
        }

        private static bool TryResolveModel(
            Player player,
            ModRightClickSyncPayload payload,
            out AbstractModel model)
        {
            model = null!;
            if (!ModModelIdentityRegistry.TryResolve(payload.Token, out var resolved))
                return false;

            switch (payload.Kind)
            {
                case ModRightClickModelKind.Card:
                    if (resolved is not CardModel card || card.Owner != player ||
                        card.Pile?.Type != PileType.Hand)
                        return false;

                    model = card;
                    return true;

                case ModRightClickModelKind.Relic:
                    if (resolved is not RelicModel relic || relic.Owner != player)
                        return false;

                    model = relic;
                    return true;

                case ModRightClickModelKind.Power:
                    if (resolved is not PowerModel power || !IsPowerReachableForPlayer(power, player))
                        return false;

                    model = power;
                    return true;

                case ModRightClickModelKind.Potion:
                    if (resolved is not PotionModel potion || potion.Owner != player)
                        return false;

                    model = potion;
                    return true;

                default:
                    return false;
            }
        }

        private static async Task<bool> TryExecuteBinding(
            ModRightClickBindingId bindingId,
            AbstractModel model,
            ModRightClickExecutionContext context)
        {
            if (bindingId == InterfaceBindingId)
            {
                if (model is not IModRightClickableModel rightClickable)
                    return false;
                if (!TryCanExecuteRightClickable(rightClickable, context, out var interfaceCanExecute))
                    return false;
                if (!interfaceCanExecute)
                    return true;

                await rightClickable.OnRightClick(context);
                return true;
            }

            if (bindingId == CapabilityBindingId)
                return await TryExecuteCapabilityRightClick(model, context, null);
            if (TryParseCapabilityBindingId(bindingId, out var selectedCapabilities))
                return await TryExecuteCapabilityRightClick(model, context, selectedCapabilities);

            var binding = TryGetBinding(bindingId);
            if (binding == null || !binding.ModelType.IsInstanceOfType(model))
                return false;
            if (!TryCanExecute(binding, context, out var canExecute))
                return false;
            if (!canExecute)
                return true;

            await binding.Execute(context);
            return true;
        }

        private static bool TryCanExecuteRightClickable(
            IModRightClickableModel rightClickable,
            ModRightClickExecutionContext context,
            out bool canExecute)
        {
            canExecute = false;
            try
            {
                canExecute = rightClickable.CanExecuteRightClick(context);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RightClick] Interface execute guard failed. " +
                    $"ModelId='{context.Model.Id}' OwnerType='{context.Model.GetType().FullName}' " +
                    $"SourceType='{rightClickable.GetType().FullName}' Error='{ex.Message}'");
                return false;
            }
        }

        private static bool TryCanExecute(
            RegisteredRightClickBinding binding,
            ModRightClickExecutionContext context,
            out bool canExecute)
        {
            canExecute = true;
            if (binding.CanExecute == null)
                return true;

            try
            {
                canExecute = binding.CanExecute(context);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[RightClick] Binding execute guard failed. BindingId='{binding.Id}' " +
                    $"ModelId='{context.Model.Id}' OwnerType='{context.Model.GetType().FullName}' " +
                    $"Error='{ex.Message}'");
                return false;
            }
        }

        private static async Task<bool> TryExecuteCapabilityRightClick(
            AbstractModel model,
            ModRightClickExecutionContext context,
            IReadOnlyList<SelectedRightClickCapability>? selectedCapabilities)
        {
            var completed = false;
            foreach (var capability in GetRightClickCapabilities(model, selectedCapabilities))
            {
                if (!TryCanExecuteCapability(capability, context, out var canExecute))
                    continue;
                if (!canExecute)
                {
                    completed = true;
                    if (capability.RightClickRunMode == ModelRightClickCapabilityRunMode.Exclusive)
                        break;

                    continue;
                }

                try
                {
                    await capability.OnRightClick(context);
                }
                catch (Exception ex)
                {
                    ModelCapabilityDiagnostics.WarnFailure(RightClickExecuteSurface, model, capability, ex);
                    continue;
                }

                completed = true;
                if (capability.RightClickRunMode == ModelRightClickCapabilityRunMode.Exclusive)
                    break;
            }

            return completed;
        }

        private static IReadOnlyList<IModelRightClickCapability> GetRightClickCapabilities(
            AbstractModel model,
            IReadOnlyList<SelectedRightClickCapability>? selectedCapabilities = null)
        {
            var capabilities = GetAllRightClickCapabilities(model);
            if (selectedCapabilities == null)
                return capabilities;

            var selected = new List<IModelRightClickCapability>(selectedCapabilities.Count);
            selected.AddRange(from selectedCapability in selectedCapabilities
                where selectedCapability.Index >= 0 && selectedCapability.Index < capabilities.Count &&
                      string.Equals(GetCapabilityId(capabilities[selectedCapability.Index]),
                          selectedCapability.CapabilityId, StringComparison.Ordinal)
                select capabilities[selectedCapability.Index]);

            return selected;
        }

        private static IReadOnlyList<IModelRightClickCapability> GetAllRightClickCapabilities(AbstractModel model)
        {
            if (ModelCapabilities.TryGet(model, out var collection))
                return SortRightClickCapabilities(collection.All);
            if (!ModelCapabilityDefaults.HasDefaultCapabilitySource(model))
                return [];

            collection = ModelCapabilities.Get(model);

            return SortRightClickCapabilities(collection.All);
        }

        private static bool TryCanHandleCapability(
            IModelRightClickCapability capability,
            ModRightClickContext context)
        {
            try
            {
                return capability.CanHandleRightClickLocal(context);
            }
            catch (Exception ex)
            {
                ModelCapabilityDiagnostics.WarnFailure(RightClickPreflightSurface, context.Model, capability, ex);
                return false;
            }
        }

        private static bool TryCanExecuteCapability(
            IModelRightClickCapability capability,
            ModRightClickExecutionContext context,
            out bool canExecute)
        {
            canExecute = false;
            try
            {
                canExecute = capability.CanExecuteRightClick(context);
                return true;
            }
            catch (Exception ex)
            {
                ModelCapabilityDiagnostics.WarnFailure(RightClickExecuteSurface, context.Model, capability, ex);
                return false;
            }
        }

        private static ModRightClickBindingId CreateCapabilityBindingId(
            IReadOnlyList<SelectedRightClickCapability> selectedCapabilities)
        {
            var parts = new List<string>(2 + selectedCapabilities.Count * 2)
            {
                CapabilityBindingId.Id,
                selectedCapabilities.Count.ToString(CultureInfo.InvariantCulture),
            };
            foreach (var selectedCapability in selectedCapabilities)
            {
                parts.Add(selectedCapability.Index.ToString(CultureInfo.InvariantCulture));
                parts.Add(selectedCapability.CapabilityId);
            }

            return new(string.Join(CapabilityBindingSeparator, parts));
        }

        private static string GetCapabilityId(IModelRightClickCapability capability)
        {
            return ((IModelCapability)capability).CapabilityId;
        }

        private static bool TryParseCapabilityBindingId(
            ModRightClickBindingId bindingId,
            out IReadOnlyList<SelectedRightClickCapability> selectedCapabilities)
        {
            selectedCapabilities = [];
            var prefix = CapabilityBindingId.Id + CapabilityBindingSeparator;
            if (!bindingId.Id.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            var parts = bindingId.Id.Split(CapabilityBindingSeparator);
            if (parts.Length < 4 ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var count) ||
                count <= 0 ||
                parts.Length != 2 + count * 2)
                return false;

            var capabilities = new List<SelectedRightClickCapability>(count);
            for (var i = 0; i < count; i++)
            {
                var indexPart = parts[2 + i * 2];
                var capabilityId = parts[3 + i * 2];
                if (!int.TryParse(indexPart, NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
                    string.IsNullOrWhiteSpace(capabilityId))
                    return false;

                capabilities.Add(new(index, capabilityId));
            }

            selectedCapabilities = capabilities;
            return true;
        }

        private static IReadOnlyList<IModelRightClickCapability> SortRightClickCapabilities(
            IReadOnlyList<IModelCapability> capabilities)
        {
            return
            [
                .. capabilities
                    .Select((capability, index) => new OrderedRightClickCapability(capability, index))
                    .Where(static entry => entry.Capability is IModelRightClickCapability)
                    .OrderByDescending(static entry =>
                        ((IModelRightClickCapability)entry.Capability).RightClickPriority)
                    .ThenBy(static entry => entry.Index)
                    .Select(static entry => (IModelRightClickCapability)entry.Capability),
            ];
        }

        private static RegisteredRightClickBinding? TryGetBinding(ModRightClickBindingId bindingId)
        {
            lock (Gate)
            {
                return Bindings.FirstOrDefault(binding => binding.Id == bindingId);
            }
        }

        private static RegisteredRightClickBinding[] GetBindingsSnapshot()
        {
            lock (Gate)
            {
                return [.. Bindings];
            }
        }

        private static void SortBindings()
        {
            Bindings.Sort((a, b) =>
            {
                var priority = b.Priority.CompareTo(a.Priority);
                return priority != 0 ? priority : a.Sequence.CompareTo(b.Sequence);
            });
        }

        private static void SerializeBindingIds(
            PacketWriter writer,
            IReadOnlyList<ModRightClickBindingId> bindingIds)
        {
            writer.WriteInt(bindingIds.Count);
            foreach (var bindingId in bindingIds)
                writer.WriteString(bindingId.Id);
        }

        private static IReadOnlyList<ModRightClickBindingId> DeserializeBindingIds(PacketReader reader)
        {
            var remainingBits = reader.Buffer.Length * 8 - reader.BitPosition;
            if (remainingBits < 32)
                return [];

            var count = reader.ReadInt();
            if (count <= 0)
                return [];

            var ids = new List<ModRightClickBindingId>(count);
            for (var i = 0; i < count; i++)
            {
                var id = reader.ReadString();
                if (!string.IsNullOrWhiteSpace(id))
                    ids.Add(new(id.Trim()));
            }

            return ids;
        }

        private sealed class BuiltInModelRightClickHandler : IModRightClickHandler
        {
            public bool TryHandle(ModRightClickContext context)
            {
                var bindingIds = CollectBindingIds(context);
                return bindingIds.Count > 0 && TryRequestSyncedModelAction(context, bindingIds);
            }

            private static List<ModRightClickBindingId> CollectBindingIds(ModRightClickContext context)
            {
                var bindings = GetBindingsSnapshot();
                var candidates = (from binding in bindings
                    where binding.ModelType.IsInstanceOfType(context.Model)
                    where TryCanHandleLocal(binding, context)
                    select new LocalRightClickCandidate(binding.Id, binding.Priority, binding.Sequence)).ToList();

                if (context.Model is IModRightClickableModel rightClickable &&
                    TryCanHandleRightClickable(rightClickable, context))
                    candidates.Add(new(InterfaceBindingId, InterfaceBindingPriority, long.MaxValue));

                AddCapabilityBindings(context, candidates);

                return
                [
                    .. candidates
                        .OrderByDescending(static candidate => candidate.Priority)
                        .ThenBy(static candidate => candidate.Sequence)
                        .Select(static candidate => candidate.Id),
                ];
            }

            private static void AddCapabilityBindings(
                ModRightClickContext context,
                List<LocalRightClickCandidate> candidates)
            {
                var capabilities = GetAllRightClickCapabilities(context.Model);
                var selectedCapabilities = new List<SelectedRightClickCapability>();
                var priority = int.MinValue;
                for (var index = 0; index < capabilities.Count; index++)
                {
                    var capability = capabilities[index];
                    if (!TryCanHandleCapability(capability, context))
                        continue;

                    selectedCapabilities.Add(new(index, GetCapabilityId(capability)));
                    priority = Math.Max(priority, capability.RightClickPriority);
                }

                if (selectedCapabilities.Count == 0)
                    return;

                candidates.Add(new(CreateCapabilityBindingId(selectedCapabilities), priority, long.MinValue));
            }

            private static bool TryCanHandleRightClickable(
                IModRightClickableModel rightClickable,
                ModRightClickContext context)
            {
                try
                {
                    return rightClickable.CanHandleRightClickLocal(context);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RightClick] Interface preflight failed. " +
                        $"ModelId='{context.Model.Id}' OwnerType='{context.Model.GetType().FullName}' " +
                        $"SourceType='{rightClickable.GetType().FullName}' Error='{ex.Message}'");
                    return false;
                }
            }

            private static bool TryCanHandleLocal(RegisteredRightClickBinding binding, ModRightClickContext context)
            {
                if (binding.CanHandleLocal == null)
                    return true;

                try
                {
                    return binding.CanHandleLocal(context);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RightClick] Binding preflight failed. BindingId='{binding.Id}' " +
                        $"ModelId='{context.Model.Id}' OwnerType='{context.Model.GetType().FullName}' " +
                        $"Error='{ex.Message}'");
                    return false;
                }
            }
        }

        private readonly record struct LocalRightClickCandidate(ModRightClickBindingId Id, int Priority, long Sequence);

        private sealed class RegisteredRightClickBinding(
            ModRightClickBindingId id,
            Type modelType,
            Func<ModRightClickContext, bool>? canHandleLocal,
            Func<ModRightClickExecutionContext, bool>? canExecute,
            Func<ModRightClickExecutionContext, Task> execute,
            int priority,
            long sequence) : IDisposable
        {
            private bool _disposed;

            public ModRightClickBindingId Id { get; } = id;
            public Type ModelType { get; } = modelType;
            public Func<ModRightClickContext, bool>? CanHandleLocal { get; } = canHandleLocal;
            public Func<ModRightClickExecutionContext, bool>? CanExecute { get; } = canExecute;
            public Func<ModRightClickExecutionContext, Task> Execute { get; } = execute;
            public int Priority { get; } = priority;
            public long Sequence { get; } = sequence;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                lock (Gate)
                {
                    Bindings.Remove(this);
                }
            }
        }

        private readonly record struct OrderedRightClickCapability(IModelCapability Capability, int Index);

        private readonly record struct SelectedRightClickCapability(int Index, string CapabilityId);

        private readonly record struct ModRightClickSyncPayload(
            ulong OwnerNetId,
            ModRightClickModelKind Kind,
            ModModelIdentityToken Token,
            ModRightClickTrigger Trigger,
            IReadOnlyList<ModRightClickBindingId> BindingIds);
    }
}
