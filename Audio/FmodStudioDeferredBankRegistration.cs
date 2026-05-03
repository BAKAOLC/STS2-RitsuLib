namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Convenience helpers for loading FMOD Studio banks after the game has finished deferred initialization.
    /// </summary>
    public static class FmodStudioDeferredBankRegistration
    {
        /// <summary>
        ///     Schedules loading one FMOD Studio bank and optional GUID path mappings once, after
        ///     <see cref="STS2RitsuLib.DeferredInitializationCompletedEvent" /> (or immediately if that milestone already
        ///     occurred).
        /// </summary>
        /// <param name="bankResourcePath">
        ///     Godot resource path to the <c>.bank</c> file (for example <c>res://Mod/audios/x.bank</c>
        ///     ).
        /// </param>
        /// <param name="studioGuidMappingsResourcePath">
        ///     Optional <c>GUIDs.txt</c>-style resource path; pass <c>null</c> when the bank does not need addon GUID mapping.
        /// </param>
        /// <param name="waitForAllLoadsAfterBanks">
        ///     When true, calls <see cref="FmodStudioServer.TryWaitForAllLoads" /> after all banks in this batch have been
        ///     submitted.
        /// </param>
        /// <returns>
        ///     Subscription token; it is disposed automatically after the deferred load attempt finishes.
        /// </returns>
        public static IDisposable QueueLoadBankAfterDeferredInitialization(
            string bankResourcePath,
            string? studioGuidMappingsResourcePath = null,
            bool waitForAllLoadsAfterBanks = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(bankResourcePath);

            return QueueLoadBanksAfterDeferredInitialization(
                [bankResourcePath],
                studioGuidMappingsResourcePath,
                waitForAllLoadsAfterBanks);
        }

        /// <summary>
        ///     Schedules loading multiple FMOD Studio banks (in order) and optional GUID path mappings once, after
        ///     <see cref="STS2RitsuLib.DeferredInitializationCompletedEvent" /> (or immediately if that milestone already
        ///     occurred).
        /// </summary>
        /// <param name="bankResourcePaths">Non-empty sequence of Godot resource paths to <c>.bank</c> files.</param>
        /// <param name="studioGuidMappingsResourcePath">
        ///     Optional <c>GUIDs.txt</c>-style resource path; pass <c>null</c> when no GUID table should be applied.
        /// </param>
        /// <param name="waitForAllLoadsAfterBanks">
        ///     When true, calls <see cref="FmodStudioServer.TryWaitForAllLoads" /> after all banks in this batch have been
        ///     submitted.
        /// </param>
        /// <returns>
        ///     Subscription token; it is disposed automatically after the deferred load attempt finishes.
        /// </returns>
        public static IDisposable QueueLoadBanksAfterDeferredInitialization(
            IEnumerable<string> bankResourcePaths,
            string? studioGuidMappingsResourcePath = null,
            bool waitForAllLoadsAfterBanks = true)
        {
            ArgumentNullException.ThrowIfNull(bankResourcePaths);

            var banks = bankResourcePaths as string[] ?? bankResourcePaths.ToArray();
            if (banks.Length == 0)
                throw new ArgumentException("At least one bank path is required.", nameof(bankResourcePaths));

            return RitsuLibFramework.SubscribeDeferredInitializationOneShot(() =>
            {
                if (FmodStudioServer.TryGet() is null)
                {
                    RitsuLibFramework.Logger.Warn(
                        "[Audio] Deferred FMOD bank load skipped: FmodServer singleton is missing.");
                    return;
                }

                foreach (var path in banks)
                {
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    if (!FmodStudioServer.TryLoadBank(path))
                        RitsuLibFramework.Logger.Warn($"[Audio] Deferred FMOD bank load failed: {path}");
                }

                if (waitForAllLoadsAfterBanks)
                    FmodStudioServer.TryWaitForAllLoads();

                if (string.IsNullOrWhiteSpace(studioGuidMappingsResourcePath))
                    return;

                if (!FmodStudioServer.TryLoadStudioGuidMappings(studioGuidMappingsResourcePath))
                    RitsuLibFramework.Logger.Warn(
                        $"[Audio] Deferred FMOD guid map failed: {studioGuidMappingsResourcePath}");
            });
        }
    }
}
