namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Names of static provider methods bound by <c>ReflectionStaticChannelBinder</c>.
    /// </summary>
    public sealed class ReflectionInteropConvention
    {
        /// <summary>
        ///     Name of the required static method <c>(string key) → object?</c> that reads a keyed payload from the
        ///     provider.
        /// </summary>
        public required string ObjectGetMethodName { get; init; }

        /// <summary>
        ///     Name of the required static method <c>(string key, object? value) → void</c> that writes a keyed
        ///     payload to the provider.
        /// </summary>
        public required string ObjectSetMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for RFC 7386 merge-patch retrieval: <c>(string key) → JsonObject?</c>.
        /// </summary>
        public string? MergePatchGetMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for applying a merge patch: <c>(string key, JsonObject patch) → void</c>.
        /// </summary>
        public string? MergePatchApplyMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for JSON Pointer node read:
        ///     <c>(string key, string jsonPointer) → JsonNode?</c>.
        /// </summary>
        public string? NodeGetMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for JSON Pointer node write:
        ///     <c>(string key, string jsonPointer, JsonNode? node) → void</c>.
        /// </summary>
        public string? NodeSetMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for merging a <see cref="System.Text.Json.Nodes.JsonObject" /> at a
        ///     pointer: <c>(string key, string jsonPointer, JsonObject value) → void</c>.
        /// </summary>
        public string? ObjectMergeAtMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for whole-document JSON text read: <c>(string key) → string?</c>.
        /// </summary>
        public string? TypedGetJsonMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for whole-document JSON text write:
        ///     <c>(string key, string json) → void</c>.
        /// </summary>
        public string? TypedSetJsonMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for root <see cref="System.Text.Json.Nodes.JsonObject" /> read:
        ///     <c>(string key) → JsonObject?</c>.
        /// </summary>
        public string? TypedGetJsonObjectMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for root <see cref="System.Text.Json.Nodes.JsonObject" /> write:
        ///     <c>(string key, JsonObject root) → void</c>.
        /// </summary>
        public string? TypedSetJsonObjectMethodName { get; init; }

        /// <summary>
        ///     Convention for <c>CreateRitsuLibModDataSchema</c> / ModData runtime interop providers.
        /// </summary>
        public static ReflectionInteropConvention ModData { get; } = new()
        {
            ObjectGetMethodName = "GetRitsuLibModDataValue",
            ObjectSetMethodName = "SetRitsuLibModDataValue",
            MergePatchGetMethodName = "GetRitsuLibModDataMergePatch",
            MergePatchApplyMethodName = "ApplyRitsuLibModDataMergePatch",
            NodeGetMethodName = "GetRitsuLibModDataNode",
            NodeSetMethodName = "SetRitsuLibModDataNode",
            ObjectMergeAtMethodName = "MergeRitsuLibModDataObject",
            TypedGetJsonMethodName = "GetRitsuLibModDataJson",
            TypedSetJsonMethodName = "SetRitsuLibModDataJson",
            TypedGetJsonObjectMethodName = "GetRitsuLibModDataJsonObject",
            TypedSetJsonObjectMethodName = "SetRitsuLibModDataJsonObject",
        };

        /// <summary>
        ///     Object resolvers for settings runtime interop. Optional JSON DOM tier names are left unset so
        ///     existing providers keep working; add a new <see cref="ReflectionInteropConvention" /> with
        ///     custom method names if a settings provider later opts into merge / pointer / text tiers.
        ///     Typed bool/int/double/string accessors remain handled by the settings runtime mirror.
        /// </summary>
        public static ReflectionInteropConvention SettingsRuntimeInterop { get; } = new()
        {
            ObjectGetMethodName = "GetRitsuLibSettingValue",
            ObjectSetMethodName = "SetRitsuLibSettingValue",
        };
    }
}
