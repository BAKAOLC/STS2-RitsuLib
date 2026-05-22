using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    internal static class ModelComponentUpgradeReplayContext
    {
        private static readonly AsyncLocal<int> CardDeserializeReplayDepth = new();

        private static readonly ConditionalWeakTable<CardModel, DeferredComponentImport> DeferredImports = [];

        public static IDisposable BeginCardDeserializeReplay()
        {
            CardDeserializeReplayDepth.Value++;
            return new ReplayScope();
        }

        public static bool TryDeferCardComponentImport(AbstractModel model, ModelComponentSaveDocument? document)
        {
            if (CardDeserializeReplayDepth.Value <= 0 || model is not CardModel card)
                return false;

            DeferredImports.Remove(card);
            DeferredImports.Add(card, new(document));
            return true;
        }

        public static void FlushDeferredCardComponentImport(CardModel? card)
        {
            if (card == null || !DeferredImports.TryGetValue(card, out var deferredImport))
                return;

            DeferredImports.Remove(card);
            try
            {
                ModelComponents.ImportImmediate(card, deferredImport.Document);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ModelComponents] Failed to import deferred card component data for {card.Id}: {ex.Message}");
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

        private sealed class DeferredComponentImport(ModelComponentSaveDocument? document)
        {
            public ModelComponentSaveDocument? Document { get; } = document;
        }
    }
}
