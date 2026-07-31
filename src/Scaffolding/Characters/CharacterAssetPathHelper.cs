using MegaCrit.Sts2.Core.Helpers;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Builds vanilla-layout resource paths from a character model entry.
    ///     </para>
    ///     <para xml:lang="zh-CN">根据角色模型条目构建符合原版布局的资源路径。</para>
    /// </summary>
    public static class CharacterAssetPathHelper
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the energy icon path for <paramref name="energyColorName" /> through
        ///         <see cref="EnergyIconHelper" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="EnergyIconHelper" /> 获取 <paramref name="energyColorName" /> 的能量图标路径。
        ///     </para>
        /// </summary>
        public static string GetEnergyIconPath(string energyColorName)
        {
            return EnergyIconHelper.GetPath(energyColorName);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the combat energy counter scene path.</para>
        ///     <para xml:lang="zh-CN">获取战斗能量计数器的场景路径。</para>
        /// </summary>
        public static string GetEnergyCounterPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"combat/energy_counters/{Normalize(characterEntry)}_energy_counter");
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the combat creature-visuals scene path.</para>
        ///     <para xml:lang="zh-CN">获取战斗生物视觉节点的场景路径。</para>
        /// </summary>
        public static string GetVisualsPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"creature_visuals/{Normalize(characterEntry)}");
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the character-select background scene path.</para>
        ///     <para xml:lang="zh-CN">获取角色选择背景的场景路径。</para>
        /// </summary>
        public static string GetCharacterSelectBackgroundPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"screens/char_select/char_select_bg_{Normalize(characterEntry)}");
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the unlocked character-select portrait path.</para>
        ///     <para xml:lang="zh-CN">获取已解锁角色选择肖像的路径。</para>
        /// </summary>
        public static string GetCharacterSelectIconPath(string characterEntry)
        {
            return ImageHelper.GetImagePath($"packed/character_select/char_select_{Normalize(characterEntry)}.png");
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the locked character-select portrait path.</para>
        ///     <para xml:lang="zh-CN">获取未解锁角色选择肖像的路径。</para>
        /// </summary>
        public static string GetCharacterSelectLockedIconPath(string characterEntry)
        {
            return ImageHelper.GetImagePath(
                $"packed/character_select/char_select_{Normalize(characterEntry)}_locked.png");
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the character's map marker path.</para>
        ///     <para xml:lang="zh-CN">获取角色的地图标记路径。</para>
        /// </summary>
        public static string GetMapMarkerPath(string characterEntry)
        {
            return ImageHelper.GetImagePath($"packed/map/icons/map_marker_{Normalize(characterEntry)}.png");
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the character's card-trail VFX scene path.</para>
        ///     <para xml:lang="zh-CN">获取角色卡牌轨迹特效的场景路径。</para>
        /// </summary>
        public static string GetTrailPath(string characterEntry)
        {
            return SceneHelper.GetScenePath($"vfx/card_trail_{Normalize(characterEntry)}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Enumerates the standard character asset paths derived from <paramref name="characterEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         枚举根据 <paramref name="characterEntry" /> 派生的标准角色资源路径。
        ///     </para>
        /// </summary>
        public static IEnumerable<string> EnumerateDefaultCharacterAssets(string characterEntry)
        {
            yield return GetVisualsPath(characterEntry);
            yield return GetCharacterSelectBackgroundPath(characterEntry);
            yield return GetCharacterSelectIconPath(characterEntry);
            yield return GetCharacterSelectLockedIconPath(characterEntry);
            yield return GetMapMarkerPath(characterEntry);
            yield return GetTrailPath(characterEntry);
            yield return GetEnergyCounterPath(characterEntry);
        }

        private static string Normalize(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value.Trim().ToLowerInvariant();
        }
    }
}
