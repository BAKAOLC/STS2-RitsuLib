using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Extension helpers for working with minted mod <see cref="PileType" /> values.
    ///     用于处理 minted mod <see cref="PileType" /> 值的 extension helper。
    /// </summary>
    public static class ModCardPileExtensions
    {
        /// <summary>
        ///     Convenience: minted <see cref="PileType" /> value for <paramref name="pileId" />, intended for
        ///     vanilla-style call sites such as <c>player.GetPile(id.GetModCardPileType())</c>.
        ///     便捷方法：返回 <paramref name="pileId" /> 的 minted <see cref="PileType" /> 值，供
        ///     <c>player.GetPile(id.GetModCardPileType())</c> 等原版风格调用点使用。
        /// </summary>
        public static PileType GetModCardPileType(this string pileId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pileId);
            return ModCardPileRegistry.GetPileType(pileId);
        }

        /// <summary>
        ///     Tries to reverse-map a minted mod <see cref="PileType" /> value to its registered string id.
        ///     尝试将 minted mod <see cref="PileType" /> 值反向映射到其已注册字符串 id。
        /// </summary>
        public static bool TryGetModCardPileId(this PileType value, out string id)
        {
            return ModCardPileRegistry.TryGetId(value, out id);
        }

        /// <summary>
        ///     Reverse-maps a minted mod <see cref="PileType" /> value to its registered string id.
        ///     将 minted mod <see cref="PileType" /> 值反向映射到其已注册字符串 id。
        /// </summary>
        public static string GetModCardPileId(this PileType value)
        {
            return ModCardPileRegistry.TryGetId(value, out var id)
                ? id
                : throw new KeyNotFoundException($"PileType '0x{(int)value:X8}' is not a registered mod card pile.");
        }
    }
}
