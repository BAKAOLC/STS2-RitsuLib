using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Persistence.Migration
{
    /// <summary>
    ///     Interface for data migrations
    ///     数据迁移接口。
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        ///     The version this migration upgrades FROM
        ///     此迁移的起始版本。
        /// </summary>
        int FromVersion { get; }

        /// <summary>
        ///     The version this migration upgrades TO
        ///     此迁移的目标版本。
        /// </summary>
        int ToVersion { get; }

        /// <summary>
        ///     Perform the migration on the JSON data
        ///     对 JSON 数据执行迁移。
        /// </summary>
        /// <param name="data">
        ///     The JSON data to migrate.
        ///     要迁移的 JSON 数据。
        /// </param>
        /// <returns>
        ///     True if migration succeeded, false otherwise.
        ///     迁移成功时返回 true，否则返回 false。
        /// </returns>
        bool Migrate(JsonObject data);
    }
}
