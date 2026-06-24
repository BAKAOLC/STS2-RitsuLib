namespace STS2RitsuLib.Scaffolding.Characters.Visuals.Definition
{
    /// <summary>
    ///     Entry point for procedural merchant / rest-site visuals (no custom <c>tscn</c> for those characters). Runtime
    ///     nodes are built by <c>ModWorldSceneVisualNodeFactory</c> in the parent <c>Visuals</c> namespace.
    ///     程序化商人 / 休息点视觉的入口点（这些角色不需要自定义 <c>tscn</c>）。运行时节点由父级
    ///     <c>Visuals</c> 命名空间中的 <c>ModWorldSceneVisualNodeFactory</c> 构建。
    /// </summary>
    public static class ModCharacterWorldSceneVisuals
    {
        /// <summary>
        ///     Begins a <see cref="CharacterWorldProceduralVisualSet" /> builder.
        ///     开始一个 <see cref="CharacterWorldProceduralVisualSet" /> 构建器。
        /// </summary>
        public static CharacterWorldProceduralVisualSetBuilder Procedural()
        {
            return CharacterWorldProceduralVisualSetBuilder.Create();
        }
    }
}
