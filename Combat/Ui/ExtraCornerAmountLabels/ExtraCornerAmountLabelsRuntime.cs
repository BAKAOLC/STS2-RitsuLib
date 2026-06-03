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
        private static readonly Lock CornerErrorSync = new();
        private static readonly HashSet<string> CornerErrorOnceKeys = [];

        internal static void SyncPower(NPower node)
        {
            var model = SafeReadModel(node);
            if (model is not IPowerExtraIconAmountLabelsProvider &&
                model is not IPowerExtraIconAmountLabelSpecsProvider)
            {
                ClearPower(node);
                return;
            }

            MegaLabel? amountRef;
            PowerHostState state;
            ExtraIconAmountLabelSpec[] snapshot;
            lock (SyncRoot)
            {
                state = GetOrCreatePowerState(node.GetInstanceId());
                state.OwnerInstanceId = node.GetInstanceId();
                ResubscribePower(state, node, model, model);
                amountRef = PowerAmountField?.GetValue(node) as MegaLabel;
                snapshot = SnapshotPowerSpecs(model);
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
            if (model is not IRelicExtraIconAmountLabelsProvider &&
                model is not IRelicExtraIconAmountLabelSpecsProvider)
            {
                ClearRelic(node);
                return;
            }

            MegaLabel? amountRef;
            RelicHostState state;
            ExtraIconAmountLabelSpec[] snapshot;
            lock (SyncRoot)
            {
                state = GetOrCreateRelicState(node.GetInstanceId());
                state.OwnerInstanceId = node.GetInstanceId();
                ResubscribeRelic(state, node, model, model);
                amountRef = RelicAmountField?.GetValue(node) as MegaLabel;
                snapshot = SnapshotRelicSpecs(model);
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
            if (intent is not IIntentExtraCornerAmountLabelsProvider &&
                intent is not IIntentExtraCornerAmountLabelSpecsProvider)
            {
                ClearIntent(node);
                return;
            }

            RichTextLabel? valueRef;
            IntentHostState state;
            ExtraIconAmountLabelSpec[] snapshot;
            lock (SyncRoot)
            {
                state = GetOrCreateIntentState(node.GetInstanceId());
                state.OwnerInstanceId = node.GetInstanceId();
                ResubscribeIntent(state, node, intent, intent);
                valueRef = IntentValueField?.GetValue(node) as RichTextLabel;
                snapshot = SnapshotIntentSpecs(intent);
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

        private static ExtraIconAmountLabelSpec[] SnapshotPowerSpecs(PowerModel model)
        {
            if (model is IPowerExtraIconAmountLabelSpecsProvider specsProvider)
                return SnapshotSpecs(specsProvider.GetPowerExtraIconAmountLabelSpecs());

            return model is IPowerExtraIconAmountLabelsProvider slotsProvider
                ? SnapshotSlotsAsSpecs(slotsProvider.GetPowerExtraIconAmountLabelSlots())
                : [];
        }

        private static ExtraIconAmountLabelSpec[] SnapshotRelicSpecs(RelicModel model)
        {
            if (model is IRelicExtraIconAmountLabelSpecsProvider specsProvider)
                return SnapshotSpecs(specsProvider.GetRelicExtraIconAmountLabelSpecs());

            return model is IRelicExtraIconAmountLabelsProvider slotsProvider
                ? SnapshotSlotsAsSpecs(slotsProvider.GetRelicExtraIconAmountLabelSlots())
                : [];
        }

        private static ExtraIconAmountLabelSpec[] SnapshotIntentSpecs(AbstractIntent intent)
        {
            if (intent is IIntentExtraCornerAmountLabelSpecsProvider specsProvider)
                return SnapshotSpecs(specsProvider.GetIntentExtraCornerAmountLabelSpecs());

            return intent is IIntentExtraCornerAmountLabelsProvider slotsProvider
                ? SnapshotSlotsAsSpecs(slotsProvider.GetIntentExtraCornerAmountLabelSlots())
                : [];
        }

        private static ExtraIconAmountLabelSpec[] SnapshotSpecs(IReadOnlyList<ExtraIconAmountLabelSpec> specs)
        {
            if (specs.Count == 0)
                return [];

            if (specs is ExtraIconAmountLabelSpec[] existing)
                return (ExtraIconAmountLabelSpec[])existing.Clone();

            var copy = new ExtraIconAmountLabelSpec[specs.Count];
            for (var i = 0; i < specs.Count; i++)
                copy[i] = specs[i];

            return copy;
        }

        private static ExtraIconAmountLabelSpec[] SnapshotSlotsAsSpecs(IReadOnlyList<ExtraIconAmountLabelSlot> slots)
        {
            if (slots.Count == 0)
                return [];

            var copy = new ExtraIconAmountLabelSpec[slots.Count];
            for (var i = 0; i < slots.Count; i++)
                copy[i] = new(slots[i]);

            return copy;
        }

        private static void SyncAnchoredSlotHosts(
            Control host,
            List<AnchoredSlotHost> pool,
            string slotNamePrefix,
            ExtraIconAmountLabelSpec[] specs,
            ExtraCornerHostKind hostKind,
            Action<Control> applyHostStyle)
        {
            var occupied = new HashSet<ExtraIconAmountLabelCorner>();

            var writeIndex = 0;
            foreach (var slot in specs)
            {
                if (string.IsNullOrWhiteSpace(slot.Text))
                    continue;

                if (slot.Corner == ExtraIconAmountLabelCorner.Custom &&
                    (slot.CustomRect.Size.X <= 0f || slot.CustomRect.Size.Y <= 0f))
                    continue;

                if (slot.Corner != ExtraIconAmountLabelCorner.Custom)
                    if (!occupied.Add(slot.Corner))
                    {
                        ReportCornerRejectedOnce(host, hostKind, in slot);
                        continue;
                    }

                while (pool.Count <= writeIndex)
                    pool.Add(new());

                var entry = pool[writeIndex];
                var live = GetOrCreateSlotLabel(
                    host,
                    entry,
                    slotNamePrefix,
                    writeIndex,
                    slot.TextMode,
                    applyHostStyle);
                applyHostStyle(live);
                ApplySlotColorOverrides(live, in slot);

                ExtraCornerHostLayout.ApplySlotAlignment(live, hostKind, in slot);
                ExtraCornerHostLayout.ApplySlotBounds(live, hostKind, in slot);
                SetSlotText(live, in slot);
                live.Visible = true;
                writeIndex++;
            }

            for (var j = writeIndex; j < pool.Count; j++)
                if (GodotObject.IsInstanceValid(pool[j].Label))
                    pool[j].Label!.Visible = false;
        }

        private static Control GetOrCreateSlotLabel(
            Control host,
            AnchoredSlotHost entry,
            string slotNamePrefix,
            int slotIndex,
            ExtraIconAmountLabelTextMode textMode,
            Action<Control> applyHostStyle)
        {
            if (GodotObject.IsInstanceValid(entry.Label) && entry.TextMode == textMode &&
                LabelMatchesTextMode(entry.Label, textMode))
                return entry.Label!;

            if (GodotObject.IsInstanceValid(entry.Label))
                entry.Label!.QueueFree();

            var label = CreateSlotLabel($"{slotNamePrefix}{slotIndex}", textMode);
            applyHostStyle(label);
            host.AddChild(label);
            host.MoveChild(label, host.GetChildCount() - 1);
            entry.Label = label;
            entry.TextMode = textMode;
            return label;
        }

        private static Control CreateSlotLabel(string name, ExtraIconAmountLabelTextMode textMode)
        {
            return textMode switch
            {
                ExtraIconAmountLabelTextMode.RichText => new MegaRichTextLabel
                {
                    Name = name,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    ClipContents = true,
                    BbcodeEnabled = true,
                    ScrollActive = false,
                    AutowrapMode = TextServer.AutowrapMode.Off,
                    FitContent = false,
                },
                _ => new MegaLabel
                {
                    Name = name,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    ClipContents = true,
                },
            };
        }

        private static bool LabelMatchesTextMode(Control? label, ExtraIconAmountLabelTextMode textMode)
        {
            return textMode switch
            {
                ExtraIconAmountLabelTextMode.RichText => label is MegaRichTextLabel,
                _ => label is MegaLabel,
            };
        }

        private static void ApplySlotColorOverrides(Control label, in ExtraIconAmountLabelSpec slot)
        {
            if (slot.FontColor is { } fontColor)
                label.AddThemeColorOverride(
                    slot.TextMode == ExtraIconAmountLabelTextMode.RichText
                        ? ThemeConstants.RichTextLabel.DefaultColor
                        : ThemeConstants.Label.FontColor,
                    fontColor);

            if (slot.FontOutlineColor is { } outlineColor)
                label.AddThemeColorOverride(
                    slot.TextMode == ExtraIconAmountLabelTextMode.RichText
                        ? ThemeConstants.RichTextLabel.FontOutlineColor
                        : ThemeConstants.Label.FontOutlineColor,
                    outlineColor);
        }

        private static void SetSlotText(Control label, in ExtraIconAmountLabelSpec slot)
        {
            var text = slot.Text.Trim();
            switch (label)
            {
                case MegaRichTextLabel rich:
                    rich.SetTextAutoSize(AlignRichText(text, slot.Corner));
                    break;
                case MegaLabel plain:
                    plain.SetTextAutoSize(text);
                    break;
            }
        }

        private static string AlignRichText(string text, ExtraIconAmountLabelCorner corner)
        {
            return corner switch
            {
                ExtraIconAmountLabelCorner.TopRight or ExtraIconAmountLabelCorner.BottomRight =>
                    $"[right]{text}[/right]",
                ExtraIconAmountLabelCorner.Custom => $"[center]{text}[/center]",
                _ => text,
            };
        }

        private static void ReportCornerRejectedOnce(
            Control host,
            ExtraCornerHostKind hostKind,
            in ExtraIconAmountLabelSpec slot)
        {
            var hostId = host.GetInstanceId();
            var text = slot.Text.Trim();
            const string reason = "requested corner is already occupied by another extra badge";
            var hostKey = GetHostLogKey(host, hostKind);

            var key = $"{hostKind}:{hostKey}:{slot.Corner}:{reason}";
            lock (CornerErrorSync)
            {
                if (!CornerErrorOnceKeys.Add(key))
                    return;
            }

            RitsuLibFramework.Logger.Warn(
                $"[ExtraCornerAmountLabels] Rejected badge slot on {hostKind} host ({hostKey}, nodeId={hostId}): corner={slot.Corner}, text='{text}', reason={reason}.");
        }

        private static string GetHostLogKey(Control host, ExtraCornerHostKind hostKind)
        {
            return hostKind switch
            {
                ExtraCornerHostKind.Power when host is NPower power =>
                    SafeReadModel(power)?.Id.ToString() ?? power.GetType().FullName ?? power.Name,
                ExtraCornerHostKind.Relic when host is NRelicInventoryHolder relic =>
                    relic.Relic.Model.Id.ToString(),
                ExtraCornerHostKind.Intent =>
                    (IntentIntentField?.GetValue(host) as AbstractIntent)?.GetType().FullName ??
                    host.GetType().FullName ?? host.Name,
                _ => host.GetType().FullName ?? host.Name,
            };
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
            object provider)
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
            object provider)
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
            object provider)
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
            public Control? Label;
            public ExtraIconAmountLabelTextMode TextMode;
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
