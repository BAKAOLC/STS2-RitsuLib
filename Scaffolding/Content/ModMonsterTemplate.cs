using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="MonsterModel" /> for mods: <see cref="IModMonsterAssetOverrides" /> for creature visuals scene
    ///     path.
    ///     Register the concrete type with <c>ModContentRegistry.RegisterMonster&lt;T&gt;()</c> or pack builder
    ///     <c>Monster&lt;T&gt;()</c>.
    /// </summary>
    public abstract class ModMonsterTemplate : MonsterModel, IModMonsterAssetOverrides
    {
        /// <inheritdoc />
        public virtual MonsterAssetProfile AssetProfile => MonsterAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomVisualsPath => AssetProfile.VisualsScenePath;
    }
}
