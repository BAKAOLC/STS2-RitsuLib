using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Persistence.Interop
{
    /// <summary>
    ///     JSON-object persistence payload compatible with <see cref="Data.ModDataStore.Register{T}" /> generic constraints.
    ///     The logical document lives under <see cref="Root" />; file JSON is this wrapper plus optional schema version
    ///     fields.
    /// </summary>
    public sealed class ModDataInteropJsonDocument
    {
        /// <summary>
        ///     Primary JSON object migrated and synchronized with interop providers.
        /// </summary>
        public JsonObject Root { get; set; } = new();
    }
}
