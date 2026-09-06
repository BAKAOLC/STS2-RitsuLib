using System.Collections.Immutable;

namespace STS2RitsuLib.Search
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Immutable matching policy owned by a caller. Explicit choices override the user's default only
    ///         for this policy; they never change settings, register providers, or load or download dictionaries.
    ///         A missing provider or dictionary produces no expanded matches. Direct text matching always remains enabled.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         由调用方持有的不可变匹配策略。显式选项仅为当前策略覆盖用户默认值，不会修改设置、注册提供器或
    ///         加载及下载词典。缺少提供器或词典时不产生扩展匹配。直接文本匹配始终启用。
    ///     </para>
    /// </summary>
    public sealed record RitsuSearchOptions
    {
        private readonly ImmutableDictionary<string, bool> _providerOverrides =
            ImmutableDictionary.Create<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes explicit per-provider choices, overriding user settings and
        ///         <see cref="UseOtherProviders" />. Keys are case-insensitive registered provider IDs; unknown IDs
        ///         remain inactive until registered. The input is copied; later caller mutations have no effect.
        ///         Pinyin's per-form choices, when set, take precedence over its provider choice.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化逐提供器的显式选项，覆盖用户设置及 <see cref="UseOtherProviders" />。
        ///         键为不区分大小写的提供器 ID；未知 ID 在注册前不生效。输入会复制，调用方后续修改不影响策略。
        ///         显式全拼及首字母选项优先于拼音提供器选项。
        ///     </para>
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">The dictionary is null.</para>
        ///     <para xml:lang="zh-CN">字典为 null。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">
        ///         More than 128 entries, duplicate case-insensitive IDs, or IDs not consisting of 1–128 ASCII
        ///         letters, digits, periods, hyphens, or underscores are supplied.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         超过 128 项、ID 不区分大小写重复，或 ID 不由 1–128 个 ASCII 字母、数字、点、连字符及下划线组成。
        ///     </para>
        /// </exception>
        public IReadOnlyDictionary<string, bool> ProviderOverrides
        {
            get => _providerOverrides;
            init
            {
                ArgumentNullException.ThrowIfNull(value);
                if (value.Count > 128)
                    throw new ArgumentException("Search policies cannot contain more than 128 provider overrides.",
                        nameof(value));
                var entries = ImmutableDictionary.CreateBuilder<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (var (id, enabled) in value)
                {
                    if (!IsValidProviderId(id) || entries.Count >= 128 || !entries.TryAdd(id, enabled))
                        throw new ArgumentException("Provider overrides require unique, valid provider IDs.",
                            nameof(value));
                }

                _providerOverrides = entries.ToImmutable();
            }
        }

        internal static bool IsValidProviderId(string? id)
        {
            return id is { Length: > 0 and <= 128 } && id.All(static character =>
                character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '-' or '_');
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared policy that follows user defaults for all registered providers.</para>
        ///     <para xml:lang="zh-CN">获取所有已注册提供器均遵循用户默认值的共享策略。</para>
        /// </summary>
        public static RitsuSearchOptions Default { get; } = new();

        /// <summary>
        ///     <para xml:lang="en">Gets a shared policy that never invokes expansion providers.</para>
        ///     <para xml:lang="zh-CN">获取从不调用扩展提供器的共享策略。</para>
        /// </summary>
        public static RitsuSearchOptions Literal { get; } = new()
        {
            Pinyin = false,
            PinyinInitials = false,
            UseOtherProviders = false,
        };

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes full pinyin matching, including alternate readings. Null follows the pinyin
        ///         provider override, or the user's setting when absent. This choice is independent of initials.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化包含多音字读音的全拼匹配。null 遵循拼音提供器覆盖值，未覆盖时使用用户设置；与首字母选项独立。
        ///     </para>
        /// </summary>
        public bool? Pinyin { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes pinyin-initial matching; null follows the provider override, or the user's setting when absent.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取或初始化拼音首字母匹配；null 遵循提供器覆盖值，未覆盖时使用用户设置。</para>
        /// </summary>
        public bool? PinyinInitials { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or initializes whether other user-enabled expansion providers may run. Defaults to true;
        ///         false excludes providers without an explicit override, without changing registration or user settings.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或初始化是否允许其他已由用户启用的扩展提供器运行。默认为 true；false 排除没有显式覆盖值的提供器，
        ///         不改变注册及用户设置。
        ///     </para>
        /// </summary>
        public bool UseOtherProviders { get; init; } = true;
    }
}
