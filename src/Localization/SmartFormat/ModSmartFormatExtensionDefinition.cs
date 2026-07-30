namespace STS2RitsuLib.Localization.SmartFormat
{
    /// <summary>
    ///     <para xml:lang="en">Describes a registered SmartFormat extension instance, its implementation type, ordering value, and owning mod.</para>
    ///     <para xml:lang="zh-CN">描述已注册的 SmartFormat 扩展实例、实现类型、排序值及所属模组。</para>
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
