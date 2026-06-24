using STS2RitsuLib.Content;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Per-mod registration surface for extra top-bar buttons. Mirrors the ergonomics of
    ///     <c>ModCardPileRegistry</c> (fluent <c>For(modId).RegisterOwned(localStem, spec)</c>), but is
    ///     fully decoupled from the card-pile subsystem — the button only knows how to show an icon,
    ///     expose a hover-tip and run a click callback.
    ///     每个 mod 的额外顶部栏按钮注册入口。对应
    ///     <c>ModCardPileRegistry</c> 的易用性（流式 <c>For(modId).RegisterOwned(localStem, spec)</c>），但
    ///     与牌堆子系统完全解耦；按钮只负责显示图标、
    ///     暴露悬停提示并运行点击回调。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Ids follow the ritsulib <c>MODID_CATEGORY_TYPENAME</c> public-entry convention via
    ///         <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" /> (middle segment fixed to
    ///         <c>TOPBARBUTTON</c>). The registered id is also the stem for <c>static_hover_tips</c>
    ///         title / description keys (<c>{id}.title</c> / <c>{id}.description</c>).
    ///     </para>
    ///     <para>
    ///         Registrations do not need to be frozen alongside <c>ModelDb</c>: top-bar buttons are mounted
    ///         when the top bar node is ready, which happens after model init. The registry therefore
    ///         simply de-duplicates by id (same mod re-registering the same stem returns the existing
    ///         definition).
    ///     </para>
    ///     <para>
    ///         Id 通过 <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" /> 遵循 ritsulib <c>MODID_CATEGORY_TYPENAME</c>
    ///         公开条目约定
    ///         （中间段固定为 <c>TOPBARBUTTON</c>）。注册的 id 也是 <c>static_hover_tips</c>
    ///         标题/描述键（<c>{id}.title</c> / <c>{id}.description</c>）的 stem。
    ///     </para>
    ///     <para>
    ///         注册无需与 <c>ModelDb</c> 一起冻结：顶部栏按钮会在顶部栏节点 ready 时挂载，
    ///         这发生在模型初始化之后。因此注册表
    ///         只按 id 去重（同一 mod 以同一 stem 重新注册会返回现有
    ///         定义）。
    ///     </para>
    /// </remarks>
    public sealed class ModTopBarButtonRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModTopBarButtonRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ModTopBarButtonDefinition> Definitions =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Logger _logger;
        private readonly string _modId;

        private ModTopBarButtonRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" />, creating it on first use.
        ///     返回 <paramref name="modId" /> 的单例注册表，首次使用时创建。
        /// </summary>
        public static ModTopBarButtonRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModTopBarButtonRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     Registers a top-bar button owned by this registry's mod. The id is produced by
        ///     <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" /> — passing the same
        ///     <paramref name="localStem" /> twice returns the existing definition.
        ///     注册一个由此注册表的 mod 拥有的顶部栏按钮。id 由
        ///     <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" /> 生成；传入相同的
        ///     <paramref name="localStem" /> 两次会返回现有定义。
        /// </summary>
        public ModTopBarButtonDefinition RegisterOwned(string localStem, ModTopBarButtonSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);
            ArgumentNullException.ThrowIfNull(spec);

            var id = ModContentRegistry.GetQualifiedTopBarButtonId(_modId, localStem);
            return RegisterCore(id, spec);
        }

        /// <summary>
        ///     Registers a top-bar button using a raw global id. Prefer <see cref="RegisterOwned" /> to
        ///     keep ids mod-scoped.
        ///     注册 a 顶部栏按钮 使用原始全局 id. 优先使用 <see cref="RegisterOwned" /> to
        ///     保持 id 受 mod 作用域约束。
        /// </summary>
        public ModTopBarButtonDefinition Register(string id, ModTopBarButtonSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            return RegisterCore(id, spec);
        }

        /// <summary>
        ///     Looks up a definition by id; returns false when the id is unknown.
        ///     按 id 查找定义；id 未知时返回 false。
        /// </summary>
        public static bool TryGet(string id, out ModTopBarButtonDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            lock (SyncRoot)
            {
                return Definitions.TryGetValue(id.Trim(), out definition!);
            }
        }

        /// <summary>
        ///     Snapshot of every registered button, ordered by <see cref="ModTopBarButtonDefinition.Order" />
        ///     then id for stability.
        ///     每个已注册按钮的快照, 按排序 <see cref="ModTopBarButtonDefinition.Order" />
        ///     然后按 id 保持稳定。
        /// </summary>
        public static ModTopBarButtonDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return Definitions.Values
                    .OrderBy(def => def.Order)
                    .ThenBy(def => def.Id, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        private ModTopBarButtonDefinition RegisterCore(string id, ModTopBarButtonSpec spec)
        {
            var normalizedId = id.Trim();
            if (spec.OnClick == null)
                throw new InvalidOperationException(
                    $"Top-bar button '{normalizedId}' must provide a non-null OnClick handler.");

            var definition = new ModTopBarButtonDefinition(
                _modId,
                normalizedId,
                spec.IconPath,
                spec.Order,
                spec.Offset,
                spec.OnClick,
                spec.VisibleWhen,
                spec.IsOpenWhen,
                spec.CountProvider);

            lock (SyncRoot)
            {
                if (Definitions.TryGetValue(normalizedId, out var existing))
                {
                    if (!StringComparer.OrdinalIgnoreCase.Equals(existing.ModId, definition.ModId))
                        throw new InvalidOperationException(
                            $"Top-bar button '{normalizedId}' is already registered by mod '{existing.ModId}'; "
                            + $"mod '{definition.ModId}' cannot re-register it.");

                    return existing;
                }

                Definitions[normalizedId] = definition;
            }

            _logger.Info($"[TopBar] Registered top-bar button: {normalizedId} (Order={spec.Order})");
            return definition;
        }
    }
}
