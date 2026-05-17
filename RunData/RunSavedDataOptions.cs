using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     Options for a run-saved data slot.
    ///     跑局保存数据槽位的选项。
    /// </summary>
    public sealed class RunSavedDataOptions
    {
        /// <summary>
        ///     Current schema version written for this slot.
        ///     此槽位写入的当前 schema 版本。
        /// </summary>
        public int SchemaVersion { get; init; } = 1;

        /// <summary>
        ///     Determines when the slot is written.
        ///     决定何时写入此槽位。
        /// </summary>
        public RunSavedDataWritePolicy WritePolicy { get; init; } = RunSavedDataWritePolicy.WhenSet;

        /// <summary>
        ///     When true, writes through <see cref="RunSavedDataLobbyScope{T}" /> or
        ///     <see cref="PlayerRunSavedDataLobbyScope{T}" /> push a lobby contribution sync in multiplayer.
        ///     为 true 时，通过 <see cref="RunSavedDataLobbyScope{T}" /> 或
        ///     <see cref="PlayerRunSavedDataLobbyScope{T}" /> 写入后会在多人游戏中推送大厅贡献同步。
        /// </summary>
        public bool SyncLobbyOnChange { get; init; }

        /// <summary>
        ///     Optional slot migrations.
        ///     可选的槽位迁移。
        /// </summary>
        public IReadOnlyList<IMigration>? Migrations { get; init; }
    }
}
