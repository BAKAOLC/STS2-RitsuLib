using System.Reflection;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;

namespace STS2RitsuLib.Compat
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Calls the parameterless <see cref="NCombatUi.AnimOut" /> overload on newer game versions, or
    ///         <c>AnimOut(CombatRoom)</c> on older versions.
    ///     </para>
    ///     <para xml:lang="zh-CN">在较新游戏版本中调用无参数的 <see cref="NCombatUi.AnimOut" /> 重载；在较旧版本中调用 <c>AnimOut(CombatRoom)</c>。</para>
    /// </summary>
    internal static class NCombatUiAnimOutCompat
    {
        private static readonly Action<NCombatUi>? ZeroArg;
        private static readonly Action<NCombatUi, CombatRoom>? OneArg;

        static NCombatUiAnimOutCompat()
        {
            var t = typeof(NCombatUi);
            var zero = t.GetMethod("AnimOut", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (zero != null)
                ZeroArg = ui => zero.Invoke(ui, null);

            var one = t.GetMethod("AnimOut", BindingFlags.Public | BindingFlags.Instance, null,
                [typeof(CombatRoom)], null);
            if (one != null)
                OneArg = (ui, room) => one.Invoke(ui, [room]);
        }

        internal static void AnimOutForGameOver(NCombatUi ui, CombatRoom currentCombatRoom)
        {
            if (ZeroArg != null)
            {
                ZeroArg(ui);
                return;
            }

            if (OneArg != null)
            {
                OneArg(ui, currentCombatRoom);
                return;
            }

            RitsuLibFramework.Logger.Warn(
                "[Visuals] NCombatUi.AnimOut has no compatible overload; skipping combat UI AnimOut during game-over handling.");
        }
    }
}
