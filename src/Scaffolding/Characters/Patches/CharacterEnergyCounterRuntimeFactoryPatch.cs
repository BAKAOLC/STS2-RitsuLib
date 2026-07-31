using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Scaffolding.Godot;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Converts a mod character's compatible legacy energy-counter scene into an
    ///         <see cref="NEnergyCounter" /> before the base game attempts to instantiate the scene as that type.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在游戏本体尝试将场景直接实例化为 <see cref="NEnergyCounter" /> 前，将模组角色兼容的旧版能量计数器
    ///         场景转换为该类型。
    ///     </para>
    /// </summary>
    internal class CharacterEnergyCounterRuntimeFactoryPatch : IPatchMethod
    {
        private static readonly FieldInfo PlayerField = AccessTools.Field(typeof(NEnergyCounter), "_player")!;
        public static string PatchId => "character_energy_counter_runtime_factory";

        public static string Description =>
            "Allow mod characters to supply NEnergyCounter via Ritsu scene conversion before direct scene instantiate";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NEnergyCounter), nameof(NEnergyCounter.Create))];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Converts a mod energy-counter scene into <see cref="NEnergyCounter" /> and assigns its owning
        ///         player before the base game performs direct scene instantiation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将模组能量计数器场景转换为 <see cref="NEnergyCounter" /> 并设置其所属玩家，然后跳过游戏本体的
        ///         直接场景实例化。
        ///     </para>
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Player player, ref NEnergyCounter? __result)
        {
            if (!CharacterAssetOverridePatchHelper.TryResolveOverridePath(
                    player.Character,
                    static o => o.CustomEnergyCounterPath,
                    nameof(IModCharacterAssetOverrides.CustomEnergyCounterPath),
                    out var energyCounterPath))
                return true;

            var scene = ContentAssetOverridePatchHelper.ResolveScene(energyCounterPath);
            if (scene == null)
                return true;

            try
            {
                var created = RitsuGodotNodeFactories.CreateFromScene<NEnergyCounter>(
                    scene,
                    PackedScene.GenEditState.Disabled);
                PlayerField.SetValue(created, player);
                __result = created;
                return false;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Godot] Failed to auto-convert energy counter '{energyCounterPath}' for character {player.Character.Id.Entry}: {ex.Message}. Falling back.");
                return true;
            }
        }
    }
}
