using System.Text.Json;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Mod-supplied default tokens that participate in every snapshot build, plus an optional callback
    ///     invoked after each snapshot is published.
    ///     mod 提供的默认令牌会参与每次快照构建，另有一个可选回调
    ///     会在每个快照发布后调用。
    /// </summary>
    /// <param name="ModId">
    ///     Mod identifier (used to namespace <c>scopes.mod:&lt;modId&gt;</c> + extensions).
    /// </param>
    /// <param name="Defaults">
    ///     Optional DTFM tree (object) merged before chain documents.
    ///     可选 DTFM 树 (对象) 合并后 在链式文档之前。
    /// </param>
    /// <param name="OnApply">
    ///     Optional callback fired after every theme rebuild with the new snapshot.
    ///     每次主题重建后，带着新快照触发的可选回调。
    /// </param>
    public sealed record RitsuShellThemeModRegistration(
        string ModId,
        JsonElement? Defaults,
        Action<RitsuShellTheme>? OnApply);
}
