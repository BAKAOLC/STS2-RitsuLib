namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Declares how a setting value relates to persistence and run lifecycle (for UI chips and mod author intent).
    /// </summary>
    public enum ModSettingsValueSemantics
    {
        /// <summary>
        ///     Normal global/profile JSON store binding.
        /// </summary>
        Standard,

        /// <summary>
        ///     Value is logically owned by the current run (must be captured into run save data by the mod).
        /// </summary>
        RunSnapshot,

        /// <summary>
        ///     Value applies only to the current combat/session and is not expected in global/profile stores.
        /// </summary>
        SessionCombat,
    }

    /// <summary>
    ///     Optional marker on <see cref="IModSettingsBinding" /> implementations to refine scope chip text.
    /// </summary>
    public interface IModSettingsBindingSemantics
    {
        /// <summary>
        ///     Semantic classification for documentation-style UI chips.
        /// </summary>
        ModSettingsValueSemantics Semantics { get; }
    }
}
