using Godot;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Builds <see cref="BackgroundAssets" /> from a <c>res://</c> layer directory using the base game's
    ///         <c>_bg_</c> and <c>_fg_</c> filename conventions.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用原版游戏的 <c>_bg_</c> 和 <c>_fg_</c> 文件名约定，从 <c>res://</c> 图层目录构建
    ///         <see cref="BackgroundAssets" />。
    ///     </para>
    /// </summary>
    internal static class ActBackgroundLayersFactory
    {
        internal static BackgroundAssets CreateFromCustomLayersDirectory(
            string layersDirectoryResPath,
            string mainBackgroundScenePath,
            Rng rng)
        {
            var normalizedDir = layersDirectoryResPath.TrimEnd('/');
            using var dirAccess = DirAccess.Open(normalizedDir);
            if (dirAccess == null)
                throw new InvalidOperationException("could not find directory " + normalizedDir);

            var bgBySlot = new Dictionary<string, List<string>>();
            var fgCandidates = new List<string>();
            dirAccess.ListDirBegin();
            for (var next = dirAccess.GetNext(); next != ""; next = dirAccess.GetNext())
            {
                if (dirAccess.CurrentIsDir())
                    throw new InvalidOperationException(
                        "there should be no other directories within the layers directory");

                if (next.Contains("_fg_"))
                {
                    fgCandidates.Add(normalizedDir + "/" + next);
                }
                else
                {
                    if (!next.Contains("_bg_"))
                        throw new InvalidOperationException("files must either contain '_fg_' or '_bg_'");

                    var afterBg = next.Split("_bg_")[1];
                    var key = afterBg.Split("_")[0];
                    if (!bgBySlot.TryGetValue(key, out var list))
                    {
                        list = [];
                        bgBySlot[key] = list;
                    }

                    list.Add(normalizedDir + "/" + next);
                }
            }

            var bgLayers = SelectRandomBackgroundLayers(rng, bgBySlot);
            var fgLayer = rng.NextItem([.. fgCandidates]);

            return CombatBackgroundAssetsFactory.Construct(mainBackgroundScenePath, bgLayers, fgLayer);
        }

        private static List<string> SelectRandomBackgroundLayers(Rng rng,
            Dictionary<string, List<string>> bgLayers)
        {
            return
            [
                .. bgLayers.OrderBy(k => k.Key, StringComparer.Ordinal).Select(kv => rng.NextItem(kv.Value))
                    .Select(item => item!),
            ];
        }
    }
}
