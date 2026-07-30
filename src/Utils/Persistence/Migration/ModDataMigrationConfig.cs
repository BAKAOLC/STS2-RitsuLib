namespace STS2RitsuLib.Utils.Persistence.Migration
{
    /// <summary>
    ///     <para xml:lang="en">Declares the JSON schema versions supported by a persistent mod-data type.</para>
    ///     <para xml:lang="zh-CN">声明一种持久化模组数据类型所支持的 JSON 架构版本。</para>
    /// </summary>
    public sealed class ModDataMigrationConfig
    {
        /// <summary>
        ///     <para xml:lang="en">Current schema version targeted by migration.</para>
        ///     <para xml:lang="zh-CN">迁移所面向的当前架构版本。</para>
        /// </summary>
        public required int CurrentDataVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Oldest schema version that can still be migrated; older versions require recovery.</para>
        ///     <para xml:lang="zh-CN">仍可迁移的最旧架构版本；更早的版本需要执行恢复。</para>
        /// </summary>
        public int MinimumSupportedDataVersion { get; init; }

        /// <summary>
        ///     <para xml:lang="en">JSON property name that stores the integer schema version.</para>
        ///     <para xml:lang="zh-CN">存储整数架构版本的 JSON 属性名。</para>
        /// </summary>
        public string SchemaVersionProperty { get; init; } = ModDataVersion.SchemaVersionProperty;
    }
}
