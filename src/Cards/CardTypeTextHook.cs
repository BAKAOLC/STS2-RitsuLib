using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Capabilities;

namespace STS2RitsuLib.Cards
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies BaseLib-compatible card-type text modifiers supplied by cards, model capabilities, run or combat
    ///         listeners, and registered global modifiers.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         应用由卡牌、模型能力、一局游戏或战斗监听器以及已注册全局修改器提供的 BaseLib 兼容卡牌类型文本修改。
    ///     </para>
    /// </summary>
    public static class CardTypeTextHook
    {
        private const string TypeArgumentName = "Type";
        private static readonly ModelHookListenerRegistry<ICardTypeTextModifier> GlobalModifiers = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a process-wide modifier. Effects owned by a model should normally implement
        ///         <see cref="ICardTypeTextModifier" /> directly.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个进程级修改器。由模型持有的效果通常应直接实现 <see cref="ICardTypeTextModifier" />。
        ///     </para>
        /// </summary>
        public static void RegisterGlobalModifier(ICardTypeTextModifier modifier)
        {
            GlobalModifiers.Register(modifier);
        }

        internal static LocString Apply(LocString originalPlaqueText, CardModel card)
        {
            var modifiers = GetTypeModifiers(card);
            var replacements = new List<ModifierEntry>();
            var wrappers = new List<ModifierEntry>();
            foreach (var entry in modifiers)
            {
                try
                {
                    (ReferencesTypeArgument(entry.Modifier) ? wrappers : replacements).Add(entry);
                }
                catch (Exception ex)
                {
                    WarnFailure(entry.Source, card, ex);
                }
            }

            foreach (var entry in replacements)
                originalPlaqueText = entry.Modifier;

            var previousTypeText = originalPlaqueText;
            foreach (var entry in wrappers)
            {
                try
                {
                    var wrapper = new LocString(entry.Modifier.LocTable, entry.Modifier.LocEntryKey);
                    wrapper.AddVariablesFrom(entry.Modifier);
                    wrapper.Add(TypeArgumentName, previousTypeText);
                    previousTypeText = wrapper;
                }
                catch (Exception ex)
                {
                    WarnFailure(entry.Source, card, ex);
                }
            }

            return previousTypeText;
        }

        private static IEnumerable<ModifierEntry> GetTypeModifiers(CardModel card)
        {
            if (card is ICustomTypeTextCard customTypeTextCard)
                foreach (var modifier in GetTypeModifiersSafely(
                             card,
                             card,
                             customTypeTextCard.GetTypeModifiers))
                    yield return new(modifier, card);

            HashSet<ICardTypeTextModifier> seen = new(ReferenceEqualityComparer.Instance);
            foreach (var capability in ModelCapabilityHost.GetCapabilities<ICardTypeTextModifier>(card))
            {
                seen.Add(capability);
                foreach (var modifier in GetTypeModifiersSafely(
                             capability,
                             card,
                             () => capability.GetTypeModifiers(card)))
                    yield return new(modifier, capability);
            }

            foreach (var source in IterateHookModifiers(card, seen))
            foreach (var modifier in GetTypeModifiersSafely(
                         source,
                         card,
                         () => source.GetTypeModifiers(card)))
                yield return new(modifier, source);
        }

        private static IEnumerable<ICardTypeTextModifier> IterateHookModifiers(
            CardModel card,
            HashSet<ICardTypeTextModifier> seen)
        {
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

        private static IReadOnlyList<LocString> GetTypeModifiersSafely(
            object source,
            CardModel card,
            Func<IEnumerable<LocString>> getModifiers)
        {
            try
            {
                return getModifiers()?.Where(static modifier => modifier != null).ToArray() ?? [];
            }
            catch (Exception ex)
            {
                WarnFailure(source, card, ex);
                return [];
            }
        }

        private static void WarnFailure(object source, CardModel card, Exception exception)
        {
            if (source is IModelCapability capability)
                ModelCapabilityDiagnostics.WarnFailure("card display/type-text", card, capability, exception);
            else
                RitsuLibFramework.Logger.Warn(
                    $"[CardTypeText] Modifier source '{source.GetType().FullName}' failed for {card.Id}: " +
                    exception);
        }

        private readonly record struct ModifierEntry(LocString Modifier, object Source);
    }
}
