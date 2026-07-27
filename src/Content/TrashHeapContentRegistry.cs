using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Content
{
    internal static class TrashHeapContentRegistry
    {
        private static readonly Lock SyncRoot = new();
        private static readonly List<RegisteredModel> Cards = [];
        private static readonly List<RegisteredModel> Relics = [];

        internal static bool RegisterCard(Type cardType, string ownerModId)
        {
            return Register(Cards, cardType, ownerModId);
        }

        internal static bool RegisterRelic(Type relicType, string ownerModId)
        {
            return Register(Relics, relicType, ownerModId);
        }

        internal static CardModel[] AppendCards(CardModel[] vanillaCards)
        {
            ArgumentNullException.ThrowIfNull(vanillaCards);
            return AppendRegisteredModels(vanillaCards, GetSnapshot(Cards), "card");
        }

        internal static RelicModel[] AppendRelics(RelicModel[] vanillaRelics)
        {
            ArgumentNullException.ThrowIfNull(vanillaRelics);
            return AppendRegisteredModels(vanillaRelics, GetSnapshot(Relics), "relic");
        }

        internal static void ValidateFrozenRegistrations()
        {
            foreach (var registration in GetSnapshot(Cards))
                RegistrationFreezeDiagnostics.WarnMissingModelType(
                    "TrashHeap",
                    registration.OwnerModId,
                    "Trash Heap card candidate",
                    registration.ModelType,
                    typeof(CardModel));

            foreach (var registration in GetSnapshot(Relics))
                RegistrationFreezeDiagnostics.WarnMissingModelType(
                    "TrashHeap",
                    registration.OwnerModId,
                    "Trash Heap relic candidate",
                    registration.ModelType,
                    typeof(RelicModel));
        }

        internal static void ClearForTests()
        {
            lock (SyncRoot)
            {
                Cards.Clear();
                Relics.Clear();
            }
        }

        private static bool Register(List<RegisteredModel> registrations, Type modelType, string ownerModId)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerModId);

            lock (SyncRoot)
            {
                if (registrations.Any(registration => registration.ModelType == modelType))
                    return false;

                registrations.Add(new(modelType, ownerModId.Trim()));
                return true;
            }
        }

        private static RegisteredModel[] GetSnapshot(List<RegisteredModel> registrations)
        {
            lock (SyncRoot)
            {
                return [.. registrations];
            }
        }

        private static TModel[] AppendRegisteredModels<TModel>(
            TModel[] vanillaModels,
            IReadOnlyList<RegisteredModel> registrations,
            string contentKind)
            where TModel : AbstractModel
        {
            if (registrations.Count == 0)
                return vanillaModels;

            var combined = new List<TModel>(vanillaModels.Length + registrations.Count);
            combined.AddRange(vanillaModels);
            var existingIds = vanillaModels.Select(static model => model.Id).ToHashSet();

            foreach (var registration in registrations)
            {
                TModel? model;
                try
                {
                    model = ModelDb.GetByIdOrNull<TModel>(ModelDb.GetId(registration.ModelType));
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.CreateLogger(registration.OwnerModId).Warn(
                        $"[TrashHeap] Failed to resolve registered {contentKind} '{registration.ModelType.FullName}': {ex}");
                    continue;
                }

                if (model == null || !existingIds.Add(model.Id))
                    continue;

                combined.Add(model);
            }

            return combined.Count == vanillaModels.Length ? vanillaModels : [.. combined];
        }

        private readonly record struct RegisteredModel(Type ModelType, string OwnerModId);
    }
}
