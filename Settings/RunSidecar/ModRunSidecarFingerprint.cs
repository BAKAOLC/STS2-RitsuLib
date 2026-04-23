using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings.RunSidecar
{
    /// <summary>
    ///     Stable client-local identity for binding mod-owned JSON to exactly one run instance. Never written into
    ///     <see cref="SerializableRun" /> packets — does not participate in vanilla multiplayer state sync.
    /// </summary>
    /// <param name="ProfileId">Save profile slot from the active profile manager.</param>
    /// <param name="RunStartTimeUnix">Run start instant in Unix seconds (from live run reflection or save payload).</param>
    /// <param name="GameModeOrdinal">Serialized ordinal of the run game mode.</param>
    /// <param name="Ascension">Ascension level for the run.</param>
    /// <param name="LocalNetId">Local player net id for this client (0 when unavailable).</param>
    /// <param name="RngStringSeed">RNG string seed identifying the run's random stream.</param>
    public readonly record struct ModRunSidecarFingerprint(
        int ProfileId,
        long RunStartTimeUnix,
        int GameModeOrdinal,
        int Ascension,
        ulong LocalNetId,
        string RngStringSeed)
    {
        /// <summary>
        ///     Builds a fingerprint from the currently active run, or returns false when no run is in progress.
        /// </summary>
        public static bool TryGetLive(out ModRunSidecarFingerprint fingerprint)
        {
            fingerprint = default;
            if (RunManager.Instance?.IsInProgress != true || RunManager.Instance.State == null)
                return false;

            var profileId = ProfileManager.Instance.CurrentProfileId;
            if (profileId < 0)
                return false;

            var rm = RunManager.Instance;
            var state = rm.State!;
            var start = ModRunSidecarRunManagerReflection.TryGetRunStartTimeUnix(rm);
            if (start is not { } startValue)
                return false;

            fingerprint = new(
                profileId,
                startValue,
                (int)state.GameMode,
                state.AscensionLevel,
                rm.NetService?.NetId ?? 0UL,
                state.Rng.StringSeed ?? string.Empty);
            return true;
        }

        /// <summary>
        ///     Builds a fingerprint from a serialized run payload (e.g. immediately after load) plus the local net id.
        /// </summary>
        public static ModRunSidecarFingerprint FromSerializableRun(SerializableRun save, ulong localNetId)
        {
            var profileId = ProfileManager.Instance.CurrentProfileId;
            if (profileId < 0)
                profileId = 0;

            return new(
                profileId,
                save.StartTime,
                (int)save.GameMode,
                save.Ascension,
                localNetId,
                save.SerializableRng.Seed ?? string.Empty);
        }

        /// <summary>
        ///     Returns true when every binding field matches (used to validate on-disk envelopes).
        /// </summary>
        public bool EqualsIgnoringProfile(ModRunSidecarFingerprint other)
        {
            return RunStartTimeUnix == other.RunStartTimeUnix &&
                   GameModeOrdinal == other.GameModeOrdinal &&
                   Ascension == other.Ascension &&
                   LocalNetId == other.LocalNetId &&
                   string.Equals(RngStringSeed, other.RngStringSeed, StringComparison.Ordinal);
        }

        /// <summary>
        ///     Full equality including profile id.
        /// </summary>
        public bool EqualsFully(ModRunSidecarFingerprint other)
        {
            return ProfileId == other.ProfileId && EqualsIgnoringProfile(other);
        }

        internal string ComputeFileStem()
        {
            var canonical =
                $"{ProfileId}|{RunStartTimeUnix}|{GameModeOrdinal}|{Ascension}|{LocalNetId}|{RngStringSeed}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        internal ModRunSidecarFingerprintDto ToDto()
        {
            return new()
            {
                ProfileId = ProfileId,
                RunStartTimeUnix = RunStartTimeUnix,
                GameModeOrdinal = GameModeOrdinal,
                Ascension = Ascension,
                LocalNetId = LocalNetId,
                RngStringSeed = RngStringSeed,
            };
        }

        internal static ModRunSidecarFingerprint FromDto(ModRunSidecarFingerprintDto dto)
        {
            return new(
                dto.ProfileId,
                dto.RunStartTimeUnix,
                dto.GameModeOrdinal,
                dto.Ascension,
                dto.LocalNetId,
                dto.RngStringSeed ?? string.Empty);
        }
    }

    /// <summary>
    ///     JSON DTO for <see cref="ModRunSidecarFingerprint" /> stored inside sidecar files.
    /// </summary>
    public sealed class ModRunSidecarFingerprintDto
    {
        /// <summary>Save profile slot id; must match the live fingerprint when reading a sidecar file.</summary>
        [JsonPropertyName("profile_id")]
        public int ProfileId { get; set; }

        /// <summary>Run start instant in Unix seconds.</summary>
        [JsonPropertyName("run_start_time_unix")]
        public long RunStartTimeUnix { get; set; }

        /// <summary>Ordinal of the run game mode at the time the sidecar was written.</summary>
        [JsonPropertyName("game_mode_ordinal")]
        public int GameModeOrdinal { get; set; }

        /// <summary>Ascension level for the run.</summary>
        [JsonPropertyName("ascension")]
        public int Ascension { get; set; }

        /// <summary>Local player net id for this client when the sidecar was written.</summary>
        [JsonPropertyName("local_net_id")]
        public ulong LocalNetId { get; set; }

        /// <summary>RNG string seed for the run; null in JSON is treated as empty when deserialized.</summary>
        [JsonPropertyName("rng_string_seed")]
        public string? RngStringSeed { get; set; }
    }
}
