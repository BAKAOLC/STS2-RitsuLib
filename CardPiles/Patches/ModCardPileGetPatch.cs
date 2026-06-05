using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Short-circuits <see cref="CardPile.Get" /> for mod-minted <see cref="PileType" /> values, returning
    ///     the per-<see cref="Player" /> / per-combat instance resolved by <see cref="ModCardPileStorage" />.
    ///     Non-mod values defer to vanilla (and any other Prefix, such as baselib's <c>GetCombatPile</c>).
    ///     对 mod-minted <see cref="PileType" /> 值短路 <see cref="CardPile.Get" />，返回由
    ///     <see cref="ModCardPileStorage" /> 解析出的 per-<see cref="Player" /> / per-combat 实例。
    ///     非 mod 值交给原版（以及任何其它 Prefix，例如 baselib 的 <c>GetCombatPile</c>）。
    /// </summary>
    /// <remarks>
    ///     Without this patch the vanilla switch falls through to <c>ArgumentOutOfRangeException</c> whenever a
    ///     caller uses a mod-minted pile id, which is why this must run as a Prefix rather than a Postfix.
    ///     没有此 patch 时，只要调用方使用 mod-minted pile id，原版 switch 就会落入
    ///     <c>ArgumentOutOfRangeException</c>；因此它必须作为 Prefix 而不是 Postfix 运行。
    /// </remarks>
    public sealed class ModCardPileGetPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_card_pile_get_mod_route";

        /// <inheritdoc />
        public static string Description => "Route CardPile.Get to ModCardPileStorage for minted mod PileType values";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardPile), nameof(CardPile.Get))];
        }

        /// <summary>
        ///     Resolves mod piles before vanilla's switch throws; returns <c>true</c> to continue vanilla
        ///     execution for unrecognized values.
        ///     在原版 switch 抛异常前解析 mod pile；对未识别值返回 <c>true</c> 以继续执行原版逻辑。
        /// </summary>
        public static bool Prefix(PileType type, Player player, ref CardPile? __result)
        {
            if (!ModCardPileRegistry.IsModPileType(type))
                return true;

            __result = ModCardPileStorage.Resolve(type, player);
            return false;
        }
    }
}
