using Godot;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    internal static class RitsuFrameAwaitExtensions
    {
        public static async Task AwaitRitsuProcessFrame(this Node node, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await RitsuGodotAwaitSafety.AwaitProcessFrameAsync(node.GetTree(), node, ct);
        }
    }
}
