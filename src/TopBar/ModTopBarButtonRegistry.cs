using STS2RitsuLib.Content;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     <para xml:lang="en">Registers mod-owned action buttons in the vanilla top bar.</para>
    ///     <para xml:lang="zh-CN">在原版顶部栏中注册归属于模组的操作按钮。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         <see cref="RegisterOwned" /> derives IDs through
    ///         <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" />. The resulting ID is also used
    ///         for <c>static_hover_tips</c> keys <c>{id}.title</c> and <c>{id}.description</c>.
    ///     </para>
    ///     <para xml:lang="en">
    ///         Register buttons before <see cref="MegaCrit.Sts2.Core.Nodes.CommonUi.NTopBar._Ready" /> creates
    ///         the current top bar. Repeating an ID for the same mod returns its existing definition; a
    ///         different mod cannot claim that ID.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="RegisterOwned" /> 通过
    ///         <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" /> 派生 ID。所得 ID 同时用于
    ///         <c>static_hover_tips</c> 中的 <c>{id}.title</c> 和 <c>{id}.description</c> 键。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         请在 <see cref="MegaCrit.Sts2.Core.Nodes.CommonUi.NTopBar._Ready" /> 创建当前顶部栏前完成注册。
    ///         同一模组重复注册同一 ID 时返回已有定义；其他模组不能占用该 ID。
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
        ///     <para xml:lang="en">
        ///         Gets the registry for <paramref name="modId" />, creating it on first use.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="modId" /> 的注册表；首次使用时创建该注册表。
        ///     </para>
        /// </summary>
        public static ModTopBarButtonRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            modId = modId.Trim();

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
        ///     <para xml:lang="en">
        ///         Registers a button whose global ID is derived from this registry's mod ID and
        ///         <paramref name="localStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册一个全局 ID 由当前注册表的模组 ID 和 <paramref name="localStem" /> 派生的按钮。
        ///     </para>
        /// </summary>
        public ModTopBarButtonDefinition RegisterOwned(string localStem, ModTopBarButtonSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localStem);
            ArgumentNullException.ThrowIfNull(spec);

            var id = ModContentRegistry.GetQualifiedTopBarButtonId(_modId, localStem);
            return RegisterCore(id, spec);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a top-bar button with a global ID. Prefer <see cref="RegisterOwned" /> to derive
        ///         a mod-qualified ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用全局 ID 注册顶部栏按钮。建议使用 <see cref="RegisterOwned" /> 派生带模组限定的 ID。
        ///     </para>
        /// </summary>
        public ModTopBarButtonDefinition Register(string id, ModTopBarButtonSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            return RegisterCore(id, spec);
        }

        /// <summary>
        ///     <para xml:lang="en">Looks up a definition by ID.</para>
        ///     <para xml:lang="zh-CN">按 ID 查找定义。</para>
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
        ///     <para xml:lang="en">
        ///         Returns a snapshot of all registered buttons, ordered by
        ///         <see cref="ModTopBarButtonDefinition.Order" /> and then ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回所有已注册按钮的快照，依次按 <see cref="ModTopBarButtonDefinition.Order" /> 和 ID 排序。
        ///     </para>
        /// </summary>
        public static ModTopBarButtonDefinition[] GetDefinitionsSnapshot()
        {
            lock (SyncRoot)
            {
                return
                [
                    .. Definitions.Values
                        .OrderBy(def => def.Order)
                        .ThenBy(def => def.Id, StringComparer.Ordinal),
                ];
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
