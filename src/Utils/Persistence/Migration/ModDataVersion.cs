namespace STS2RitsuLib.Utils.Persistence.Migration
{
    /// <summary>
    ///     <para xml:lang="en">Provides shared constants for mod-data JSON schema versioning.</para>
    ///     <para xml:lang="zh-CN">提供模组数据 JSON 架构版本控制使用的共享常量。</para>
    /// </summary>
    public static class ModDataVersion
    {
        /// <summary>
        ///     <para xml:lang="en">Default JSON property name for the persisted schema-version integer.</para>
        ///     <para xml:lang="zh-CN">持久化架构版本整数的默认 JSON 属性名。</para>
        /// </summary>
        public const string SchemaVersionProperty = "schema_version";
    }
}
