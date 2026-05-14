using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Enumerates resource paths referenced by procedural ancient stage definitions for room preloading.
    ///     枚举远古事件程序化舞台定义引用的资源路径，用于房间预加载。
    /// </summary>
    internal static class AncientEventStageProceduralAssetPaths
    {
        public static string[] Collect(AncientEventStageProceduralVisualSet? stage)
        {
            if (stage == null)
                return [];

            return Enumerate(stage)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static IEnumerable<string> Enumerate(AncientEventStageProceduralVisualSet stage)
        {
            if (!string.IsNullOrWhiteSpace(stage.BackgroundVideoPath))
                yield return stage.BackgroundVideoPath;

            foreach (var path in Enumerate(stage.BackgroundCueSet))
                yield return path;

            foreach (var path in Enumerate(stage.ForegroundCueSet))
                yield return path;
        }

        private static IEnumerable<string> Enumerate(VisualCueSet? cueSet)
        {
            if (cueSet == null)
                yield break;

            if (cueSet.TexturePathByCue != null)
                foreach (var path in cueSet.TexturePathByCue.Values)
                    yield return path;

            if (cueSet.FrameSequenceByCue == null)
                yield break;

            foreach (var sequence in cueSet.FrameSequenceByCue.Values)
            foreach (var frame in sequence.Frames)
                yield return frame.TexturePath;
        }
    }
}
