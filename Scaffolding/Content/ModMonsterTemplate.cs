using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="MonsterModel" /> for mods: <see cref="IModMonsterAssetOverrides" /> supplies the visuals scene
    ///     path; override <see cref="TryCreateCreatureVisuals" /> to build <see cref="NCreatureVisuals" /> in code instead.
    ///     Use <see cref="RitsuGodotNodeFactories" /> for explicit <c>CreateFromResource</c> / <c>CreateFromScenePath</c>
    ///     construction. Register with <c>ModContentRegistry.RegisterMonster&lt;T&gt;()</c> or <c>Monster&lt;T&gt;()</c> on
    ///     the pack builder.
    /// </summary>
    public abstract class ModMonsterTemplate : MonsterModel, IModMonsterAssetOverrides,
        IModMonsterCreatureVisualsFactory
    {
        /// <inheritdoc />
        public virtual MonsterAssetProfile AssetProfile => MonsterAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomVisualsPath => AssetProfile.VisualsScenePath;

        NCreatureVisuals? IModMonsterCreatureVisualsFactory.TryCreateCreatureVisuals()
        {
            return TryCreateCreatureVisuals();
        }

        /// <summary>
        ///     Non-null value becomes combat visuals; otherwise paths (<see cref="CustomVisualsPath" /> / vanilla) apply.
        /// </summary>
        protected virtual NCreatureVisuals? TryCreateCreatureVisuals()
        {
            return null;
        }
    }
}
