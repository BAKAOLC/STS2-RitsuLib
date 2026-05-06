using System.Reflection;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    /// <summary>
    ///     Host-side hook: when vanilla compare detects checksum mismatch, trigger a one-shot sidecar coordinated dump.
    /// </summary>
    internal sealed class RitsuLibSidecarChecksumDivergenceRelayPatch : IPatchMethod
    {
        private static readonly FieldInfo? NetChecksumDataChecksumField =
            Type.GetType("MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Checksums.NetChecksumData, sts2")
                ?.GetField("checksum");

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
            if (local == remote)
                return;

            RitsuLibSidecarChecksumDiagnostics.TryTriggerHostCoordinatedDump(remoteId);
        }

        private static bool TryReadChecksum(object source, out uint checksum)
        {
            checksum = 0;
            if (source == null)
                return false;

            var t = source.GetType();
            if (t.Name == "TrackedChecksum" && TrackedChecksumDataField != null)
            {
                var data = TrackedChecksumDataField.GetValue(source);
                if (data == null || NetChecksumDataChecksumField == null)
                    return false;
                var v = NetChecksumDataChecksumField.GetValue(data);
                if (v is not uint u)
                    return false;
                checksum = u;
                return true;
            }

            if (NetChecksumDataChecksumField == null ||
                !NetChecksumDataChecksumField.DeclaringType!.IsInstanceOfType(source))
                return false;
            var raw = NetChecksumDataChecksumField.GetValue(source);
            if (raw is not uint value)
                return false;
            checksum = value;
            return true;
        }
    }
}
