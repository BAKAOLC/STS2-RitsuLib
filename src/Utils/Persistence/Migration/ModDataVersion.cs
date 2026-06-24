namespace STS2RitsuLib.Utils.Persistence.Migration
{
    /// <summary>
    ///     Shared constants for mod JSON persistence versioning.
    ///     Mod JSON 持久化版本控制的共享常量。
    /// </summary>
    public static class ModDataVersion
    {
        /// <summary>
        ///     Default JSON property name for the persisted schema version integer.
        ///     持久化 schema 版本整数的默认 JSON 属性名。
        /// </summary>
        public const string SchemaVersionProperty = "schema_version";
    }
}
