using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Multiplexes per-form <see cref="IAnimationBackend" /> instances, keeping one active while allowing
    ///         runtime form changes beneath a persistent <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals" /> root.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         复用各形态的 <see cref="IAnimationBackend" /> 实例；在持久的
    ///         <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals" /> 根节点下切换形态时仅保持一个后端活动。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         This backend is intended for the "single visuals root, switch child form" pattern: each form gets its
    ///         own child backend (Spine, animated sprite, animation player, ...), and
    ///         <see cref="SwitchForm" /> swaps the active backend without rebuilding the creature node.
    ///     </para>
    ///     <para xml:lang="en">
    ///         If <c>replayCurrent</c> is <see langword="true" />, switching replays the current logical
    ///         animation id on the newly selected form when possible; otherwise callers typically follow with an
    ///         explicit trigger (for example <c>SetTrigger("Idle")</c>).
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         此后端用于“单一视觉根节点、切换子形态”模式：每个形态都有自己的
    ///         子后端（Spine、动画精灵、动画播放器等），并由
    ///         <see cref="SwitchForm" /> 在不重建生物节点的情况下切换活动后端。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         如果 <c>replayCurrent</c> 为 <see langword="true" />，切换时会在可能的情况下于新选中的形态上重放当前逻辑
    ///         动画 ID；否则调用方通常会紧接着显式触发一次状态切换（例如 <c>SetTrigger("Idle")</c>）。
    ///     </para>
    /// </remarks>
    public sealed class FormSwitchingAnimationBackend : IAnimationBackend, IAnimationTimingProvider
    {
        private readonly Dictionary<string, IAnimationBackend> _backendsByForm;
        private readonly Dictionary<string, bool> _loopByAnimationId = new(StringComparer.Ordinal);
        private string? _currentId;
        private bool _currentLoop;
        private string? _queuedId;
        private bool _queuedLoop;

        /// <summary>
        ///     <para xml:lang="en">Creates a switchable backend over prebuilt per-form backends.</para>
        ///     <para xml:lang="zh-CN">从预先构建的各形态后端创建可切换后端。</para>
        /// </summary>
        /// <param name="backendsByForm">
        ///     <para xml:lang="en">Maps stable form IDs to backend instances.</para>
        ///     <para xml:lang="zh-CN">从稳定形态 ID 到后端实例的映射。</para>
        /// </param>
        /// <param name="initialFormId">
        ///     <para xml:lang="en">The initially active form ID.</para>
        ///     <para xml:lang="zh-CN">初始激活的形态 ID。</para>
        /// </param>
        /// <param name="ownerNode">
        ///     <para xml:lang="en">Optional owner-node override.</para>
        ///     <para xml:lang="zh-CN">可选的所有者节点覆盖。</para>
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
        ///     <para xml:lang="en">Gets the active form ID.</para>
        ///     <para xml:lang="zh-CN">获取当前激活的形态 ID。</para>
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
            if (!CurrentBackend.HasAnimation(id))
                return;

            _queuedId = null;
            _currentId = id;
            _currentLoop = loop;
            _loopByAnimationId[id] = loop;
            CurrentBackend.Play(id, loop);
        }

        /// <inheritdoc />
        public void Queue(string id, bool loop)
        {
            if (!CurrentBackend.HasAnimation(id))
                return;

            _queuedId = id;
            _queuedLoop = loop;
            _loopByAnimationId[id] = loop;
            CurrentBackend.Queue(id, loop);
        }

        /// <inheritdoc />
        public void Stop()
        {
            _currentId = null;
            _queuedId = null;
            CurrentBackend.Stop();
        }

        /// <inheritdoc />
        public bool TryGetAnimationDuration(string id, out float seconds)
        {
            seconds = 0f;
            return CurrentBackend is IAnimationTimingProvider timing &&
                   timing.TryGetAnimationDuration(id, out seconds);
        }

        /// <inheritdoc />
        public bool TryGetCurrentAnimationRemaining(out float seconds)
        {
            seconds = 0f;
            return CurrentBackend is IAnimationTimingProvider timing &&
                   timing.TryGetCurrentAnimationRemaining(out seconds);
        }

        /// <summary>
        ///     <para xml:lang="en">Switches the active form backend.</para>
        ///     <para xml:lang="zh-CN">切换当前激活的形态后端。</para>
        /// </summary>
        /// <param name="formId">
        ///     <para xml:lang="en">The target form ID.</para>
        ///     <para xml:lang="zh-CN">目标形态 ID。</para>
        /// </param>
        /// <param name="replayCurrent">
        ///     <para xml:lang="en">Whether to replay the current animation ID on the new form when it is available.</para>
        ///     <para xml:lang="zh-CN">新形态具备该动画时，是否重播当前动画 ID。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the active form changed.</para>
        ///     <para xml:lang="zh-CN">当前激活的形态发生变化时为 <see langword="true" />。</para>
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
            {
                _queuedId = null;
                return true;
            }

            if (!CurrentBackend.HasAnimation(_currentId))
            {
                _queuedId = null;
                return true;
            }

            var queuedId = _queuedId;
            var queuedLoop = _queuedLoop;
            CurrentBackend.Play(_currentId, _currentLoop);
            if (queuedId != null && CurrentBackend.HasAnimation(queuedId))
            {
                _queuedId = queuedId;
                _queuedLoop = queuedLoop;
                CurrentBackend.Queue(queuedId, queuedLoop);
            }
            else
            {
                _queuedId = null;
            }

            return true;
        }

        private void OnChildStarted(IAnimationBackend child, string id)
        {
            if (!ReferenceEquals(child, CurrentBackend))
                return;

            _currentId = id;
            if (_loopByAnimationId.TryGetValue(id, out var loop))
                _currentLoop = loop;
            if (string.Equals(_queuedId, id, StringComparison.Ordinal))
                _queuedId = null;
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
