using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace STS2RitsuLib.Combat.Rewards
{
    internal static class LinkedRewardSetSerialization
    {
        internal static SerializableReward CreateSerializable(LinkedRewardSet linkedRewardSet)
        {
            var children = new List<LinkedRewardChildExtData>(linkedRewardSet.Rewards.Count);
            var serializedCharacterCount = 0;
            foreach (var reward in linkedRewardSet.Rewards)
            {
                var serializedReward = reward.ToSerializable();
                RewardSerializationExt.TryGetExtData(serializedReward, out var extension);
                var rewardJson = JsonSerializer.Serialize(serializedReward, JsonSerializationUtility.Options);
                var extensionJson = extension == null ? null : RewardSerializationExt.ToJson(extension);
                var childCharacterCount = rewardJson.Length + (extensionJson?.Length ?? 0);
                if (childCharacterCount > LinkedRewardSets.MaximumSerializedChildCharacters)
                    throw new InvalidOperationException(
                        $"Serialized linked reward child exceeds " +
                        $"{LinkedRewardSets.MaximumSerializedChildCharacters} characters.");
                serializedCharacterCount += childCharacterCount;
                if (serializedCharacterCount > LinkedRewardSets.MaximumSerializedSetCharacters)
                    throw new InvalidOperationException(
                        $"Serialized linked reward set exceeds " +
                        $"{LinkedRewardSets.MaximumSerializedSetCharacters} characters.");

                children.Add(new()
                {
                    RewardJson = rewardJson,
                    ExtensionJson = extensionJson,
                });
            }

            var result = new SerializableReward { RewardType = RewardType.None };
            RewardSerializationExt.SetExtData(result, new()
            {
                LinkedRewardSet = new()
                {
                    Mode = (int)LinkedRewardSetRuntime.GetMode(linkedRewardSet),
                    Children = children,
                },
            });
            return result;
        }

        internal static bool HasSerializedData(SerializableReward serializedReward)
        {
            if (!RewardSerializationExt.TryGetExtData(serializedReward, out var extension) ||
                extension?.LinkedRewardSet is not { } linkedData)
                return false;

            return TryValidate(linkedData);
        }

        internal static bool TryCreate(
            SerializableReward serializedReward,
            Player player,
            out LinkedRewardSet? linkedRewardSet)
        {
            linkedRewardSet = null;
            if (!RewardSerializationExt.TryGetExtData(serializedReward, out var extension) ||
                extension?.LinkedRewardSet is not { } linkedData)
                return false;
            if (!TryValidate(linkedData))
                throw new InvalidDataException("The saved linked reward set contains invalid or unsupported data.");

            var mode = (LinkedRewardSelectionMode)linkedData.Mode;
            var children = new List<Reward>(linkedData.Children.Count);
            foreach (var childData in linkedData.Children)
            {
                var childSave = JsonSerializer.Deserialize<SerializableReward>(
                                    childData.RewardJson,
                                    JsonSerializationUtility.Options)
                                ?? throw new InvalidDataException(
                                    "A saved linked reward child could not be deserialized.");
                if (childData.ExtensionJson != null)
                {
                    var childExtension = RewardSerializationExt.FromJson(childData.ExtensionJson)
                                         ?? throw new InvalidDataException(
                                             "A saved linked reward child extension is invalid.");
                    RewardSerializationExt.SetExtData(childSave, childExtension);
                }

                children.Add(Reward.FromSerializable(childSave, player));
            }

            linkedRewardSet = LinkedRewardSets.Create(children, player, mode);
            return true;
        }

        private static bool TryValidate(LinkedRewardSetExtData linkedData)
        {
            if (!Enum.IsDefined((LinkedRewardSelectionMode)linkedData.Mode) ||
                linkedData.Children.Count is < 1 or > LinkedRewardSets.MaximumEncodedChildren)
                return false;

            var serializedCharacterCount = 0;
            foreach (var child in linkedData.Children)
            {
                if (string.IsNullOrWhiteSpace(child.RewardJson))
                    return false;

                var childCharacterCount = child.RewardJson.Length + (child.ExtensionJson?.Length ?? 0);
                if (childCharacterCount > LinkedRewardSets.MaximumSerializedChildCharacters)
                    return false;
                serializedCharacterCount += childCharacterCount;
                if (serializedCharacterCount > LinkedRewardSets.MaximumSerializedSetCharacters)
                    return false;

                try
                {
                    var childSave = JsonSerializer.Deserialize<SerializableReward>(
                        child.RewardJson,
                        JsonSerializationUtility.Options);
                    if (childSave == null || childSave.RewardType == RewardType.None)
                        return false;
                    if (child.ExtensionJson != null && RewardSerializationExt.FromJson(child.ExtensionJson) == null)
                        return false;
                }
                catch (JsonException)
                {
                    return false;
                }
                catch (NotSupportedException)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
