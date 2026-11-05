#if STS2_AT_LEAST_0_113_0
using System.Reflection;
#endif
using MegaCrit.Sts2.Core.HoverTips;

namespace STS2RitsuLib.Compat
{
    internal static class HoverTipCompat
    {
#if STS2_AT_LEAST_0_113_0
        private static readonly FieldInfo IdField = typeof(HoverTip).GetField(
                                                        "<Id>k__BackingField",
                                                        BindingFlags.Instance | BindingFlags.NonPublic)
                                                    ?? throw new MissingFieldException(typeof(HoverTip).FullName,
                                                        "<Id>k__BackingField");
#endif

        internal static HoverTip WithId(HoverTip tip, string id)
        {
#if STS2_AT_LEAST_0_113_0
            object boxed = tip;
            IdField.SetValue(boxed, id);
            return (HoverTip)boxed;
#else
            tip.Id = id;
            return tip;
#endif
        }
    }
}
