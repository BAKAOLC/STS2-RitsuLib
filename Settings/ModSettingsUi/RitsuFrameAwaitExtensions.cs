using Godot;

namespace STS2RitsuLib.Settings
{
    internal static class RitsuFrameAwaitExtensions
    {
        public static async Task AwaitRitsuProcessFrame(this Node node, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var tree = node.GetTree();
            if (tree == null)
            {
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
                return;
            }

            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            ct.ThrowIfCancellationRequested();
        }
    }
}
