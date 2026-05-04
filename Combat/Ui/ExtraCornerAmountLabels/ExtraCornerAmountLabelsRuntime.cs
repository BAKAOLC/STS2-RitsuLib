using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    internal static class ExtraCornerAmountLabelsRuntime
    {
        private const string PowerSlotPrefix = "RitsuExtraPowerCornerSlot_";
        private const string RelicSlotPrefix = "RitsuExtraRelicCornerSlot_";
        private const string IntentSlotPrefix = "RitsuExtraIntentCornerSlot_";

        private static readonly FieldInfo? PowerAmountField = AccessTools.Field(typeof(NPower), "_amountLabel");

        private static readonly FieldInfo? RelicAmountField =
            AccessTools.Field(typeof(NRelicInventoryHolder), "_amountLabel");

        private static readonly FieldInfo? IntentValueField = AccessTools.Field(typeof(NIntent), "_valueLabel");
        private static readonly FieldInfo? IntentIntentField = AccessTools.Field(typeof(NIntent), "_intent");

        private static readonly Lock SyncRoot = new();
        private static readonly Dictionary<ulong, PowerHostState> PowerStates = [];
        private static readonly Dictionary<ulong, RelicHostState> RelicStates = [];
        private static readonly Dictionary<ulong, IntentHostState> IntentStates = [];

        internal static void SyncPower(NPower node)
        {
            var model = SafeReadModel(node);
            if (model is not IPowerExtraIconAmountLabelsProvider provider)
            {
                ClearPower(node);
                return;
            }

            MegaLabel? amountRef;
            PowerHostState state;
            ExtraIconAmountLabelSlot[] snapshot;
            lock (SyncRoot)
            {
                state = GetOrCreatePowerState(node.GetInstanceId());
                state.OwnerInstanceId = node.GetInstanceId();
                ResubscribePower(state, node, model, provider);
                amountRef = PowerAmountField?.GetValue(node) as MegaLabel;
                snapshot = SnapshotSlots(provider.GetPowerExtraIconAmountLabelSlots());
            }

            SyncAnchoredSlotHosts(node, state.Slots, PowerSlotPrefix, snapshot, ExtraCornerHostKind.Power,
                l => ExtraCornerPowerAmountStyle.Apply(l, amountRef));
        }

        internal static void ClearPower(NPower node)
        {
            lock (SyncRoot)
            {
                if (!PowerStates.Remove(node.GetInstanceId(), out var state))
                    return;

                UnsubscribePowerState(state);
                FreeAnchoredSlotHosts(state.Slots);
            }
        }

        internal static void SyncRelic(NRelicInventoryHolder node)
        {
            var model = node.Relic.Model;
            if (model is not IRelicExtraIconAmountLabelsProvider provider)
            {
                ClearRelic(node);
                return;
            }

            MegaLabel? amountRef;
            RelicHostState state;
            ExtraIconAmountLabelSlot[] snapshot;
            lock (SyncRoot)
            {
                state = GetOrCreateRelicState(node.GetInstanceId());
                state.OwnerInstanceId = node.GetInstanceId();
                ResubscribeRelic(state, node, model, provider);
                amountRef = RelicAmountField?.GetValue(node) as MegaLabel;
                snapshot = SnapshotSlots(provider.GetRelicExtraIconAmountLabelSlots());
            }

            SyncAnchoredSlotHosts(node, state.Slots, RelicSlotPrefix, snapshot, ExtraCornerHostKind.Relic,
                l => ExtraCornerRelicAmountStyle.Apply(l, amountRef));
        }

        internal static void ClearRelic(NRelicInventoryHolder node)
        {
            lock (SyncRoot)
            {
                if (!RelicStates.Remove(node.GetInstanceId(), out var state))
                    return;

                UnsubscribeRelicState(state);
                FreeAnchoredSlotHosts(state.Slots);
            }
        }

        internal static void SyncIntent(NIntent node)
        {
            var intent = IntentIntentField?.GetValue(node) as AbstractIntent;
            if (intent is not IIntentExtraCornerAmountLabelsProvider provider)
            {
                ClearIntent(node);
                return;
            }

            RichTextLabel? valueRef;
            IntentHostState state;
            ExtraIconAmountLabelSlot[] snapshot;
            lock (SyncRoot)
            {
                state = GetOrCreateIntentState(node.GetInstanceId());
                state.OwnerInstanceId = node.GetInstanceId();
                ResubscribeIntent(state, node, intent, provider);
                valueRef = IntentValueField?.GetValue(node) as RichTextLabel;
                snapshot = SnapshotSlots(provider.GetIntentExtraCornerAmountLabelSlots());
            }

            SyncAnchoredSlotHosts(node, state.Slots, IntentSlotPrefix, snapshot, ExtraCornerHostKind.Intent,
                l => ExtraCornerIntentValueStyle.Apply(l, valueRef));
        }

        internal static void ClearIntent(NIntent node)
        {
            lock (SyncRoot)
            {
                if (!IntentStates.Remove(node.GetInstanceId(), out var state))
                    return;

                UnsubscribeIntentState(state);
                FreeAnchoredSlotHosts(state.Slots);
            }
        }

        private static ExtraIconAmountLabelSlot[] SnapshotSlots(IReadOnlyList<ExtraIconAmountLabelSlot> slots)
        {
            if (slots.Count == 0)
                return [];

            if (slots is ExtraIconAmountLabelSlot[] existing)
                return (ExtraIconAmountLabelSlot[])existing.Clone();

            var copy = new ExtraIconAmountLabelSlot[slots.Count];
            for (var i = 0; i < slots.Count; i++)
                copy[i] = slots[i];

            return copy;
        }

        private static void SyncAnchoredSlotHosts(
            Control host,
            List<AnchoredSlotHost> pool,
            string slotNamePrefix,
            ExtraIconAmountLabelSlot[] specs,
            ExtraCornerHostKind hostKind,
            Action<MegaLabel> applyHostStyle)
        {
            var writeIndex = 0;
            foreach (var slot in specs)
            {
                if (string.IsNullOrWhiteSpace(slot.Text))
                    continue;

                if (slot.Corner == ExtraIconAmountLabelCorner.Custom &&
                    (slot.CustomRect.Size.X <= 0f || slot.CustomRect.Size.Y <= 0f))
                    continue;

                while (pool.Count <= writeIndex)
                    pool.Add(new());

                var entry = pool[writeIndex];
                if (!GodotObject.IsInstanceValid(entry.Label))
                {
                    var label = new MegaLabel
                    {
                        Name = $"{slotNamePrefix}{writeIndex}",
                        MouseFilter = Control.MouseFilterEnum.Ignore,
                        ClipContents = true,
                    };
                    host.AddChild(label);
                    host.MoveChild(label, host.GetChildCount() - 1);
                    entry.Label = label;
                }

                var live = entry.Label!;
                applyHostStyle(live);
                ExtraCornerHostLayout.ApplySlotAlignment(live, hostKind, in slot);
                ExtraCornerHostLayout.ApplySlotBounds(live, hostKind, in slot);
                live.SetTextAutoSize(slot.Text.Trim());
                live.Visible = true;
                writeIndex++;
            }

            for (var j = writeIndex; j < pool.Count; j++)
                if (GodotObject.IsInstanceValid(pool[j].Label))
                    pool[j].Label!.Visible = false;
        }

        private static void FreeAnchoredSlotHosts(List<AnchoredSlotHost> pool)
        {
            foreach (var entry in pool)
            {
                entry.Label?.QueueFree();
                entry.Label = null;
            }

            pool.Clear();
        }

        private static PowerModel? SafeReadModel(NPower node)
        {
            try
            {
                return node.Model;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static PowerHostState GetOrCreatePowerState(ulong id)
        {
            if (PowerStates.TryGetValue(id, out var state)) return state;
            state = new();
            PowerStates[id] = state;

            return state;
        }

        private static RelicHostState GetOrCreateRelicState(ulong id)
        {
            if (RelicStates.TryGetValue(id, out var state)) return state;
            state = new();
            RelicStates[id] = state;

            return state;
        }

        private static IntentHostState GetOrCreateIntentState(ulong id)
        {
            if (IntentStates.TryGetValue(id, out var state)) return state;
            state = new();
            IntentStates[id] = state;

            return state;
        }

        private static void ResubscribePower(PowerHostState state, NPower node, PowerModel model,
            IPowerExtraIconAmountLabelsProvider provider)
        {
            if (ReferenceEquals(state.SubscribedModel, model))
                return;

            UnsubscribePowerState(state);
            state.SubscribedModel = model;
            if (provider is not IPowerExtraIconAmountLabelsChangeSource change) return;
            var ownerId = state.OwnerInstanceId;
            state.Handler = () =>
            {
                if (GodotObject.InstanceFromId(ownerId) is NPower n)
                    SyncPower(n);
            };
            change.PowerExtraIconAmountLabelsInvalidated += state.Handler;
        }

        private static void UnsubscribePowerState(PowerHostState state)
        {
            if (state is { SubscribedModel: IPowerExtraIconAmountLabelsChangeSource c, Handler: not null })
                c.PowerExtraIconAmountLabelsInvalidated -= state.Handler;

            state.Handler = null;
            state.SubscribedModel = null;
        }

        private static void ResubscribeRelic(RelicHostState state, NRelicInventoryHolder node, RelicModel model,
            IRelicExtraIconAmountLabelsProvider provider)
        {
            if (ReferenceEquals(state.SubscribedModel, model))
                return;

            UnsubscribeRelicState(state);
            state.SubscribedModel = model;
            if (provider is not IRelicExtraIconAmountLabelsChangeSource change) return;
            var ownerId = state.OwnerInstanceId;
            state.Handler = () =>
            {
                if (GodotObject.InstanceFromId(ownerId) is NRelicInventoryHolder n)
                    SyncRelic(n);
            };
            change.RelicExtraIconAmountLabelsInvalidated += state.Handler;
        }

        private static void UnsubscribeRelicState(RelicHostState state)
        {
            if (state is { SubscribedModel: IRelicExtraIconAmountLabelsChangeSource c, Handler: not null })
                c.RelicExtraIconAmountLabelsInvalidated -= state.Handler;

            state.Handler = null;
            state.SubscribedModel = null;
        }

        private static void ResubscribeIntent(IntentHostState state, NIntent node, AbstractIntent intent,
            IIntentExtraCornerAmountLabelsProvider provider)
        {
            if (ReferenceEquals(state.SubscribedIntent, intent))
                return;

            UnsubscribeIntentState(state);
            state.SubscribedIntent = intent;
            if (provider is not IIntentExtraCornerAmountLabelsChangeSource change) return;
            var ownerId = state.OwnerInstanceId;
            state.Handler = () =>
            {
                if (GodotObject.InstanceFromId(ownerId) is NIntent n)
                    SyncIntent(n);
            };
            change.IntentExtraCornerAmountLabelsInvalidated += state.Handler;
        }

        private static void UnsubscribeIntentState(IntentHostState state)
        {
            if (state is { SubscribedIntent: IIntentExtraCornerAmountLabelsChangeSource c, Handler: not null })
                c.IntentExtraCornerAmountLabelsInvalidated -= state.Handler;

            state.Handler = null;
            state.SubscribedIntent = null;
        }

        private sealed class AnchoredSlotHost
        {
            public MegaLabel? Label;
        }

        private sealed class PowerHostState
        {
            public readonly List<AnchoredSlotHost> Slots = [];
            public Action? Handler;
            public ulong OwnerInstanceId;
            public PowerModel? SubscribedModel;
        }

        private sealed class RelicHostState
        {
            public readonly List<AnchoredSlotHost> Slots = [];
            public Action? Handler;
            public ulong OwnerInstanceId;
            public RelicModel? SubscribedModel;
        }

        private sealed class IntentHostState
        {
            public readonly List<AnchoredSlotHost> Slots = [];
            public Action? Handler;
            public ulong OwnerInstanceId;
            public AbstractIntent? SubscribedIntent;
        }
    }
}
