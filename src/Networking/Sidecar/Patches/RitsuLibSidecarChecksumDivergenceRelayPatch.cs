using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Triggers a one-shot coordinated sidecar diagnostic dump when the host detects a checksum mismatch.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当主机检测到校验和不匹配时，触发一次 sidecar 协同诊断转储。
    ///     </para>
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
