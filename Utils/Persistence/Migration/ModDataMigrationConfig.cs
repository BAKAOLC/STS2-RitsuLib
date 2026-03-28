namespace STS2RitsuLib.Utils.Persistence.Migration
{
    /// <summary>
    ///     Declares supported JSON schema versions for a mod persistence type.
    /// </summary>
    public sealed class ModDataMigrationConfig
    {
        /// <summary>
        ///     Current on-disk schema version written by new saves.
        /// </summary>
        public required int CurrentDataVersion { get; init; }

        /// <summary>
        ///     Oldest schema version that will still be migrated (below this triggers recovery).
        /// </summary>
        public int MinimumSupportedDataVersion { get; init; }

        /// <summary>
        ///     JSON property name storing the integer schema version.
        /// </summary>
        public string SchemaVersionProperty { get; init; } = ModDataVersion.SchemaVersionProperty;
    }
}
