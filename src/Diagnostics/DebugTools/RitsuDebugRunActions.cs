using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Diagnostics.DebugTools
{
    internal static class RitsuDebugRunActions
    {
        private const int MaxAncientOptionCount = 128;

        internal const string EnterRoomActionId = "run.room.enter";
        internal const string EnterEncounterActionId = "run.encounter.enter";
        internal const string EnterEventActionId = "run.event.enter";

        private static readonly PropertyInfo? EventOwnerProperty = typeof(EventModel).GetProperty(
            nameof(EventModel.Owner),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        internal static void RegisterBuiltInActions()
        {
            RitsuDebugActionProtocol.Register<RoomPayload>(
                EnterRoomActionId,
                ValidateEnterRoom,
                ExecuteEnterRoomAsync);
            RitsuDebugActionProtocol.Register<ModelPayload>(
                EnterEncounterActionId,
                ValidateEnterEncounter,
                ExecuteEnterEncounterAsync);
            RitsuDebugActionProtocol.Register<EventPayload>(
                EnterEventActionId,
                ValidateEnterEvent,
                ExecuteEnterEventAsync);
        }

        internal static RitsuDebugActionSubmission SubmitEnterRoom(
            Player requester,
            RoomType roomType)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                EnterRoomActionId,
                requester,
                requester,
                new RoomPayload(roomType.ToString()));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitEnterEncounter(
            Player requester,
            string encounterId)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                EnterEncounterActionId,
                requester,
                requester,
                new ModelPayload(encounterId));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static RitsuDebugActionSubmission SubmitEnterEvent(
            Player requester,
            Player historyPlayer,
            string eventId,
            string? ancientOption)
        {
            var envelope = RitsuDebugActionProtocol.CreateEnvelope(
                EnterEventActionId,
                requester,
                historyPlayer,
                new EventPayload(eventId, ancientOption));
            return RitsuDebugActionProtocol.Submit(requester, envelope);
        }

        internal static bool TryResolveEncounter(
            string input,
            out EncounterModel encounter,
            out RitsuDebugActionFeedback feedback)
        {
            return TryResolveModel(ModelDb.AllEncounters, input, "encounter", out encounter, out feedback);
        }

        internal static bool TryResolveEvent(
            string input,
            out EventModel eventModel,
            out RitsuDebugActionFeedback feedback)
        {
            return TryResolveModel(
                ModelDb.AllEvents.Concat(ModelDb.AllAncients).DistinctBy(static model => model.Id),
                input,
                "event",
                out eventModel,
                out feedback);
        }

        internal static bool TryGetAvailableAncientOptions(
            AncientEventModel canonical,
            Player player,
            out EventOption[] options,
            out RitsuDebugActionFeedback feedback)
        {
            ArgumentNullException.ThrowIfNull(canonical);
            ArgumentNullException.ThrowIfNull(player);
            options = [];
            if (EventOwnerProperty?.SetMethod == null)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "run.ancientUnsupportedVersion",
                    "This game version cannot determine which Ancient options are available for the selected player.");
                return false;
            }

            try
            {
                var preview = (AncientEventModel)canonical.ToMutable();
                EventOwnerProperty.SetValue(preview, player);
                var candidates = preview.AllPossibleOptions
                    .Where(static option => option is { IsLocked: false })
                    .Take(MaxAncientOptionCount + 1)
                    .ToArray();
                if (candidates.Length > MaxAncientOptionCount)
                {
                    feedback = RitsuDebugActionFeedback.Create(
                        "run.ancientTooManyOptions",
                        "Ancient event {0} exposes more than {1} options.",
                        canonical.Id,
                        MaxAncientOptionCount);
                    return false;
                }

                options = candidates
                    .DistinctBy(GetAncientOptionToken, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                feedback = default;
                return true;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugTools] Could not resolve Ancient options for '{canonical.Id}': {ex}");
                feedback = RitsuDebugActionFeedback.Create(
                    "run.ancientOptionsUnavailable",
                    "Available Ancient options could not be determined for the selected player.");
                return false;
            }
        }

        private static RitsuDebugActionCheck ValidateEnterRoom(
            RitsuDebugActionContext context,
            RoomPayload payload)
        {
            if (!TryRequireActiveRun(out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (!Enum.TryParse<RoomType>(payload.RoomType, true, out var roomType) ||
                roomType == RoomType.Unassigned)
                return RitsuDebugActionCheck.Fail(
                    "run.invalidRoomType",
                    "Unknown or unsupported room type '{0}'.",
                    payload.RoomType);
            return RitsuDebugActionCheck.Ok;
        }

        private static RitsuDebugActionCheck ValidateEnterEncounter(
            RitsuDebugActionContext context,
            ModelPayload payload)
        {
            if (!TryRequireActiveRun(out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return TryResolveEncounter(payload.ModelId, out _, out feedback)
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(feedback);
        }

        private static RitsuDebugActionCheck ValidateEnterEvent(
            RitsuDebugActionContext context,
            EventPayload payload)
        {
            if (!TryRequireActiveRun(out var feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (!TryResolveEvent(payload.EventId, out var eventModel, out feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            if (payload.AncientOption == null)
                return RitsuDebugActionCheck.Ok;
            if (eventModel is not AncientEventModel ancient)
                return RitsuDebugActionCheck.Fail(
                    "run.ancientOptionOnly",
                    "An Ancient option may be specified only for an Ancient event.");
            if (!TryGetAvailableAncientOptions(ancient, context.Target, out var options, out feedback))
                return RitsuDebugActionCheck.Fail(feedback);
            return options.Any(option =>
                GetAncientOptionToken(option).Equals(payload.AncientOption, StringComparison.OrdinalIgnoreCase))
                ? RitsuDebugActionCheck.Ok
                : RitsuDebugActionCheck.Fail(
                    "run.ancientOptionMissing",
                    "Ancient event {0} has no option '{1}'.",
                    ancient.Id,
                    payload.AncientOption);
        }

        private static async Task<string> ExecuteEnterRoomAsync(
            RitsuDebugActionContext context,
            RoomPayload payload)
        {
            _ = Enum.TryParse<RoomType>(payload.RoomType, true, out var roomType);
            await RunManager.Instance.EnterRoomDebug(roomType);
            return $"Entered the selected room: {roomType}.";
        }

        private static async Task<string> ExecuteEnterEncounterAsync(
            RitsuDebugActionContext context,
            ModelPayload payload)
        {
            _ = TryResolveEncounter(payload.ModelId, out var canonical, out _);
            var encounter = canonical.ToMutable();
            encounter.DebugRandomizeRng();
            await RunManager.Instance.EnterRoomDebug(
                RoomType.Monster,
                MapPointType.Unassigned,
                encounter);
            return $"Entered encounter {canonical.Id}.";
        }

        private static async Task<string> ExecuteEnterEventAsync(
            RitsuDebugActionContext context,
            EventPayload payload)
        {
            _ = TryResolveEvent(payload.EventId, out var eventModel, out _);
            var mapPointType = eventModel is AncientEventModel
                ? MapPointType.Ancient
                : MapPointType.Unknown;
            var option = payload.AncientOption?.ToUpperInvariant();
            var room = new EventRoom(eventModel)
            {
                OnStart = option == null
                    ? null
                    : model => ((AncientEventModel)model).DebugOption = option,
            };
            context.Target.RunState.AppendToMapPointHistory(mapPointType, RoomType.Event, eventModel.Id);
            await RunManager.Instance.EnterRoom(room);
            return $"Entered event {eventModel.Id}.";
        }

        internal static string GetAncientOptionToken(EventOption option)
        {
            return option.TextKey.Split('.').Last();
        }

        private static bool TryRequireActiveRun(out RitsuDebugActionFeedback feedback)
        {
            if (!RunManager.Instance.IsInProgress)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    "run.notInProgress",
                    "A run is not currently in progress.");
                return false;
            }

            feedback = default;
            return true;
        }

        private static bool TryResolveModel<TModel>(
            IEnumerable<TModel> candidates,
            string input,
            string kind,
            out TModel model,
            out RitsuDebugActionFeedback feedback)
            where TModel : AbstractModel
        {
            model = null!;
            if (string.IsNullOrWhiteSpace(input) || input.Length > 128)
            {
                feedback = RitsuDebugActionFeedback.Create(
                    $"model.{kind}IdInvalid",
                    $"The {kind} ID is empty or too long.");
                return false;
            }

            var candidateArray = candidates as TModel[] ?? candidates.ToArray();
            var full = candidateArray
                .Where(candidate => candidate.Id.ToString().Equals(input, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            var matches = full.Length > 0
                ? full
                : candidateArray
                    .Where(candidate => candidate.Id.Entry.Equals(input, StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToArray();
            if (matches.Length == 1)
            {
                model = matches[0];
                feedback = default;
                return true;
            }

            feedback = matches.Length == 0
                ? RitsuDebugActionFeedback.Create(
                    $"model.{kind}Unknown",
                    $"Unknown {kind} '{{0}}'.",
                    input)
                : RitsuDebugActionFeedback.Create(
                    $"model.{kind}Ambiguous",
                    $"The {kind} ID '{{0}}' is ambiguous; use the full model ID.",
                    input);
            return false;
        }

        internal readonly record struct RoomPayload(string RoomType);

        internal readonly record struct ModelPayload(string ModelId);

        internal readonly record struct EventPayload(string EventId, string? AncientOption);
    }
}
