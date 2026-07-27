using System.Reflection;
using System.Runtime.CompilerServices;

namespace STS2RitsuLib.Utils.Speculation
{
    /// <summary>
    ///     Severity of a speculative-execution diagnostic.
    ///     推演执行诊断的严重程度。
    /// </summary>
    public enum SpeculativeDiagnosticSeverity
    {
        /// <summary>
        ///     Informational execution detail.
        ///     信息性执行详情。
        /// </summary>
        Information,

        /// <summary>
        ///     Recoverable condition that may reduce precision.
        ///     可能降低精度的可恢复状况。
        /// </summary>
        Warning,

        /// <summary>
        ///     Condition that makes the result incomplete.
        ///     导致结果不完整的错误状况。
        /// </summary>
        Error,
    }

    /// <summary>
    ///     Limits applied to one speculative execution.
    ///     单次推演执行使用的限制。
    /// </summary>
    public sealed record SpeculativeExecutionBudget
    {
        /// <summary>
        ///     Maximum number of accounted operations.
        ///     允许计入的最大操作数量。
        /// </summary>
        public int MaxOperations { get; init; } = 100_000;

        /// <summary>
        ///     Maximum nested execution-frame depth.
        ///     允许嵌套的最大执行帧深度。
        /// </summary>
        public int MaxDepth { get; init; } = 128;
    }

    /// <summary>
    ///     One diagnostic emitted during speculative execution.
    ///     推演执行期间产生的一条诊断。
    /// </summary>
    public sealed record SpeculativeDiagnostic(
        SpeculativeDiagnosticSeverity Severity,
        string Code,
        string Message,
        MethodBase? Method = null,
        int? InstructionIndex = null);

    /// <summary>
    ///     One ordered terminal effect recorded by a speculative execution.
    ///     推演执行记录的一条有序终点效果。
    /// </summary>
    public sealed record SpeculativeEffect(
        int Sequence,
        string Kind,
        object? Source,
        object? Payload);

    /// <summary>
    ///     Async-flowing execution context with a shadow-state overlay, ordered effect journal, and hard budgets.
    ///     随异步流传播的执行上下文，包含影子状态覆盖、有序效果日志和硬预算。
    /// </summary>
    public sealed class SpeculativeExecutionSession
    {
        private static readonly AsyncLocal<AmbientNode?> Ambient = new();

        private readonly List<SpeculativeDiagnostic> _diagnostics = [];
        private readonly List<SpeculativeEffect> _effects = [];
        private readonly Stack<object?> _sources = [];
        private readonly Dictionary<StateKey, object?> _state = new(StateKeyComparer.Instance);
        private int _depth;
        private bool _depthBudgetReported;
        private bool _operationBudgetReported;

