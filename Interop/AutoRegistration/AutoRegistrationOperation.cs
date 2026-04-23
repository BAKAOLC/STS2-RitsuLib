using System.Reflection;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    internal enum AutoRegistrationPhase
    {
        ContentPrimary = 0,
        ContentSecondary = 1,
        AncientMappings = 2,
        Keywords = 3,
        CardPiles = 4,
        TopBarButtons = 5,
        TimelineLayout = 6,
        Timeline = 7,
        Unlocks = 8,
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
