namespace STS2RitsuLib.Settings
{
    internal sealed record ModSettingsClipboardEnvelope(
        string Kind,
        string TypeName,
        string TargetSignature,
        string SchemaSignature,
        ModSettingsClipboardScope Scope,
        string Payload);
}
