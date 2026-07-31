using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Appends opted-in mod character card pools to the reward-color candidates used by Colorful Philosophers.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将主动加入的模组角色牌池追加到“色彩哲学家”使用的奖励颜色候选列表中。
    ///     </para>
    /// </summary>
    internal class ColorfulPhilosophersCardPoolColorOrderPatch : IPatchMethod
    {
        public static string PatchId => "colorful_philosophers_card_pool_color_order";

        public static string Description =>
            "Append opt-in mod character card pools to Colorful Philosophers color order";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ColorfulPhilosophers), "CardPoolColorOrder", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Extends the original candidate order while leaving option generation and reward handling to the
        ///         base game.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         扩展原有的候选顺序，选项生成和奖励处理仍由游戏本体负责。
        ///     </para>
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref IEnumerable<CardPoolModel> __result)
        {
            var modPools = ModelDb.AllCharacterCardPools
                .Where(static pool => pool is IModColorfulPhilosophersCardPool);

            __result =
            [
                .. __result
                    .Concat(modPools)
                    .DistinctBy(static pool => pool.Id),
            ];
        }
    }
}
