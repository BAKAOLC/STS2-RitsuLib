using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Appends <see cref="ModCardPileScope.CombatOnly" /> piles to
    ///     <see cref="PlayerCombatState.AllPiles" /> so that vanilla code paths that iterate combat piles
    ///     (enumeration, <c>AfterCombatEnd</c>, broadcast helpers) transparently include mod piles.
    ///     将 <see cref="ModCardPileScope.CombatOnly" /> 牌堆追加到
    ///     <see cref="PlayerCombatState.AllPiles" />，使枚举 combat pile 的原版代码路径
    ///     （enumeration、<c>AfterCombatEnd</c>、broadcast helper）透明包含 mod 牌堆。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A Postfix is used instead of a Transpiler (unlike baselib's <c>SpecialPileInCombat</c>) so both
    ///         libraries can coexist without IL conflicts. Whatever vanilla or baselib produced is treated as
    ///         the base, and ritsulib's piles are concatenated on top.
    ///     </para>
    ///     <para>
    ///         The underlying <c>_piles</c> field is updated when present (publicized STS2) so subsequent
    ///         getter calls see the combined array without reallocating per access; otherwise the postfix
    ///         still works by replacing <c>__result</c>.
    ///     </para>
    ///     <para>
    ///         这里使用 Postfix 而不是 Transpiler（不同于 baselib 的 <c>SpecialPileInCombat</c>），
    ///         让两个库可以共存而不发生 IL 冲突。原版或 baselib 生成的结果会作为基础，
    ///         ritsulib 的牌堆再拼接到其后。
    ///     </para>
    ///     <para>
    ///         当底层 <c>_piles</c> 字段存在时（publicized STS2），会同步更新该字段，使后续
    ///         getter 调用能看到合并后的数组，而不必每次访问都重新分配；否则 postfix
    ///         仍通过替换 <c>__result</c> 工作。
    ///     </para>
    /// </remarks>
    public sealed class ModCardPileAllPilesPatch : IPatchMethod
    {
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
            return [new(typeof(PlayerCombatState), nameof(PlayerCombatState.AllPiles), MethodType.Getter)];
        }

        /// <summary>
        ///     Merges mod piles into <see cref="PlayerCombatState.AllPiles" />'s return value.
        ///     将 mod 牌堆合并进 <see cref="PlayerCombatState.AllPiles" /> 的返回值。
        /// </summary>
        public static void Postfix(PlayerCombatState __instance, ref IReadOnlyList<CardPile> __result)
        {
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
            return needles.Select(needle => haystack.Any(t => ReferenceEquals(t, needle))).All(found => found);
        }
    }
}
