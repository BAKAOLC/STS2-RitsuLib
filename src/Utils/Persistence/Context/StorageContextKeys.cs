namespace STS2RitsuLib.Utils.Persistence.Context
{
    /// <summary>
    ///     <para xml:lang="en">Provides built-in <see cref="StorageContextKey{TValue}" /> values used by RitsuLib persistence.</para>
    ///     <para xml:lang="zh-CN">提供 RitsuLib 持久化所用的内置 <see cref="StorageContextKey{TValue}" /> 值。</para>
    /// </summary>
    public static class StorageContextKeys
    {
        /// <summary>
        ///     <para xml:lang="en">Overrides the active game profile ID for a persistence operation.</para>
        ///     <para xml:lang="zh-CN">覆盖持久化操作的活动游戏档案 ID。</para>
        /// </summary>
        public static StorageContextKey<int> ProfileId { get; } = new("sts2ritsulib.profileId");
    }
}
