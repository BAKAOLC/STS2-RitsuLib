using System.Text.Json;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Mod-supplied default tokens that participate in every snapshot build, plus an optional callback
    ///     invoked after each snapshot is published.
    /// </summary>
    /// <param name="ModId">Mod identifier (used to namespace <c>scopes.mod:&lt;modId&gt;</c> + extensions).</param>
    /// <param name="Defaults">Optional DTFM tree (object) merged before chain documents.</param>
    /// <param name="OnApply">Optional callback fired after every theme rebuild with the new snapshot.</param>
    public sealed record RitsuShellThemeModRegistration(
        string ModId,
        JsonElement? Defaults,
        Action<RitsuShellTheme>? OnApply);
}
