using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches
{
    /// <summary>
    ///     Keeps dynamic hand-outline colors fresh through a child process ticker while the holder is alive.
    ///     holder 存活期间通过子 process ticker 保持动态手牌描边颜色新鲜。
    /// </summary>
    internal sealed class NHandCardHolderDynamicOutlineTickPatch : IPatchMethod
    {
        public static string PatchId => "n_hand_card_holder_dynamic_outline_tick";

        public static string Description => "Refresh dynamic hand-outline colors every process frame";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHandCardHolder), nameof(NHandCardHolder._Ready))];
        }

        public static void Postfix(NHandCardHolder __instance)
        {
            if (!GodotObject.IsInstanceValid(__instance) || !__instance.IsInsideTree() || __instance.GetTree() == null)
                return;

            NHandCardHolderDynamicOutlineTicker.Ensure(__instance);
        }
    }

    internal sealed partial class NHandCardHolderDynamicOutlineTicker : Node
    {
        private const string NodeName = "RitsuLibDynamicHandOutlineTicker";
        private NHandCardHolder _holder = null!;

        internal static void Ensure(NHandCardHolder holder)
        {
            if (holder.GetNodeOrNull<NHandCardHolderDynamicOutlineTicker>(NodeName) is { } existing)
            {
                existing._holder = holder;
                existing.SetProcess(true);
                return;
            }

            holder.AddChild(new NHandCardHolderDynamicOutlineTicker
            {
                Name = NodeName,
                ProcessMode = ProcessModeEnum.Always,
                _holder = holder,
            });
        }

        public override void _Process(double delta)
        {
            if (!ModCardHandOutlineRegistry.HasAny)
                return;

            if (!IsInstanceValid(_holder) || !_holder.IsInsideTree())
            {
                SetProcess(false);
                return;
            }

            ModCardHandOutlineRegistry.TryRefreshDynamicOutlineForHolder(_holder);
        }
    }
}
