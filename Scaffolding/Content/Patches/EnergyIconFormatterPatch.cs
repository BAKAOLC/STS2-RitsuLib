using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Intercepts <c>EnergyIconsFormatter.TryEvaluateFormat</c> after it assembles the
    ///     hard-coded rich-text img tag for the small energy icon used in card descriptions, and
    ///     replaces it with a custom path when the owning card pool implements
    ///     <see cref="IModTextEnergyIconPool" />.
    ///     <para />
    ///     The default game path pattern is:
    ///     <c>[img]res://images/packed/sprite_fonts/{prefix}_energy_icon.png[/img]</c>.
    ///     Implementing <see cref="IModTextEnergyIconPool.TextEnergyIconPath" /> on the
    ///     <see cref="MegaCrit.Sts2.Core.Models.CardPoolModel" /> lets you use any resource path.
    ///     拦截 <c>EnergyIconsFormatter.TryEvaluateFormat</c>：在它组装用于卡牌描述中小型能量图标的
    ///     硬编码富文本 img 标签后，
    ///     如果所属卡牌池实现 <see cref="IModTextEnergyIconPool" />，则替换为自定义路径。
    ///     <para />
    ///     游戏默认路径模式为：
    ///     在 <see cref="MegaCrit.Sts2.Core.Models.CardPoolModel" /> 上实现
    ///     <see cref="IModTextEnergyIconPool.TextEnergyIconPath" /> 后即可使用任意资源路径。
    /// </summary>
    public class EnergyIconFormatterPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "energy_icon_formatter_text_icon_override";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Allow mod card pools to override the small energy icon path in rich-text card descriptions";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EnergyIconsFormatter), "TryEvaluateFormat")];
        }

        /// <summary>
        ///     After the formatter stores the assembled <c>text3</c> img-tag into its local variable,
        ///     insert a call that lets <see cref="ModTextEnergyIconHelper" /> redirect to a custom path.
        ///     Matched IL pattern (inside TryEvaluateFormat):
        ///     <code>
        ///         ldstr  "[img]res://images/packed/sprite_fonts/"
        ///         ldloc  (text / prefix)
        ///         ldstr  "_energy_icon.png[/img]"
        ///         call   string.Concat(string, string, string)
        ///         stloc  (text3)                        ← match ends here
        ///     </code>
        ///     Inserted after stloc:
        ///     <code>
        ///         ldloc  (text)
        ///         ldloc  (text3)
        ///         call   ModTextEnergyIconHelper.OverrideTextIconTag
        ///         stloc  (text3)
        ///     </code>
        ///     格式化器将组装好的 <c>text3</c> img 标签存入局部变量后，
        ///     插入一次调用，让 <see cref="ModTextEnergyIconHelper" /> 重定向到自定义路径。
        ///     匹配的 IL 模式（位于 TryEvaluateFormat 内）：
        ///     <code>
        /// ldloc  (text / prefix)
        /// </code>
        ///     在 stloc 后插入：
        ///     <code>
        /// ldloc  (text)
        /// ldloc  (text3)
        /// call   ModTextEnergyIconHelper.OverrideTextIconTag
        /// stloc  (text3)
        /// </code>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var concatMethod = AccessTools.Method(
                typeof(string), nameof(string.Concat),
                [typeof(string), typeof(string), typeof(string)]);

            var overrideMethod = AccessTools.Method(
                typeof(ModTextEnergyIconHelper),
                nameof(ModTextEnergyIconHelper.OverrideTextIconTag));

            var rewriter = HarmonyIlRewriter.From(instructions);
            var pattern = HarmonyIlPattern.Sequence(
                HarmonyIl.IsLdstr("[img]res://images/packed/sprite_fonts/"),
                HarmonyIl.IsLdloc(),
                HarmonyIl.IsLdstr(),
                HarmonyIl.IsCall(concatMethod),
                HarmonyIl.IsStloc());

            if (!rewriter.TryFind(pattern, out var match))
            {
                if (!rewriter.Contains(instruction => HarmonyIl.IsCallTo(instruction, overrideMethod)))
                    RitsuLibFramework.Logger.Warn(
                        "[EnergyIconFormatter] Could not find text energy icon concat pattern; override patch skipped.");

                return rewriter.Instructions();
            }

            var prefixLocal = match.GetLocalLoad(rewriter.Code, 1);
            var textIconLocal = match.GetLocalStore(rewriter.Code, 4);

            var report = rewriter.TryInsertAfterFirst(
                "[EnergyIconFormatter] Insert text energy icon override",
                pattern,
                [
                    prefixLocal.Load(),
                    textIconLocal.Load(),
                    HarmonyIl.Call(overrideMethod),
                    textIconLocal.Store(),
                ],
                code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, overrideMethod)));
            report.RequireSucceeded();
            if (report.Applied > 0)
                report.RequireExactly(1);

            return rewriter.InstructionsChecked("[EnergyIconFormatter] Insert text energy icon override");
        }
    }

    /// <summary>
    ///     Runtime helper called by the patched formatter.
    ///     On first use it builds a lookup table from all registered mod characters' card pools
    ///     that implement <see cref="IModTextEnergyIconPool" />.
    ///     由已修补格式化器调用的运行时辅助方法。
    ///     首次使用时，它会从所有已注册 mod 角色中实现 <see cref="IModTextEnergyIconPool" /> 的卡牌池
    ///     构建查找表。
    /// </summary>
    internal static class ModTextEnergyIconHelper
    {
        private static Dictionary<string, string>? _cache;

        public static string OverrideTextIconTag(string prefix, string defaultTag)
        {
            _cache ??= BuildCache();
            return _cache.TryGetValue(prefix, out var path)
                ? $"[img]{path}[/img]"
                : defaultTag;
        }

        private static Dictionary<string, string> BuildCache()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var character in ModContentRegistry.GetModCharacters())
                AddPoolIfMapped(dict, character.CardPool);

            foreach (var pool in ModelDb.AllCards.Select(c => c.Pool).Distinct())
                AddPoolIfMapped(dict, pool);

            foreach (var pool in ModelDb.AllRelics.Select(r => r.Pool).Distinct())
                AddPoolIfMapped(dict, pool);

            foreach (var pool in ModelDb.AllPotions.Select(p => p.Pool).Distinct())
                AddPoolIfMapped(dict, pool);

            return dict;
        }

        private static void AddPoolIfMapped(Dictionary<string, string> dict, IPoolModel pool)
        {
            if (pool is not IModTextEnergyIconPool mapped)
                return;

            if (string.IsNullOrWhiteSpace(mapped.TextEnergyIconPath))
                return;

            if (!AssetPathDiagnostics.Exists(mapped.TextEnergyIconPath!, pool,
                    nameof(IModTextEnergyIconPool.TextEnergyIconPath)))
                return;

            dict.TryAdd(pool.EnergyColorName, mapped.TextEnergyIconPath!);
        }
    }
}
