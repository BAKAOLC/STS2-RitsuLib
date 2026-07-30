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
    ///     <para xml:lang="en">
    ///         Replaces the base game's rich-text energy-icon path after
    ///         <c>EnergyIconsFormatter.TryEvaluateFormat</c> builds its image tag, using mappings supplied by
    ///         <see cref="IModTextEnergyIconPool" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <c>EnergyIconsFormatter.TryEvaluateFormat</c> 构建图像标签后，使用
    ///         <see cref="IModTextEnergyIconPool" /> 提供的映射替换原版游戏的富文本能量图标路径。
    ///     </para>
    /// </summary>
    internal class EnergyIconFormatterPatch : IPatchMethod
    {
        public static string PatchId => "energy_icon_formatter_text_icon_override";

        public static string Description =>
            "Allow mod card pools to override the small energy icon path in rich-text card descriptions";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EnergyIconsFormatter), "TryEvaluateFormat")];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Inserts <see cref="ModTextEnergyIconHelper.OverrideTextIconTag" /> immediately after the formatter
        ///         stores its assembled rich-text image tag.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在格式化器保存组装完成的富文本图像标签后，立即插入
        ///         <see cref="ModTextEnergyIconHelper.OverrideTextIconTag" /> 调用。
        ///     </para>
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
    ///     <para xml:lang="en">
    ///         Resolves rich-text energy-icon paths from registered pools that implement
    ///         <see cref="IModTextEnergyIconPool" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         从实现 <see cref="IModTextEnergyIconPool" /> 的已注册内容池中解析富文本能量图标路径。
    ///     </para>
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

            foreach (var pool in ModelDb.AllRelicPools)
                AddPoolIfMapped(dict, pool);

            foreach (var pool in ModelDb.AllPotionPools)
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
