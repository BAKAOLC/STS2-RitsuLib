using STS2RitsuLib.Content;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Declares a top-bar button registration row for content packs. Registering the row with
    ///         <see cref="ModTopBarButtonRegistry" /> performs the corresponding button registration.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         声明内容包的顶部栏按钮注册行。将该行注册到 <see cref="ModTopBarButtonRegistry" /> 时会执行对应的按钮注册。
    ///     </para>
    /// </summary>
    public sealed record TopBarButtonRegistrationEntry
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a registration row for a provided global top-bar button ID.</para>
        ///     <para xml:lang="zh-CN">为给定的全局顶部栏按钮 ID 创建注册行。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">The button ID to register.</para>
        ///     <para xml:lang="zh-CN">要注册的按钮 ID。</para>
        /// </param>
        /// <param name="spec">
        ///     <para xml:lang="en">The button metadata and click behavior.</para>
        ///     <para xml:lang="zh-CN">按钮元数据和点击行为。</para>
        /// </param>
        public TopBarButtonRegistrationEntry(string id, ModTopBarButtonSpec spec)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(spec);

            Id = id;
            Spec = spec;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the button ID supplied by this row.</para>
        ///     <para xml:lang="zh-CN">获取此注册行提供的按钮 ID。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the button metadata and click behavior applied during registration.</para>
        ///     <para xml:lang="zh-CN">获取注册期间应用的按钮元数据和点击行为。</para>
        /// </summary>
        public ModTopBarButtonSpec Spec { get; }

        /// <summary>
        ///     <para xml:lang="en">Registers this row with <paramref name="registry" />.</para>
        ///     <para xml:lang="zh-CN">将此行注册到 <paramref name="registry" />。</para>
        /// </summary>
        public void Register(ModTopBarButtonRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);
            registry.Register(Id, Spec);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a registration row whose global button ID is qualified from <paramref name="modId" /> and
        ///         <paramref name="localButtonStem" /> through <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="ModContentRegistry.GetQualifiedTopBarButtonId" />，使用 <paramref name="modId" />
        ///         和 <paramref name="localButtonStem" /> 限定全局按钮 ID 后创建注册行。
        ///     </para>
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
