using System.Text.Json;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a mod's default token contribution and optional callback for newly published
    ///         shell-theme snapshots.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述模组贡献的默认令牌，以及可选的 Shell 主题新快照发布回调。
    ///     </para>
    /// </summary>
    /// <param name="ModId">
    ///     <para xml:lang="en">
    ///         The mod identifier used by <c>scopes.mod:&lt;modId&gt;</c> and extension data.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <c>scopes.mod:&lt;modId&gt;</c> 及扩展数据使用的模组标识符。
    ///     </para>
    /// </param>
    /// <param name="Defaults">
    ///     <para xml:lang="en">
    ///         The optional Design Tokens Format Module object merged before the selected theme's inheritance
    ///         chain.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可选的设计令牌格式模块对象，会在所选主题的继承链之前合并。
    ///     </para>
    /// </param>
    /// <param name="OnApply">
    ///     <para xml:lang="en">The optional callback invoked after a rebuilt snapshot is published.</para>
    ///     <para xml:lang="zh-CN">重建后的快照发布后调用的可选回调。</para>
    /// </param>
    public sealed record RitsuShellThemeModRegistration(
        string ModId,
        JsonElement? Defaults,
        Action<RitsuShellTheme>? OnApply);
}
