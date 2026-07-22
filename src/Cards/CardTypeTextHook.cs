using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Capabilities;

namespace STS2RitsuLib.Cards
{
    /// <summary>
    ///     Dispatches BaseLib-compatible card type text modifiers from cards, model capabilities, run/combat hook
    ///     listeners, and registered global modifiers.
    ///     从卡牌、模型能力、跑局/战斗 hook listener 和已注册的全局修改器分发与 BaseLib 兼容的卡牌类型文本修改。
    /// </summary>
    public static class CardTypeTextHook
    {
        private const string TypeArgumentName = "Type";
        private static readonly ModelHookListenerRegistry<ICardTypeTextModifier> GlobalModifiers = new();

        /// <summary>
        ///     Registers a process-wide modifier. Model-owned effects should usually implement
        ///     <see cref="ICardTypeTextModifier" /> directly.
        ///     注册一个进程级修改器。模型所属效果通常应直接实现 <see cref="ICardTypeTextModifier" />。
        /// </summary>
        public static void RegisterGlobalModifier(ICardTypeTextModifier modifier)
        {
            GlobalModifiers.Register(modifier);
        }

        internal static LocString Apply(LocString originalPlaqueText, CardModel card)
        {
            var modifiers = GetTypeModifiers(card);
            var modifiersByWrapMode = modifiers.ToLookup(ReferencesTypeArgument);

            foreach (var modifier in modifiersByWrapMode[false])
                originalPlaqueText = modifier;

            var previousTypeText = originalPlaqueText;
            foreach (var modifier in modifiersByWrapMode[true])
            {
                modifier.Add(TypeArgumentName, previousTypeText);
                previousTypeText = modifier;
            }

            return previousTypeText;
        }

        private static IEnumerable<LocString> GetTypeModifiers(CardModel card)
        {
            if (card is ICustomTypeTextCard customTypeTextCard)
                foreach (var modifier in customTypeTextCard.GetTypeModifiers())
                    yield return modifier;

            foreach (var capability in ModelCapabilityHost.GetCapabilities<ICardTypeTextModifier>(card))
            foreach (var modifier in capability.GetTypeModifiers(card))
                yield return modifier;

            foreach (var source in IterateHookModifiers(card))
            foreach (var modifier in source.GetTypeModifiers(card))
                yield return modifier;
        }

        private static IEnumerable<ICardTypeTextModifier> IterateHookModifiers(CardModel card)
        {
            HashSet<ICardTypeTextModifier> seen = new(ReferenceEqualityComparer.Instance);
            foreach (var capability in ModelCapabilityHost.GetCapabilities<ICardTypeTextModifier>(card))
                seen.Add(capability);

            if (card.RunState is { } runState)
            {
                foreach (var entry in ModelHookListenerDispatcher.FromRun(
                             runState,
                             card.CombatState,
                             GlobalModifiers))
                    if (seen.Add(entry.Listener))
                        yield return entry.Listener;
                yield break;
            }

            if (card.CombatState is { } combatState)
            {
                foreach (var entry in ModelHookListenerDispatcher.FromCombat(combatState, GlobalModifiers))
                    if (seen.Add(entry.Listener))
                        yield return entry.Listener;
                yield break;
            }

            foreach (var modifier in GlobalModifiers.Snapshot())
                if (seen.Add(modifier))
                    yield return modifier;
        }

        private static bool ReferencesTypeArgument(LocString modifier)
        {
            return modifier.GetRawText().Contains("{" + TypeArgumentName + "}", StringComparison.Ordinal);
        }
    }
}
