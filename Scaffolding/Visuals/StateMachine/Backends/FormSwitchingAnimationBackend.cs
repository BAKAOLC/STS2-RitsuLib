using Godot;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends
{
    /// <summary>
    ///     <see cref="IAnimationBackend" /> multiplexer that keeps one backend active at a time and allows runtime
    ///     form switching (for example, swapping between multiple child visuals under one persistent
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals" /> root).
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
    /// </remarks>
    public sealed class FormSwitchingAnimationBackend : IAnimationBackend
    {
        private readonly Dictionary<string, IAnimationBackend> _backendsByForm;
        private readonly Dictionary<string, bool> _loopByAnimationId = new(StringComparer.Ordinal);
        private string? _currentId;
        private bool _currentLoop;

        /// <summary>
        ///     Creates a switchable backend over prebuilt per-form backends.
        /// </summary>
        /// <param name="backendsByForm">Map from stable form id to backend instance.</param>
        /// <param name="initialFormId">Initially active form id.</param>
        /// <param name="ownerNode">Optional owner node override.</param>
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
        /// </summary>
        /// <param name="formId">Target form id.</param>
        /// <param name="replayCurrent">
        ///     When true, replays current animation id on the new form if available.
        /// </param>
        /// <returns><see langword="true" /> when the active form changed.</returns>
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
