namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     Convenience builders for common adaptive music override patterns.
    /// </summary>
    public static class AudioAdaptivePlans
    {
        /// <summary>
        ///     Builds a plan that overrides combat music and optionally room and victory transitions.
        /// </summary>
        public static AudioAdaptiveMusicPlan CombatOverride(
            AudioSource combatSource,
            AudioSource? roomSource = null,
            AudioSource? victorySource = null,
            AudioPlaybackOptions? combatOptions = null,
            AudioPlaybackOptions? roomOptions = null,
            AudioPlaybackOptions? victoryOptions = null)
        {
            return new()
            {
                RoomSource = roomSource,
                CombatSource = combatSource,
                VictorySource = victorySource,
                RoomOptions = roomOptions ?? new AudioPlaybackOptions { Scope = AudioLifecycleScope.Room },
                CombatOptions = combatOptions ?? new AudioPlaybackOptions { Scope = AudioLifecycleScope.Combat },
                VictoryOptions = victoryOptions ?? new AudioPlaybackOptions { Scope = AudioLifecycleScope.Combat },
            };
        }

        /// <summary>
        ///     Builds a plan that supplies room and combat overrides for the full run without restoring vanilla music after
        ///     combat.
        /// </summary>
        public static AudioAdaptiveMusicPlan FullRunOverride(
            AudioSource roomSource,
            AudioSource combatSource,
            AudioSource? victorySource = null)
        {
            return new()
            {
                RoomSource = roomSource,
                CombatSource = combatSource,
                VictorySource = victorySource,
                RestoreVanillaMusicOnCombatEnd = false,
                RoomOptions = new() { Scope = AudioLifecycleScope.Room },
                CombatOptions = new() { Scope = AudioLifecycleScope.Combat },
                VictoryOptions = new() { Scope = AudioLifecycleScope.Combat },
            };
        }
    }
}
