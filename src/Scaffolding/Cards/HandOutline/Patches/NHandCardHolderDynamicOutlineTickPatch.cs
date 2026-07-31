using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds a child process node that refreshes dynamic hand-card outline rules each frame.
    ///     </para>
    ///     <para xml:lang="zh-CN">添加子处理节点，以便每帧刷新动态手牌描边规则。</para>
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
        private bool _hadDynamicRule;
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

        public override void _EnterTree()
        {
            SetProcess(true);
        }

        public override void _Process(double delta)
        {
            if (!IsInstanceValid(_holder) || !_holder.IsInsideTree())
            {
                SetProcess(false);
                return;
            }

            if (!ModCardHandOutlineRegistry.HasAny)
            {
                RestoreAfterDynamicRule();
                return;
            }

            if (ModCardHandOutlineRegistry.TryRefreshDynamicOutlineForHolder(_holder))
            {
                _hadDynamicRule = true;
                return;
            }

            RestoreAfterDynamicRule();
        }

        private void RestoreAfterDynamicRule()
        {
            if (!_hadDynamicRule)
                return;

            _hadDynamicRule = false;
            _holder.UpdateCard();
        }
    }
}