        /// <summary>
        ///     Creates an isolated speculative-execution session.
        ///     创建一个独立的推演执行会话。
        /// </summary>
        public SpeculativeExecutionSession(SpeculativeExecutionBudget? budget = null)
        {
            Budget = budget ?? new();
            ArgumentOutOfRangeException.ThrowIfLessThan(Budget.MaxOperations, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(Budget.MaxDepth, 1);
        }

        /// <summary>
        ///     Session currently active in this async control flow, if any.
        ///     当前异步控制流中处于活动状态的会话；没有时为 null。
        /// </summary>
        public static SpeculativeExecutionSession? Current => Ambient.Value?.Session;

        /// <summary>
        ///     Limits applied to this session.
        ///     此会话使用的限制。
        /// </summary>
        public SpeculativeExecutionBudget Budget { get; }

        /// <summary>
        ///     Ordered effects recorded by this session.
        ///     此会话记录的有序效果。
        /// </summary>
        public IReadOnlyList<SpeculativeEffect> Effects => _effects;

        /// <summary>
        ///     Diagnostics recorded by this session.
        ///     此会话记录的诊断。
        /// </summary>
        public IReadOnlyList<SpeculativeDiagnostic> Diagnostics => _diagnostics;

        /// <summary>
        ///     Number of operations consumed so far.
        ///     当前已消耗的操作数量。
        /// </summary>
        public int Operations { get; private set; }

        /// <summary>
        ///     True when no error diagnostic was recorded.
        ///     未记录错误诊断时为 true。
        /// </summary>
        public bool IsComplete =>
            _diagnostics.All(static diagnostic =>
                diagnostic.Severity != SpeculativeDiagnosticSeverity.Error);

        /// <summary>
        ///     Makes this session current until the returned scope is disposed.
        ///     在返回的作用域释放前将此会话设为当前会话。
        /// </summary>
        public IDisposable Enter()
        {
            var previous = Ambient.Value;
            var node = new AmbientNode(this, previous);
            Ambient.Value = node;
            return new ScopeLease(() =>
            {
                if (!ReferenceEquals(Ambient.Value, node))
                    throw new InvalidOperationException(
                        "Speculative execution scopes must be disposed in reverse order.");
                Ambient.Value = previous;
            });
        }

        /// <summary>
        ///     Attempts to enter one nested execution frame.
        ///     尝试进入一个嵌套执行帧。
        /// </summary>
        /// <param name="method">
        ///     Method represented by the frame, when known.
        ///     此执行帧表示的方法（如已知）。
        /// </param>
        /// <param name="scope">
        ///     Scope that leaves the frame when disposed.
        ///     释放时离开执行帧的作用域。
        /// </param>
        /// <returns>
        ///     True when both operation and depth budgets permit the frame.
        ///     操作预算与深度预算均允许此执行帧时为 true。
        /// </returns>
        public bool TryEnterFrame(MethodBase? method, out IDisposable scope)
        {
            if (!TryConsumeOperation(method))
            {
                scope = ScopeLease.Empty;
                return false;
            }

            if (_depth >= Budget.MaxDepth)
            {
                if (!_depthBudgetReported)
                {
                    _depthBudgetReported = true;
                    AddDiagnostic(new(
                        SpeculativeDiagnosticSeverity.Error,
                        "depth_budget_exceeded",
                        $"Execution depth budget {Budget.MaxDepth} was exceeded.",
                        method));
                }

                scope = ScopeLease.Empty;
                return false;
            }

            _depth++;
            scope = new ScopeLease(() => _depth--);
            return true;
        }

        /// <summary>
        ///     Accounts one operation and reports whether execution may continue.
        ///     计入一个操作并报告执行是否可以继续。
        /// </summary>
        public bool TryConsumeOperation(MethodBase? method = null, int? instructionIndex = null)
        {
            if (Operations >= Budget.MaxOperations)
            {
                if (_operationBudgetReported) return false;
                _operationBudgetReported = true;
                AddDiagnostic(new(
                    SpeculativeDiagnosticSeverity.Error,
                    "operation_budget_exceeded",
                    $"Operation budget {Budget.MaxOperations} was exceeded.",
                    method,
                    instructionIndex));

                return false;
            }

            Operations++;
            return true;
        }

        /// <summary>
        ///     Pushes a causal source used by effects that do not supply an explicit source.
        ///     压入因果来源，供未显式指定来源的效果使用。
        /// </summary>
        public IDisposable PushSource(object? source)
        {
            _sources.Push(source);
            return new ScopeLease(() => _sources.Pop());
        }

        /// <summary>
        ///     Appends one terminal effect to the ordered journal.
        ///     向有序日志追加一条终点效果。
        /// </summary>
        public SpeculativeEffect RecordEffect(
            string kind,
            object? payload = null,
            object? source = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(kind);
            var effect = new SpeculativeEffect(
                _effects.Count,
                kind,
                source ?? (_sources.TryPeek(out var ambientSource) ? ambientSource : null),
                payload);
            _effects.Add(effect);
            return effect;
        }

        /// <summary>
        ///     Appends a diagnostic.
        ///     追加一条诊断。
        /// </summary>
        public void AddDiagnostic(SpeculativeDiagnostic diagnostic)
        {
            ArgumentNullException.ThrowIfNull(diagnostic);
            _diagnostics.Add(diagnostic);
        }

        /// <summary>
        ///     Reads an overlaid value or obtains and caches its original value.
        ///     读取覆盖值；不存在时读取并缓存原始值。
        /// </summary>
        public T GetState<T>(
            object target,
            object slot,
            Func<T> readOriginal)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(slot);
            ArgumentNullException.ThrowIfNull(readOriginal);

            var key = new StateKey(target, slot);
            if (_state.TryGetValue(key, out var value))
                return CastState<T>(value, slot);

            var original = readOriginal();
            _state.Add(key, original);
            return original;
        }

        /// <summary>
        ///     Writes a value to the session overlay without modifying the target object.
        ///     将值写入会话覆盖层而不修改目标对象。
        /// </summary>
        public void SetState<T>(object target, object slot, T value)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(slot);
            _state[new(target, slot)] = value;
        }

        /// <summary>
        ///     Returns whether this session contains an overlaid value.
        ///     返回此会话是否包含指定覆盖值。
        /// </summary>
        public bool HasState(object target, object slot)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(slot);
            return _state.ContainsKey(new(target, slot));
        }

        private static T CastState<T>(object? value, object slot)
        {
            return value switch
            {
                T typed => typed,
                null when default(T) == null => default!,
                _ => throw new InvalidCastException(
                    $"Shadow slot '{slot}' contains {value?.GetType().FullName ?? "null"}, not {typeof(T).FullName}."),
            };
        }

        private sealed record AmbientNode(
            SpeculativeExecutionSession Session,
            AmbientNode? Previous);

        private readonly record struct StateKey(object Target, object Slot);

        private sealed class StateKeyComparer : IEqualityComparer<StateKey>
        {
            public static StateKeyComparer Instance { get; } = new();

            public bool Equals(StateKey x, StateKey y)
            {
                return ReferenceEquals(x.Target, y.Target) &&
                       Equals(x.Slot, y.Slot);
            }

            public int GetHashCode(StateKey obj)
            {
                return HashCode.Combine(
                    RuntimeHelpers.GetHashCode(obj.Target),
                    obj.Slot);
            }
        }

        private sealed class ScopeLease(Action? dispose) : IDisposable
        {
            private Action? _dispose = dispose;

            public static ScopeLease Empty { get; } = new(null);

            public void Dispose()
            {
                Interlocked.Exchange(ref _dispose, null)?.Invoke();
            }
        }
    }
}
