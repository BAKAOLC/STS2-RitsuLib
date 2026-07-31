using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     <para xml:lang="en">Configures a run saved-data slot.</para>
    ///     <para xml:lang="zh-CN">配置跑局保存数据槽位。</para>
    /// </summary>
    public sealed class RunSavedDataOptions
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the current schema version written for this slot.</para>
        ///     <para xml:lang="zh-CN">获取此槽位写入数据时使用的当前架构版本。</para>
        /// </summary>
        public int SchemaVersion { get; init; } = 1;

        /// <summary>
        ///     <para xml:lang="en">Gets the policy that determines when this slot is written.</para>
        ///     <para xml:lang="zh-CN">获取决定何时写入此槽位的策略。</para>
        /// </summary>
        public RunSavedDataWritePolicy WritePolicy { get; init; } = RunSavedDataWritePolicy.WhenSet;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether writes through <see cref="RunSavedDataLobbyScope{T}" /> or
        ///         <see cref="PlayerRunSavedDataLobbyScope{T}" /> push a contribution update in multiplayer.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取多人游戏中通过 <see cref="RunSavedDataLobbyScope{T}" /> 或
        ///         <see cref="PlayerRunSavedDataLobbyScope{T}" /> 写入后是否推送贡献更新。
        ///     </para>
        /// </summary>
        public bool SyncLobbyOnChange { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional migrations for this slot.</para>
        ///     <para xml:lang="zh-CN">获取此槽位的可选迁移。</para>
        /// </summary>
        public IReadOnlyList<IMigration>? Migrations { get; init; }
    }
}
