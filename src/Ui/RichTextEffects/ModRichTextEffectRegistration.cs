using Godot;

namespace STS2RitsuLib.Ui.RichTextEffects
{
    /// <summary>
    ///     Immutable snapshot of a registered mod rich-text effect.
    ///     已注册 mod 富文本特效的不可变快照。
    /// </summary>
    public sealed record ModRichTextEffectRegistration(
        string ModId,
        string Bbcode,
        RichTextEffect Effect);
}
