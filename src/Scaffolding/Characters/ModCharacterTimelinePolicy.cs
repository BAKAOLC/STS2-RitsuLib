using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace STS2RitsuLib.Scaffolding.Characters
{
    internal static class ModCharacterTimelinePolicy
    {
        private static readonly Lock Gate = new();
        private static readonly HashSet<Type> WarnedUnregisteredTypes = [];

        internal static bool IsOwnedOrUsesTimelinePolicy(CharacterModel character)
        {
            ArgumentNullException.ThrowIfNull(character);

            if (ModContentRegistry.TryGetOwnerModId(character.GetType(), out _))
                return true;

            if (character is not IModCharacterEpochTimelineRequirement)
                return false;

            WarnUnregisteredInterfaceUse(character);
            return true;
        }

        internal static bool TryGetRequiresEpochAndTimeline(CharacterModel character, out bool requires)
        {
            ArgumentNullException.ThrowIfNull(character);

            if (character is IModCharacterEpochTimelineRequirement requirement)
            {
                WarnUnregisteredInterfaceUse(character);
                requires = requirement.RequiresEpochAndTimeline;
                return true;
            }

            requires = true;
            return false;
        }

        internal static bool DoesNotRequireEpochAndTimeline(CharacterModel character)
        {
            return TryGetRequiresEpochAndTimeline(character, out var requires) && !requires;
        }

        private static void WarnUnregisteredInterfaceUse(CharacterModel character)
        {
            var characterType = character.GetType();
            if (ModContentRegistry.TryGetOwnerModId(characterType, out _))
                return;

            lock (Gate)
            {
                if (!WarnedUnregisteredTypes.Add(characterType))
                    return;
            }

            RitsuLibFramework.Logger.Warn(
                $"[Content] Unregistered character uses RitsuLib timeline policy: {character.Id} type={characterType.FullName}");
        }
    }
}
