namespace STS2RitsuLib.Localization.SmartFormat
{
    /// <summary>
    ///     Registered SmartFormat extension instance and its owning mod metadata.
    /// </summary>
    public sealed record ModSmartFormatExtensionDefinition(
        string OwnerModId,
        SmartFormatExtensionKind Kind,
        Type ImplementationType,
        int Order,
        object Instance)
    {
        internal ModSmartFormatExtensionDefinition(
            string ownerModId,
            SmartFormatExtensionKind kind,
            Type implementationType,
            int order,
            object instance,
            long sequence)
            : this(ownerModId, kind, implementationType, order, instance)
        {
            Sequence = sequence;
        }

        internal long Sequence { get; init; }
    }
}
