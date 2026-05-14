using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Allows mod characters to convert legacy energy-counter scenes into <see cref="NEnergyCounter" /> before
    ///     vanilla tries to instantiate the scene as the final type directly.
    ///     允许 mod 角色在原版尝试直接将场景实例化为最终类型之前，将旧版能量计数器场景转换为 <see cref="NEnergyCounter" />。
    /// </summary>
    public class CharacterEnergyCounterRuntimeFactoryPatch : IPatchMethod
    {
        private static readonly FieldInfo PlayerField = AccessTools.Field(typeof(NEnergyCounter), "_player")!;

        /// <inheritdoc />
        public static string PatchId => "character_energy_counter_runtime_factory";

        /// <inheritdoc />
        public static string Description =>
            "Allow mod characters to supply NEnergyCounter via Ritsu scene conversion before direct scene instantiate";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NEnergyCounter), nameof(NEnergyCounter.Create))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Converts a mod energy-counter scene into <see cref="NEnergyCounter" /> and injects the owning player
        ///     before vanilla performs direct scene instantiation.
        ///     将 mod 能量计数器场景转换为 <see cref="NEnergyCounter" />，并在原版执行直接场景实例化之前
        ///     注入所属玩家。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Player player, ref NEnergyCounter? __result)
        {
            if (player.Character is not IModCharacterAssetOverrides { CustomEnergyCounterPath: { } energyCounterPath })
                return true;

            if (!ResourceLoader.Exists(energyCounterPath))
                return true;

            var created = RitsuGodotNodeFactories.CreateFromScenePath<NEnergyCounter>(
                energyCounterPath,
                PackedScene.GenEditState.Disabled);
            PlayerField.SetValue(created, player);
            __result = created;
            return false;
        }
        // ReSharper restore InconsistentNaming
    }
}
