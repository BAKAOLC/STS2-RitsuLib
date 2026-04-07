namespace STS2RitsuLib.Scaffolding.Characters.Visuals.Definition
{
    /// <summary>
    ///     Entry point for procedural merchant / rest-site visuals (no custom <c>tscn</c> for those characters). Runtime
    ///     nodes are built by <c>ModWorldSceneVisualNodeFactory</c> in the parent <c>Visuals</c> namespace.
    /// </summary>
    public static class ModCharacterWorldSceneVisuals
    {
        /// <summary>
        ///     Begins a <see cref="CharacterWorldProceduralVisualSet" /> builder.
        /// </summary>
        public static CharacterWorldProceduralVisualSetBuilder Procedural()
        {
            return CharacterWorldProceduralVisualSetBuilder.Create();
        }
    }
}
