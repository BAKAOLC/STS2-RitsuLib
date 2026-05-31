using System.Reflection;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    internal enum AutoRegistrationPhase
    {
        ContentPrimary = 0,
        ContentSecondary = 1,
        AncientMappings = 2,
        Keywords = 3,
        CardTags = 4,
        CardPiles = 5,
        TopBarButtons = 6,
        NodeAttachments = 7,
        TimelineLayout = 8,
        Timeline = 9,
        Unlocks = 10,
        Localization = 11,
    }

    internal sealed record AutoRegistrationOperation(
        string OwnerModId,
        Assembly SourceAssembly,
        Type SourceType,
        AutoRegistrationPhase Phase,
        int Order,
        string Signature,
        string AttributeName,
        Action Execute,
        IReadOnlyList<string>? Dependencies = null,
        IReadOnlyList<string>? ProvidedKeys = null);
}
