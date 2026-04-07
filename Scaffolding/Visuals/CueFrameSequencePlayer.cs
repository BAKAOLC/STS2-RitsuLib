using Godot;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Visuals
{
    /// <summary>
    ///     Internal driver that swaps a <see cref="Sprite2D.Texture" /> through a <see cref="VisualFrameSequence" />.
    /// </summary>
    internal partial class CueFrameSequencePlayer : Node
    {
        internal const string NodeName = "RitsuCueFrameSequencePlayer";
        private bool _active;
        private Texture2D?[] _cache = [];
        private double _carry;
        private double _frameDurationSeconds;
        private VisualFrame[] _frames = [];
        private int _index;
        private bool[] _loadFailed = [];
        private bool _loop;

        private Sprite2D? _sprite;

        public override void _Ready()
        {
            SetProcess(false);
        }

        public override void _Process(double delta)
        {
            if (!_active || _sprite == null || _frames.Length == 0)
                return;

            _carry += delta;
            while (_carry >= _frameDurationSeconds && _active)
            {
                _carry -= _frameDurationSeconds;
                Advance();
            }
        }

        internal void StopAndReset()
        {
            _active = false;
            _sprite = null;
            _frames = [];
            _cache = [];
            _loadFailed = [];
            _index = 0;
            _carry = 0;
            SetProcess(false);
        }

        internal bool TryStart(Sprite2D sprite, VisualFrameSequence sequence)
        {
            if (sequence.Frames.Count == 0)
                return false;

            var frames = new VisualFrame[sequence.Frames.Count];
            for (var i = 0; i < sequence.Frames.Count; i++)
            {
                var f = sequence.Frames[i];
                if (string.IsNullOrWhiteSpace(f.TexturePath))
                    return false;

                frames[i] = f;
            }

            StopAndReset();
            _sprite = sprite;
            _frames = frames;
            _cache = new Texture2D?[frames.Length];
            _loadFailed = new bool[frames.Length];
            _loop = sequence.Loop;
            _index = 0;
            _carry = 0;
            _frameDurationSeconds = ClampFrameDuration(frames[0].DurationSeconds);
            ApplyFrame(0);

            if (frames.Length == 1 && !sequence.Loop)
            {
                _active = false;
                SetProcess(false);
                return true;
            }

            _active = true;
            SetProcess(true);
            return true;
        }

        private void Advance()
        {
            _index++;
            if (_index < _frames.Length)
            {
                ApplyFrame(_index);
                _frameDurationSeconds = ClampFrameDuration(_frames[_index].DurationSeconds);
                return;
            }

            if (_loop)
            {
                _index = 0;
                ApplyFrame(0);
                _frameDurationSeconds = ClampFrameDuration(_frames[0].DurationSeconds);
                return;
            }

            _active = false;
            SetProcess(false);
        }

        private static double ClampFrameDuration(float seconds)
        {
            return !float.IsFinite(seconds) || seconds <= 0f ? 1.0 / 60.0 : seconds;
        }

        private void ApplyFrame(int i)
        {
            if (_sprite == null || i < 0 || i >= _frames.Length)
                return;

            var tex = _cache[i];
            if (tex == null)
            {
                if (_loadFailed[i])
                    return;

                tex = ResourceLoader.Load<Texture2D>(_frames[i].TexturePath);
                if (tex == null)
                {
                    _loadFailed[i] = true;
                    return;
                }

                _cache[i] = tex;
            }

            _sprite.Texture = tex;
        }

        internal static CueFrameSequencePlayer EnsureUnder(Node parent)
        {
            if (parent.GetNodeOrNull(NodeName) is CueFrameSequencePlayer existing)
                return existing;

            var player = new CueFrameSequencePlayer();
            player.Name = NodeName;
            parent.AddChild(player);
            return player;
        }

        internal static void StopUnder(Node? parent)
        {
            if (!IsInstanceValid(parent))
                return;

            (parent.GetNodeOrNull(NodeName) as CueFrameSequencePlayer)?.StopAndReset();
        }
    }
}
