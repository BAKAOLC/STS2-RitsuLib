using Godot;

namespace STS2RitsuLib.Ui.RichTextEffects
{
    /// <summary>
    ///     <para xml:lang="en">Contains the metadata and effect instance of a registered mod rich-text effect.</para>
    ///     <para xml:lang="zh-CN">包含已注册模组富文本特效的元数据与特效实例。</para>
    /// </summary>
    /// <param name="ModId">
    ///     <para xml:lang="en">ID of the mod that owns the registration.</para>
    ///     <para xml:lang="zh-CN">拥有该注册项的模组 ID。</para>
    /// </param>
    /// <param name="Bbcode">
    ///     <para xml:lang="en">Global BBCode tag name handled by the effect.</para>
    ///     <para xml:lang="zh-CN">该特效处理的全局 BBCode 标签名。</para>
    /// </param>
    /// <param name="Effect">
    ///     <para xml:lang="en">Registered effect instance.</para>
    ///     <para xml:lang="zh-CN">已注册的特效实例。</para>
    /// </param>
    public sealed record ModRichTextEffectRegistration(
        string ModId,
        string Bbcode,
        RichTextEffect Effect);
}
