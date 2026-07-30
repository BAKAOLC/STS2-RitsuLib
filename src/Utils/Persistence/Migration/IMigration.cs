using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Persistence.Migration
{
    /// <summary>
    ///     <para xml:lang="en">Defines a JSON-object data migration between two schema versions.</para>
    ///     <para xml:lang="zh-CN">定义两个架构版本之间的 JSON 对象数据迁移。</para>
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        ///     <para xml:lang="en">Inclusive lower bound of the source schema versions handled by this migration.</para>
        ///     <para xml:lang="zh-CN">此迁移可处理的源架构版本下限，包含该版本。</para>
        /// </summary>
        int FromVersion { get; }

        /// <summary>
        ///     <para xml:lang="en">Schema version produced by this migration.</para>
        ///     <para xml:lang="zh-CN">此迁移生成的架构版本。</para>
        /// </summary>
        int ToVersion { get; }

        /// <summary>
        ///     <para xml:lang="en">Performs the migration on the JSON data.</para>
        ///     <para xml:lang="zh-CN">对 JSON 数据执行迁移。</para>
        /// </summary>
        /// <param name="data">
        ///     <para xml:lang="en">JSON data to migrate.</para>
        ///     <para xml:lang="zh-CN">要迁移的 JSON 数据。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the migration succeeds; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">迁移成功时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        bool Migrate(JsonObject data);
    }
}
