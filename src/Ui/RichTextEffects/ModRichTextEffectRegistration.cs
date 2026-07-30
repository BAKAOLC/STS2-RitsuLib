using Godot;

namespace STS2RitsuLib.Ui.RichTextEffects
{
    /// <summary>
    ///     <para xml:lang="en">Immutable definition of a registered mod rich-text effect.</para>
    ///     <para xml:lang="zh-CN">已注册 mod 富文本特效的不可变定义。</para>
    /// </summary>
    /// <param name="ModId">
    ///     <para xml:lang="en">ID of the mod that owns the registration.</para>
    ///     <para xml:lang="zh-CN">拥有该注册项的 mod ID。</para>
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
