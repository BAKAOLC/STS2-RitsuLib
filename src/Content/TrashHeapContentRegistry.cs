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

            var candidate = new RegisteredModel(modelType, ownerModId.Trim());
            lock (SyncRoot)
            {
                var existingIndex = registrations.FindIndex(registration => registration.ModelType == modelType);
                if (existingIndex >= 0)
                {
                    var existing = registrations[existingIndex];
                    if (RegisteredModelComparer.Instance.Compare(candidate, existing) >= 0)
                        return false;

                    registrations[existingIndex] = candidate;
                    return true;
                }

                registrations.Add(candidate);
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
            var combined = new List<TModel>(vanillaModels.Length + registrations.Count);
            combined.AddRange(vanillaModels);

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

                if (model == null)
                    continue;

                combined.Add(model);
            }

            combined.Sort(ModelComparer<TModel>.Instance);

            var seenIds = new HashSet<ModelId>();
            return [.. combined.Where(model => seenIds.Add(model.Id))];
        }

        private readonly record struct RegisteredModel(Type ModelType, string OwnerModId);

        private sealed class RegisteredModelComparer : IComparer<RegisteredModel>
        {
            internal static RegisteredModelComparer Instance { get; } = new();

            public int Compare(RegisteredModel x, RegisteredModel y)
            {
                var result = StringComparer.Ordinal.Compare(x.OwnerModId, y.OwnerModId);
                if (result != 0)
                    return result;

                return StringComparer.Ordinal.Compare(
                    x.ModelType.AssemblyQualifiedName ?? x.ModelType.FullName ?? x.ModelType.Name,
                    y.ModelType.AssemblyQualifiedName ?? y.ModelType.FullName ?? y.ModelType.Name);
            }
        }

        private sealed class ModelComparer<TModel> : IComparer<TModel>
            where TModel : AbstractModel
        {
            internal static ModelComparer<TModel> Instance { get; } = new();

            public int Compare(TModel? x, TModel? y)
            {
                if (ReferenceEquals(x, y))
                    return 0;
                if (x is null)
                    return -1;
                if (y is null)
                    return 1;

                var result = x.Id.CompareTo(y.Id);
                if (result != 0)
                    return result;

                return StringComparer.Ordinal.Compare(
                    x.GetType().AssemblyQualifiedName ?? x.GetType().FullName ?? x.GetType().Name,
                    y.GetType().AssemblyQualifiedName ?? y.GetType().FullName ?? y.GetType().Name);
            }
        }
    }
}
