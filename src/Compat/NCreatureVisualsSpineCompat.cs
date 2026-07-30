using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies skeleton overrides through either the legacy <c>Body</c> member or the newer <c>SpineBody</c>
    ///         member.
    ///     </para>
    ///     <para xml:lang="zh-CN">通过旧版 <c>Body</c> 成员或新版 <c>SpineBody</c> 成员应用骨架覆盖。</para>
    /// </summary>
    internal static class NCreatureVisualsSpineCompat
    {
        private static readonly MethodInfo? SetSkeletonDataRes =
            typeof(MegaSprite).GetMethod(nameof(MegaSprite.SetSkeletonDataRes),
                BindingFlags.Public | BindingFlags.Instance,
                null, [typeof(MegaSkeletonDataResource)], null);

        internal static bool TryApplyCombatSkeletonOverride(NCreatureVisuals visuals, Resource skeletonData)
        {
            if (SetSkeletonDataRes == null || !visuals.HasSpineAnimation)
                return false;

            var wrapper = new MegaSkeletonDataResource(skeletonData);

            var spineProp =
                typeof(NCreatureVisuals).GetProperty("SpineBody", BindingFlags.Public | BindingFlags.Instance);
            if (spineProp?.GetValue(visuals) is { } spineBody)
            {
                SetSkeletonDataRes.Invoke(spineBody, [wrapper]);
                return true;
            }

            var bodyProp = typeof(NCreatureVisuals).GetProperty("Body", BindingFlags.Public | BindingFlags.Instance);
            if (bodyProp?.GetValue(visuals) is not Node2D bodyNode) return false;
            var mega = new MegaSprite(bodyNode);
            SetSkeletonDataRes.Invoke(mega, [wrapper]);
            return true;
        }

        internal static bool HasSpineTargetForOverride(NCreatureVisuals visuals)
        {
            if (typeof(NCreatureVisuals).GetProperty("SpineBody", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(visuals) != null)
                return true;

            return typeof(NCreatureVisuals).GetProperty("Body", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(visuals) is Node2D;
        }
    }
}
