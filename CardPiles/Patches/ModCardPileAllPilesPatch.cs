using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Appends <see cref="ModCardPileScope.CombatOnly" /> piles to
    ///     <see cref="PlayerCombatState.AllPiles" /> so that vanilla code paths that iterate combat piles
    ///     (enumeration, <c>AfterCombatEnd</c>, broadcast helpers) transparently include mod piles.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A Postfix is used instead of a Transpiler (unlike baselib's <c>SpecialPileInCombat</c>) so both
    ///         libraries can coexist without IL conflicts. Whatever vanilla or baselib produced is treated as
    ///         the base, and ritsulib's piles are concatenated on top.
    ///     </para>
    ///     <para>
    ///         The underlying <c>_piles</c> field is also updated via reflection when present so subsequent
    ///         getter calls see the combined array without reallocating per access; when the field is absent
    ///         (future vanilla refactors), the postfix still works by replacing <c>__result</c>.
    ///     </para>
    /// </remarks>
    public sealed class ModCardPileAllPilesPatch : IPatchMethod
    {
        private static readonly FieldInfo? PilesField =
            typeof(PlayerCombatState).GetField("_piles", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <inheritdoc />
        public static string PatchId => "ritsulib_player_combat_state_all_piles_append";

        /// <inheritdoc />
        public static string Description =>
            "Append ritsulib CombatOnly mod piles to PlayerCombatState.AllPiles without transpiling";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PlayerCombatState), "get_" + nameof(PlayerCombatState.AllPiles))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Merges mod piles into <see cref="PlayerCombatState.AllPiles" />'s return value.
        /// </summary>
        public static void Postfix(PlayerCombatState __instance, ref IReadOnlyList<CardPile> __result)
        {
            var modPiles = ModCardPileStorage.GetCombatPiles(__instance);
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

            PilesField?.SetValue(__instance, combined);
            __result = combined;
        }
        // ReSharper restore InconsistentNaming

        private static bool ContainsAll(IReadOnlyList<CardPile> haystack, IReadOnlyCollection<ModCardPile> needles)
        {
            return needles.Select(needle => haystack.Any(t => ReferenceEquals(t, needle))).All(found => found);
        }
    }
}
