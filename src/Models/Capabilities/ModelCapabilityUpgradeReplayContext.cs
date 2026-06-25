using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    internal static class ModelCapabilityUpgradeReplayContext
    {
        private static readonly AsyncLocal<int> CardDeserializeReplayDepth = new();

        private static readonly ConditionalWeakTable<CardModel, DeferredCapabilityImport> DeferredImports = [];

        private static readonly ConditionalWeakTable<CardModel, DeferredModelSavedDataImport>
            DeferredModelSavedDataImports =
                [];

        public static IDisposable BeginCardDeserializeReplay()
        {
            CardDeserializeReplayDepth.Value++;
            return new ReplayScope();
        }

        public static bool TryDeferCardCapabilityImport(AbstractModel model, ModelCapabilitySaveDocument? document)
        {
            if (CardDeserializeReplayDepth.Value <= 0 || model is not CardModel card)
                return false;

            DeferredImports.Remove(card);
            DeferredImports.Add(card, new(document));
            return true;
        }

        public static bool TryDeferCardModelSavedDataImport(AbstractModel model, ModelSavedDataDocument? document)
        {
            if (CardDeserializeReplayDepth.Value <= 0 || model is not CardModel card)
                return false;

            DeferredModelSavedDataImports.Remove(card);
            DeferredModelSavedDataImports.Add(card, new(document));
            return true;
        }

        public static void FlushDeferredCardModelSavedDataImport(CardModel? card)
        {
            if (card == null || !DeferredModelSavedDataImports.TryGetValue(card, out var deferredImport))
                return;

            DeferredModelSavedDataImports.Remove(card);
            try
            {
                ModelSavedDataRuntime.AttachDocumentImmediate(card, deferredImport.Document);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModelSavedData] Failed to import deferred card model data for {card.Id}: {ex.Message}");
            }
        }

        public static void FlushDeferredCardCapabilityImport(CardModel? card)
        {
            if (card == null || !DeferredImports.TryGetValue(card, out var deferredImport))
                return;

            DeferredImports.Remove(card);
            try
            {
                ModelCapabilities.ImportImmediate(card, deferredImport.Document);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModelCapabilities] Failed to import deferred card capability data for {card.Id}: {ex.Message}");
            }
        }

        private sealed class ReplayScope : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                CardDeserializeReplayDepth.Value = Math.Max(0, CardDeserializeReplayDepth.Value - 1);
            }
        }

        private sealed class DeferredCapabilityImport(ModelCapabilitySaveDocument? document)
        {
            public ModelCapabilitySaveDocument? Document { get; } = document;
        }

        private sealed class DeferredModelSavedDataImport(ModelSavedDataDocument? document)
        {
            public ModelSavedDataDocument? Document { get; } = document;
        }
    }
}
