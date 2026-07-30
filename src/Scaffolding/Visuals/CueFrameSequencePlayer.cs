using Godot;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Visuals
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Drives a <see cref="Sprite2D.Texture" /> through the frames of a <see cref="VisualFrameSequence" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         按 <see cref="VisualFrameSequence" /> 中的帧依次切换 <see cref="Sprite2D.Texture" />。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Emits <see cref="SignalName.Finished" /> when a non-looping sequence reaches the end of its final
    ///         frame. <c>CueAnimationBackend</c> consumes the signal so
    ///         <see cref="StateMachine.ModAnimStateMachine" /> can advance to
    ///         <see cref="StateMachine.ModAnimState.NextState" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当非循环序列播放完最后一帧时发出 <see cref="SignalName.Finished" />。
    ///         <c>CueAnimationBackend</c> 使用此信号，使 <see cref="StateMachine.ModAnimStateMachine" />
    ///         能够推进到 <see cref="StateMachine.ModAnimState.NextState" />。
    ///     </para>
    /// </remarks>
    internal partial class CueFrameSequencePlayer : Node
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Raised when a non-looping sequence completes. Looping sequences do not raise this signal.
        ///     </para>
        ///     <para xml:lang="zh-CN">当非循环序列播放完成时触发；循环序列不会触发此信号。</para>
        /// </summary>
        [Signal]
        public delegate void FinishedEventHandler();

        internal const string NodeName = "RitsuCueFrameSequencePlayer";
        private bool _active;
        private Texture2D?[] _cache = [];
        private double _carry;
        private VisualNodeStyle? _defaultStyle;
        private double _frameDurationSeconds;
        private VisualFrame[] _frames = [];
        private VisualNodeStyle?[] _frameStyles = [];
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
            _defaultStyle = null;
            _frameStyles = [];
            _cache = [];
            _loadFailed = [];
            _index = 0;
            _carry = 0;
            SetProcess(false);
        }

        internal bool TryStart(Sprite2D sprite, VisualFrameSequence sequence)
        {
            StopAndReset();

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

            _sprite = sprite;
            _frames = frames;
            _defaultStyle = sequence.DefaultStyle;
            _frameStyles = BuildFrameStyleArray(sequence, frames.Length);
            _cache = new Texture2D?[frames.Length];
            _loadFailed = new bool[frames.Length];
            _loop = sequence.Loop;
            _index = 0;
            _carry = 0;
            _frameDurationSeconds = ClampFrameDuration(frames[0].DurationSeconds);
            if (!ApplyFrame(0))
            {
                StopAndReset();
                return false;
            }

            _active = true;
            SetProcess(true);
            return true;
        }

        internal bool TryGetRemaining(out float seconds)
        {
            seconds = 0f;
            if (!_active || _frames.Length == 0 || _index < 0 || _index >= _frames.Length)
                return false;

            var remaining = Math.Max(0.0, _frameDurationSeconds - _carry);
            for (var i = _index + 1; i < _frames.Length; i++)
                remaining += ClampFrameDuration(_frames[i].DurationSeconds);

            if (!double.IsFinite(remaining) || remaining < 0.0 || remaining > float.MaxValue)
                return false;

            seconds = (float)remaining;
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
            EmitSignal(SignalName.Finished);
        }

        private static double ClampFrameDuration(float seconds)
        {
            return !float.IsFinite(seconds) || seconds <= 0f ? 1.0 / 60.0 : seconds;
        }

        private bool ApplyFrame(int i)
        {
            if (_sprite == null || i < 0 || i >= _frames.Length)
                return false;

            var tex = _cache[i];
            if (tex == null)
            {
                if (_loadFailed[i])
                    return false;

                tex = ResourceLoader.Load<Texture2D>(_frames[i].TexturePath);
                if (tex == null)
                {
                    _loadFailed[i] = true;
                    return false;
                }

                _cache[i] = tex;
            }

            _sprite.Texture = tex;
            var style = _frameStyles.Length > i ? _frameStyles[i] : null;
            (style ?? _defaultStyle).ApplyTo(_sprite);
            return true;
        }

        private static VisualNodeStyle?[] BuildFrameStyleArray(VisualFrameSequence sequence, int frameCount)
        {
            if (frameCount == 0)
                return [];

            var source = sequence.FrameStyles;
            if (source == null || source.Count == 0)
                return new VisualNodeStyle?[frameCount];

            var styles = new VisualNodeStyle?[frameCount];
            var n = Math.Min(frameCount, source.Count);
            for (var i = 0; i < n; i++)
                styles[i] = source[i];

            return styles;
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
