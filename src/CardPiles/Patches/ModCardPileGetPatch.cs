using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes <see cref="CardPile.Get" /> for registered dynamic pile types through
    ///         <see cref="ModCardPileStorage" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将已注册动态牌堆类型的 <see cref="CardPile.Get" /> 调用交由
    ///         <see cref="ModCardPileStorage" /> 处理。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Unregistered values continue through the original implementation.</para>
    ///     <para xml:lang="zh-CN">未注册的值继续交由原始实现处理。</para>
    /// </remarks>
    internal sealed class ModCardPileGetPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_card_pile_get_mod_route";
        public static string Description => "Route CardPile.Get to ModCardPileStorage for minted mod PileType values";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardPile), nameof(CardPile.Get))];
        }

        public static bool Prefix(PileType type, Player player, ref CardPile? __result)
        {
            if (!ModCardPileRegistry.IsModPileType(type))
                return true;

            __result = ModCardPileStorage.Resolve(type, player);
            return false;
        }
    }
}
