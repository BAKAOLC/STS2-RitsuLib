using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    /// <summary>
    ///     Host-side hook: when vanilla compare detects checksum mismatch, trigger a one-shot sidecar coordinated dump.
    ///     主机侧 hook：当原版比较检测到 checksum 不匹配时，触发一次性 sidecar 协调 dump。
    /// </summary>
    internal sealed class RitsuLibSidecarChecksumDivergenceRelayPatch : IPatchMethod
    {
        private static readonly FieldInfo? NetChecksumDataChecksumField =
            typeof(NetChecksumData).GetField("checksum");

        private static readonly FieldInfo? NetChecksumDataIdField =
            typeof(NetChecksumData).GetField("id");

        private static readonly FieldInfo? TrackedChecksumDataField =
            typeof(ChecksumTracker).GetNestedType("TrackedChecksum", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetField("data");

        public static string PatchId => "ritsulib_sidecar_checksum_divergence_relay";

        public static bool IsCritical => false;

        public static string Description =>
            "Host-side: when vanilla checksum mismatch occurs, trigger sidecar coordinated dump.";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ChecksumTracker), "CompareChecksums")];
        }

        public static void Prefix(object localChecksum, object remoteChecksum, ulong remoteId)
        {
            if (!TryReadChecksum(localChecksum, out var local) || !TryReadChecksum(remoteChecksum, out var remote))
                return;
            if (local.Id != remote.Id || local.Checksum == remote.Checksum)
                return;

            RitsuLibSidecarChecksumDiagnostics.TryTriggerHostCoordinatedDump(remoteId, local.Id);
        }

        private static bool TryReadChecksum(object source, out ChecksumSnapshot checksum)
        {
            checksum = default;
            if (source == null)
                return false;

            var t = source.GetType();
            if (t.Name == "TrackedChecksum" && TrackedChecksumDataField != null)
            {
                var data = TrackedChecksumDataField.GetValue(source);
                return data != null && TryReadNetChecksumData(data, out checksum);
            }

            if (NetChecksumDataChecksumField == null || NetChecksumDataIdField == null ||
                !NetChecksumDataChecksumField.DeclaringType!.IsInstanceOfType(source))
                return false;
            return TryReadNetChecksumData(source, out checksum);
        }

        private static bool TryReadNetChecksumData(object source, out ChecksumSnapshot checksum)
        {
            checksum = default;
            if (NetChecksumDataChecksumField == null || NetChecksumDataIdField == null)
                return false;

            var rawChecksum = NetChecksumDataChecksumField.GetValue(source);
            var rawId = NetChecksumDataIdField.GetValue(source);
            if (rawChecksum is not uint checksumValue || rawId is not uint id)
                return false;
            checksum = new(id, checksumValue);
            return true;
        }

        private readonly record struct ChecksumSnapshot(uint Id, uint Checksum);
    }
}
