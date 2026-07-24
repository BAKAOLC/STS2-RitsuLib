using System.Globalization;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    internal static class ModOrbRandomPoolPolicyRuntime
    {
        private static readonly FieldInfo? VanillaValidOrbsField = AccessTools.Field(typeof(OrbModel), "_validOrbs");

        public static bool HasModdedCandidates()
        {
            var vanillaIds = GetVanillaRandomOrbIds();
            return vanillaIds.Length != 0 && ModelDb.Orbs.Any(orb => ShouldAddFromModelDbOrbs(vanillaIds, orb));
        }

        public static bool TryGetRandomOrb(Rng rng, out OrbModel orb)
        {
            var vanillaIds = GetVanillaRandomOrbIds();
            if (vanillaIds.Length == 0)
            {
                orb = null!;
                return false;
            }

            var candidates = BuildCandidates(vanillaIds);
            var selected = rng.NextItem(candidates);
            if (selected == null)
            {
                orb = null!;
                return false;
            }

            orb = selected;
            return true;
        }

        private static ModelId[] GetVanillaRandomOrbIds()
        {
            return VanillaValidOrbsField?.GetValue(null) as ModelId[] ?? [];
        }

        private static OrbModel[] BuildCandidates(IReadOnlyCollection<ModelId> vanillaIds)
        {
            return
            [
                .. vanillaIds
                    .Select(static id => ModelDb.GetByIdOrNull<OrbModel>(id))
                    .OfType<OrbModel>()
                    .Concat(ModelDb.Orbs.Where(orb => ShouldAddFromModelDbOrbs(vanillaIds, orb)))
                    .DistinctBy(static orb => orb.Id),
            ];
        }

        private static bool ShouldAddFromModelDbOrbs(IReadOnlyCollection<ModelId> vanillaIds, OrbModel orb)
        {
            return !vanillaIds.Contains(orb.Id) &&
                   orb is IModOrbRandomPoolPolicy { AllowInRandomOrbPool: true };
        }
    }

    /// <summary>
    ///     Replaces <see cref="OrbModel.GetRandomOrb" /> only when registered mod orbs opt in to the random pool.
    ///     仅当已注册 mod 充能球选择加入随机池时替换 <see cref="OrbModel.GetRandomOrb" />。
    /// </summary>
    internal sealed class OrbModelRandomPoolPolicyPatch : IPatchMethod
    {
        public static string PatchId => "orb_model_random_pool_policy";
        public static bool IsCritical => false;
        public static string Description => "Include opt-in mod orbs in OrbModel.GetRandomOrb";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(OrbModel), nameof(OrbModel.GetRandomOrb), [typeof(Rng)])];
        }

        /// <summary>
        ///     Uses the vanilla random pool plus registered <see cref="IModOrbRandomPoolPolicy" /> opt-in candidates.
        ///     使用原版随机池，加上已注册且通过 <see cref="IModOrbRandomPoolPolicy" /> 选择加入的候选项。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId, Const.FrameworkContentRegistryHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(Rng rng, ref OrbModel __result)
        {
            if (!ModOrbRandomPoolPolicyRuntime.HasModdedCandidates())
                return true;

            if (!ModOrbRandomPoolPolicyRuntime.TryGetRandomOrb(rng, out var orb))
                return true;

            __result = orb;
            return false;
        }
    }

    internal static class ModOrbValueDisplayPolicyRuntime
    {
        private static readonly FieldInfo? PassiveLabelField = AccessTools.Field(typeof(NOrb), "_passiveLabel");
        private static readonly FieldInfo? EvokeLabelField = AccessTools.Field(typeof(NOrb), "_evokeLabel");

        public static void Apply(NOrb node, bool isEvoking)
        {
            var model = node.Model;
            if (model == null)
                return;

            var passiveLabel = PassiveLabelField?.GetValue(node) as MegaLabel;
            var evokeLabel = EvokeLabelField?.GetValue(node) as MegaLabel;
            if (passiveLabel == null || evokeLabel == null)
                return;

            var defaultPassiveText = model.PassiveVal.ToString("0", CultureInfo.InvariantCulture);
            var defaultEvokeText = model.EvokeVal.ToString("0", CultureInfo.InvariantCulture);
            var baseState = model is IModOrbValueDisplayPolicy policy
                ? new OrbValueDisplayState(
                    policy.ValueDisplayMode,
                    policy.PassiveValueDisplayText,
                    policy.EvokeValueDisplayText)
                : new OrbValueDisplayState(
                    ModOrbValueDisplayMode.Vanilla,
                    defaultPassiveText,
                    defaultEvokeText);

            var state = ModelCapabilityHost.ApplyOrbValueDisplay(new(
                model,
                isEvoking,
                baseState.DisplayMode,
                baseState.PassiveText,
                baseState.EvokeText));

            if (state.DisplayMode == ModOrbValueDisplayMode.Vanilla &&
                string.Equals(state.PassiveText, defaultPassiveText, StringComparison.Ordinal) &&
                string.Equals(state.EvokeText, defaultEvokeText, StringComparison.Ordinal))
                return;

            if (state.DisplayMode != ModOrbValueDisplayMode.Vanilla)
            {
                var (showPassive, showEvoke) = state.DisplayMode switch
                {
                    ModOrbValueDisplayMode.Hidden => (false, false),
                    ModOrbValueDisplayMode.Contextual => (!isEvoking, isEvoking),
                    ModOrbValueDisplayMode.SinglePassive => (true, false),
                    ModOrbValueDisplayMode.SingleEvoke => (false, true),
                    ModOrbValueDisplayMode.Both => (true, true),
                    _ => (passiveLabel.Visible, evokeLabel.Visible),
                };

                passiveLabel.Visible = showPassive;
                evokeLabel.Visible = showEvoke;
            }

            if (passiveLabel.Visible)
                passiveLabel.SetTextAutoSize(state.PassiveText);
            if (evokeLabel.Visible)
                evokeLabel.SetTextAutoSize(state.EvokeText);
        }
    }

    /// <summary>
    ///     Applies <see cref="IModOrbValueDisplayPolicy" /> after vanilla orb visual refresh.
    ///     在原版充能球视觉刷新之后应用 <see cref="IModOrbValueDisplayPolicy" />。
    /// </summary>
    internal sealed class NOrbValueDisplayPolicyPatch : IPatchMethod
    {
        public static string PatchId => "norb_value_display_policy";
        public static bool IsCritical => false;
        public static string Description => "Allow mod orbs to control passive/evoke value labels";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NOrb), nameof(NOrb.UpdateVisuals), [typeof(bool)])];
        }

        /// <summary>
        ///     Reconciles labels with the mod orb's requested display mode.
        ///     按 mod 充能球请求的显示模式同步标签。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(NOrb __instance, bool isEvoking)
        {
            ModOrbValueDisplayPolicyRuntime.Apply(__instance, isEvoking);
        }
    }
}
