using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Collects the resource paths referenced by a procedural Ancient event stage for room preloading.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         收集程序化先古之民事件舞台引用的资源路径，以便预加载房间资源。
    ///     </para>
    /// </summary>
    internal static class AncientEventStageProceduralAssetPaths
    {
        public static string[] Collect(AncientEventStageProceduralVisualSet? stage)
        {
            if (stage == null)
                return [];

            return
            [
                .. Enumerate(stage)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Distinct(StringComparer.Ordinal),
            ];
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
