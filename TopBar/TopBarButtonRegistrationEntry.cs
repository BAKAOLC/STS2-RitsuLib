using STS2RitsuLib.Content;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Declarative top-bar-button row for content packs: register with <see cref="ModTopBarButtonRegistry" /> in
    ///     one call.
    ///     内容包的声明式顶部栏按钮行：通过 <see cref="ModTopBarButtonRegistry" /> 一次调用完成注册。
    /// </summary>
    public sealed record TopBarButtonRegistrationEntry
    {
        /// <summary>
        ///     Creates a raw global top-bar-button registration row.
        ///     创建原始全局顶部栏按钮注册行。
        /// </summary>
        /// <param name="id">
        ///     Registered button id.
        ///     已注册的按钮 id。
        /// </param>
        /// <param name="spec">
        ///     Button metadata and click behavior.
        ///     按钮元数据和点击行为。
        /// </param>
        public TopBarButtonRegistrationEntry(string id, ModTopBarButtonSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            Id = id;
            Spec = spec;
        }

        /// <summary>
        ///     Registered button id.
        ///     已注册的按钮 id。
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Button metadata and click behavior applied during registration.
        ///     按钮元数据和点击行为 在注册期间应用。
        /// </summary>
        public ModTopBarButtonSpec Spec { get; }

        /// <summary>
        ///     Registers this row against <paramref name="registry" />.
        ///     将此行注册到 <paramref name="registry" />。
        /// </summary>
        public void Register(ModTopBarButtonRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);
            registry.Register(Id, Spec);
        }

        /// <summary>
        ///     Builds an owned button id via <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" /> and registers it.
        ///     通过构建 owned 按钮 id <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" /> 并注册它。
        /// </summary>
        public static TopBarButtonRegistrationEntry Owned(
            string modId,
            string localButtonStem,
            ModTopBarButtonSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localButtonStem);
            ArgumentNullException.ThrowIfNull(spec);

            return new(ModContentRegistry.GetQualifiedTopBarButtonId(modId, localButtonStem), spec);
        }
    }
}
