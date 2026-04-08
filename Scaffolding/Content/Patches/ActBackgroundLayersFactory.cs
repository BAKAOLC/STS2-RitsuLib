using Godot;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Builds <see cref="BackgroundAssets" /> from an arbitrary <c>res://</c> layers directory using the same file naming
    ///     rules as vanilla (<c>_bg_</c> / <c>_fg_</c>).
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
            var fgLayer = rng.NextItem(fgCandidates.ToArray());

            return CombatBackgroundAssetsFactory.Construct(mainBackgroundScenePath, bgLayers, fgLayer);
        }

        private static List<string> SelectRandomBackgroundLayers(Rng rng,
            Dictionary<string, List<string>> bgLayers)
        {
            return bgLayers.OrderBy(k => k.Key, StringComparer.Ordinal).Select(kv => rng.NextItem(kv.Value))
                .Select(item => item!).ToList();
        }
    }
}
