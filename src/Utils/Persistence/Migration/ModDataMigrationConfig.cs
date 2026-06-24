namespace STS2RitsuLib.Utils.Persistence.Migration
{
    /// <summary>
    ///     Declares supported JSON schema versions for a mod persistence type.
    ///     声明 mod 持久化类型支持的 JSON schema 版本。
    /// </summary>
    public sealed class ModDataMigrationConfig
    {
        /// <summary>
        ///     Current on-disk schema version written by new saves.
        ///     新存档写入的当前磁盘 schema 版本。
        /// </summary>
        public required int CurrentDataVersion { get; init; }

        /// <summary>
        ///     Oldest schema version that will still be migrated (below this triggers recovery).
        ///     仍会迁移的最旧 schema 版本（低于此值会触发恢复）。
        /// </summary>
        public int MinimumSupportedDataVersion { get; init; }

        /// <summary>
        ///     JSON property name storing the integer schema version.
        ///     存储整数 schema 版本的 JSON 属性名。
        /// </summary>
        public string SchemaVersionProperty { get; init; } = ModDataVersion.SchemaVersionProperty;
    }
}
