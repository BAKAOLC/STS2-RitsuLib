namespace STS2RitsuLib.Scaffolding.Characters.Visuals.Definition
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the entry point for procedural merchant and rest-site visuals that do not require custom
    ///         <c>tscn</c> scenes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供程序化商人和休息处形象的入口，使其无需自定义 <c>tscn</c> 场景。
    ///     </para>
    /// </summary>
    public static class ModCharacterWorldSceneVisuals
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a builder for <see cref="CharacterWorldProceduralVisualSet" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建 <see cref="CharacterWorldProceduralVisualSet" /> 构建器。
        ///     </para>
        /// </summary>
        public static CharacterWorldProceduralVisualSetBuilder Procedural()
        {
            return CharacterWorldProceduralVisualSetBuilder.Create();
        }
    }
}
