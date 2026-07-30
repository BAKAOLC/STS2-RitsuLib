using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds <see cref="ModCardPileScope.CombatOnly" /> mod piles to
    ///         <see cref="PlayerCombatState.AllPiles" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="ModCardPileScope.CombatOnly" /> 模组牌堆加入
    ///         <see cref="PlayerCombatState.AllPiles" />。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         The postfix preserves the existing result and appends missing RitsuLib piles.
    ///     </para>
    ///     <para xml:lang="en">
    ///         The combined array is also stored in <c>_piles</c> so later getter calls reuse it.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         后置补丁会保留现有结果，并在其后追加缺失的 RitsuLib 牌堆。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         合并后的数组也会写入 <c>_piles</c>，供后续属性访问复用。
    ///     </para>
    /// </remarks>
    internal sealed class ModCardPileAllPilesPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_player_combat_state_all_piles_append";

        public static string Description =>
            "Append ritsulib CombatOnly mod piles to PlayerCombatState.AllPiles without transpiling";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PlayerCombatState), nameof(PlayerCombatState.AllPiles), MethodType.Getter)];
        }

        public static void Postfix(PlayerCombatState __instance, ref IReadOnlyList<CardPile> __result)
        {
            var definitions = ModCardPileRegistry.GetCombatDefinitionsSnapshot();
            if (definitions.Length == 0 || ContainsAllDefinitions(__result, definitions))
                return;

            var modPiles = ModCardPileStorage.GetOrCreateCombatPiles(__instance);
            if (modPiles.Count == 0)
                return;

            if (ContainsAll(__result, modPiles))
                return;

            var combined = new CardPile[__result.Count + modPiles.Count];
            for (var i = 0; i < __result.Count; i++)
                combined[i] = __result[i];
            var j = __result.Count;
            foreach (var pile in modPiles)
                combined[j++] = pile;

            __instance._piles = combined;
            __result = combined;
        }

        private static bool ContainsAll(IReadOnlyList<CardPile> haystack, IReadOnlyCollection<ModCardPile> needles)
        {
            return needles.Select(needle => haystack.Any(cardPile => ReferenceEquals(cardPile, needle)))
                .All(found => found);
        }

        private static bool ContainsAllDefinitions(
            IReadOnlyList<CardPile> haystack,
            IReadOnlyList<ModCardPileDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                var found = false;
                foreach (var cardPile in haystack)
                {
                    if (cardPile is not ModCardPile pile || !ReferenceEquals(pile.Definition, definition))
                        continue;

                    found = true;
                    break;
                }

                if (!found)
                    return false;
            }

            return true;
        }
    }
}
