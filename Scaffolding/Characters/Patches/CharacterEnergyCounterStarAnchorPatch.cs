using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Reparents <see cref="NStarCounter" /> to <c>%StarAnchor</c> when the active energy counter scene exposes
    ///     that anchor, so custom energy counters can control star counter placement.
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    public sealed class CharacterEnergyCounterStarAnchorPatch : IPatchMethod
    {
        private static readonly FieldInfo? EnergyCounterField = AccessTools.Field(typeof(NCombatUi), "_energyCounter");
        private static readonly FieldInfo? StarCounterField = AccessTools.Field(typeof(NCombatUi), "_starCounter");

        /// <inheritdoc />
        public static string PatchId => "character_energy_counter_star_anchor";

        /// <inheritdoc />
        public static string Description => "Reparent star counter to energy counter %StarAnchor when present";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatUi), nameof(NCombatUi.Activate), [typeof(CombatState)])];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Moves the star counter under <c>%StarAnchor</c> and resets anchor/offset fields for predictable layout.
        /// </summary>
        public static void Postfix(NCombatUi __instance)
        {
            if (EnergyCounterField?.GetValue(__instance) is not NEnergyCounter energyCounter)
                return;
            if (StarCounterField?.GetValue(__instance) is not NStarCounter starCounter)
                return;
            if (energyCounter.GetNodeOrNull<CanvasItem>("%StarAnchor") is not { } starAnchor)
                return;

            var currentScale = starCounter.Scale;
            var targetSize = starCounter.Size == Vector2.Zero ? new(128f, 128f) : starCounter.Size;

            starCounter.Reparent(starAnchor);
            starCounter.AnchorLeft = 0f;
            starCounter.AnchorTop = 0f;
            starCounter.AnchorRight = 0f;
            starCounter.AnchorBottom = 0f;
            starCounter.OffsetLeft = 0f;
            starCounter.OffsetTop = 0f;
            starCounter.OffsetRight = targetSize.X;
            starCounter.OffsetBottom = targetSize.Y;
            starCounter.Position = Vector2.Zero;
            starCounter.Scale = currentScale;
        }
        // ReSharper restore InconsistentNaming
    }
}
