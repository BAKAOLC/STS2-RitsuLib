using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <see cref="IAnimationBackend" /> multiplexer that keeps one backend active at a time and allows runtime
    ///     form switching (for example, swapping between multiple child visuals under one persistent
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals" /> root).
    ///     <see cref="IAnimationBackend" /> 复用器：同一时间只保持一个后端活动，并允许运行时
    ///     形态切换（例如在一个持久的 <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals" />
    ///     根节点下切换多个子视觉）。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This backend is intended for the "single visuals root, switch child form" pattern: each form gets its
    ///         own child backend (Spine, animated sprite, animation player, ...), and
    ///         <see cref="SwitchForm" /> swaps the active backend without rebuilding the creature node.
    ///     </para>
    ///     <para>
    ///         If <c>replayCurrent</c> is <see langword="true" />, switching replays the current logical
    ///         animation id on the newly selected form when possible; otherwise callers typically follow with an
    ///         explicit trigger (for example <c>SetTrigger("Idle")</c>).
    ///     </para>
    ///     <para>
    ///         此后端用于“单一视觉根节点、切换子形态”模式：每个形态都有自己的
    ///         子后端（Spine、animated sprite、animation player 等），并由
    ///         <see cref="SwitchForm" /> 在不重建生物节点的情况下切换活动后端。
    ///     </para>
    ///     <para>
    ///         如果 <c>replayCurrent</c> 为 <see langword="true" />，切换时会在可能的情况下于新选中的形态上重放当前逻辑
    ///         动画 id；否则调用方通常会接着显式触发
    ///         一次触发器（例如 <c>SetTrigger("Idle")</c>）。
    ///     </para>
    /// </remarks>
    public sealed class FormSwitchingAnimationBackend : IAnimationBackend
    {
        private readonly Dictionary<string, IAnimationBackend> _backendsByForm;
        private readonly Dictionary<string, bool> _loopByAnimationId = new(StringComparer.Ordinal);
        private string? _currentId;
        private bool _currentLoop;

        /// <summary>
        ///     Creates a switchable backend over prebuilt per-form backends.
        ///     基于预先构建的逐形态后端创建可切换后端。
        /// </summary>
        /// <param name="backendsByForm">
        ///     Map from stable form id to backend instance.
        ///     从稳定形态 id 到后端实例的映射。
        /// </param>
        /// <param name="initialFormId">
        ///     Initially active form id.
        ///     初始激活的形态 id。
        /// </param>
        /// <param name="ownerNode">
        ///     Optional owner node override.
        ///     可选拥有者节点覆盖。
        /// </param>
        public FormSwitchingAnimationBackend(
            IReadOnlyDictionary<string, IAnimationBackend> backendsByForm,
            string initialFormId,
            Node? ownerNode = null)
        {
            ArgumentNullException.ThrowIfNull(backendsByForm);
            ArgumentException.ThrowIfNullOrWhiteSpace(initialFormId);
            if (backendsByForm.Count == 0)
                throw new ArgumentException("At least one form backend is required.", nameof(backendsByForm));

            _backendsByForm = new(StringComparer.Ordinal);
            foreach (var (formId, backend) in backendsByForm)
            {
                if (string.IsNullOrWhiteSpace(formId))
                    throw new ArgumentException("Form id cannot be null or whitespace.", nameof(backendsByForm));

                ArgumentNullException.ThrowIfNull(backend);
                if (!_backendsByForm.TryAdd(formId, backend))
                    throw new ArgumentException($"Duplicate form id '{formId}'.", nameof(backendsByForm));

                backend.Started += id => OnChildStarted(backend, id);
                backend.Completed += id => OnChildCompleted(backend, id);
                backend.Interrupted += id => OnChildInterrupted(backend, id);
            }

            if (!_backendsByForm.ContainsKey(initialFormId))
                throw new ArgumentException(
                    $"Initial form '{initialFormId}' is missing from the backend map.",
                    nameof(initialFormId));

            ActiveFormId = initialFormId;
            OwnerNode = ownerNode ?? _backendsByForm[ActiveFormId].OwnerNode;
        }

        /// <summary>
        ///     Active form id.
        ///     当前激活形态 id。
        /// </summary>
        public string ActiveFormId { get; private set; }

        private IAnimationBackend CurrentBackend => _backendsByForm[ActiveFormId];

        /// <inheritdoc />
        public Node? OwnerNode { get; }

        /// <inheritdoc />
        public event Action<string>? Started;

        /// <inheritdoc />
        public event Action<string>? Completed;

        /// <inheritdoc />
        public event Action<string>? Interrupted;

        /// <inheritdoc />
        public bool HasAnimation(string id)
        {
            return CurrentBackend.HasAnimation(id);
        }

        /// <inheritdoc />
        public void Play(string id, bool loop)
        {
            _currentId = id;
            _currentLoop = loop;
            _loopByAnimationId[id] = loop;
            CurrentBackend.Play(id, loop);
        }

        /// <inheritdoc />
        public void Queue(string id, bool loop)
        {
            _loopByAnimationId[id] = loop;
            CurrentBackend.Queue(id, loop);
        }

        /// <inheritdoc />
        public void Stop()
        {
            _currentId = null;
            CurrentBackend.Stop();
        }

        /// <summary>
        ///     Switches the active form backend.
        ///     切换激活的形态后端。
        /// </summary>
        /// <param name="formId">
        ///     Target form id.
        ///     目标形态 id。
        /// </param>
        /// <param name="replayCurrent">
        ///     When true, replays current animation id on the new form if available.
        ///     为 true 时，如果新形态可用，则在新形态上重播当前动画 id。
        /// </param>
        /// <returns>
        ///     <see langword="true" /> when the active form changed.
        ///     激活形态发生变化时返回 <see langword="true" />。
        /// </returns>
        public bool SwitchForm(string formId, bool replayCurrent = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(formId);
            if (!_backendsByForm.ContainsKey(formId))
                return false;
            if (string.Equals(ActiveFormId, formId, StringComparison.Ordinal))
                return false;

            var previous = CurrentBackend;
            ActiveFormId = formId;
            previous.Stop();

            if (!replayCurrent || _currentId == null)
                return true;

            if (!CurrentBackend.HasAnimation(_currentId))
                return true;

            CurrentBackend.Play(_currentId, _currentLoop);
            return true;
        }

        private void OnChildStarted(IAnimationBackend child, string id)
        {
            if (!ReferenceEquals(child, CurrentBackend))
                return;

            _currentId = id;
            if (_loopByAnimationId.TryGetValue(id, out var loop))
                _currentLoop = loop;
            Started?.Invoke(id);
        }

        private void OnChildCompleted(IAnimationBackend child, string id)
        {
            if (!ReferenceEquals(child, CurrentBackend))
                return;

            Completed?.Invoke(id);
        }

        private void OnChildInterrupted(IAnimationBackend child, string id)
        {
            if (!ReferenceEquals(child, CurrentBackend))
                return;

            Interrupted?.Invoke(id);
        }
    }
}
