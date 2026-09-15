using Godot;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Controls
{
    /// <summary>
    ///     <para xml:lang="en">Creates reusable themed labels, dividers, and navigation controls on the Godot main thread.</para>
    ///     <para xml:lang="zh-CN">在 Godot 主线程创建可复用的主题标签、分隔线和导航控件。</para>
    /// </summary>
    public static class RitsuControlFactory
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a themed settings-sidebar button for a page, group, or subpage item.</para>
        ///     <para xml:lang="zh-CN">创建用于页面、分组或子页面项的主题设置侧边栏按钮。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The button label.</para>
        ///     <para xml:lang="zh-CN">按钮标签。</para>
        /// </param>
        /// <param name="onPressed">
        ///     <para xml:lang="en">The action invoked when the button is pressed.</para>
        ///     <para xml:lang="zh-CN">按钮按下时调用的操作。</para>
        /// </param>
        /// <param name="kind">
        ///     <para xml:lang="en">The sidebar item kind that controls its visual treatment.</para>
        ///     <para xml:lang="zh-CN">控制视觉表现的侧边栏项类型。</para>
        /// </param>
        /// <param name="prefix">
        ///     <para xml:lang="en">Optional text displayed before the main label.</para>
        ///     <para xml:lang="zh-CN">显示在主标签之前的可选文本。</para>
        /// </param>
        /// <param name="indentLevel">
        ///     <para xml:lang="en">The hierarchy indentation level; negative values are treated as zero.</para>
        ///     <para xml:lang="zh-CN">层级缩进级别；负值按零处理。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured sidebar button.</para>
        ///     <para xml:lang="zh-CN">配置完成的侧边栏按钮。</para>
        /// </returns>
        public static ModSettingsSidebarButton CreateSidebarButton(string text, Action onPressed,
            ModSettingsSidebarItemKind kind = ModSettingsSidebarItemKind.Page,
            string? prefix = null,
            int indentLevel = 0)
        {
            return new(text, onPressed, kind, prefix, indentLevel);
        }


        /// <summary>
        ///     <para xml:lang="en">Creates a noninteractive horizontal divider using the current shell theme.</para>
        ///     <para xml:lang="zh-CN">使用当前界面主题创建不可交互的水平分隔线。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The themed divider control.</para>
        ///     <para xml:lang="zh-CN">采用主题样式的分隔线控件。</para>
        /// </returns>
        public static ColorRect CreateDivider()
        {
            return new()
            {
                CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                    "components.divider.layout.minSize",
                    new(0f, 2f)),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Color = RitsuShellTheme.Current.Color.Divider,
            };
        }


        /// <summary>
        ///     <para xml:lang="en">Creates a themed section title. The returned control belongs to the caller until parented.</para>
        ///     <para xml:lang="zh-CN">创建主题节标题。返回控件在被挂载前归调用方所有。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">Non-null title text.</para>
        ///     <para xml:lang="zh-CN">非 null 的标题文本。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The newly created title label.</para>
        ///     <para xml:lang="zh-CN">新建的标题标签。</para>
        /// </returns>
        public static MegaRichTextLabel CreateSectionTitle(string text)
        {
            var label = CreateHeaderLabel(text, 22, HorizontalAlignment.Left, null,
                RitsuShellTheme.Current.Text.RichTitle);
            label.CustomMinimumSize = RitsuShellThemeLayoutResolver.ResolveMinSize(
                "components.section.layout.title.minSize",
                new(0f, 34f));
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            return label;
        }


        /// <summary>
        ///     <para xml:lang="en">Creates a BBCode-enabled, horizontally bound rich-text label using the settings fonts.</para>
        ///     <para xml:lang="zh-CN">使用设置界面字体创建支持 BBCode、受水平宽度约束的富文本标签。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The initial BBCode text.</para>
        ///     <para xml:lang="zh-CN">初始 BBCode 文本。</para>
        /// </param>
        /// <param name="fontSize">
        ///     <para xml:lang="en">The maximum font size used by normal and emphasized text.</para>
        ///     <para xml:lang="zh-CN">普通与强调文本使用的最大字号。</para>
        /// </param>
        /// <param name="alignment">
        ///     <para xml:lang="en">The horizontal text alignment.</para>
        ///     <para xml:lang="zh-CN">文本的水平对齐方式。</para>
        /// </param>
        /// <param name="scrollViewportHeight">
        ///     <para xml:lang="en">
        ///         An optional finite positive viewport height that enables internal scrolling; other values use
        ///         fit-to-content layout.
        ///     </para>
        ///     <para xml:lang="zh-CN">用于启用内部滚动的可选有限正数视口高度；其他值使用适应内容的布局。</para>
        /// </param>
        /// <param name="textModulate">
        ///     <para xml:lang="en">An optional modulation color; white is used when omitted.</para>
        ///     <para xml:lang="zh-CN">可选的调制颜色；未指定时使用白色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured rich-text label.</para>
        ///     <para xml:lang="zh-CN">配置完成的富文本标签。</para>
        /// </returns>
        public static MegaRichTextLabel CreateHeaderLabel(string text, int fontSize, HorizontalAlignment alignment,
            float? scrollViewportHeight = null, Color? textModulate = null)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentOutOfRangeException.ThrowIfLessThan(fontSize, 1);
            if (!Enum.IsDefined(alignment))
                throw new ArgumentOutOfRangeException(nameof(alignment));
            var boundedScroll = scrollViewportHeight is > 0f && float.IsFinite(scrollViewportHeight.Value);
            var label = new MegaRichTextLabel
            {
                BbcodeEnabled = true,
                AutoSizeEnabled = false,
                FitContent = !boundedScroll,
                ScrollActive = boundedScroll,
                ClipContents = boundedScroll,
                FocusMode = Control.FocusModeEnum.None,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = alignment,
                Theme = ModSettingsUiResources.SettingsLineTheme,
                IsHorizontallyBound = true,
                Modulate = textModulate ?? Colors.White,
            };
            label.AddThemeStyleboxOverride("normal", CreateZeroMarginRichTextStyle());

            if (boundedScroll)
                label.CustomMinimumSize = new(0f, scrollViewportHeight!.Value);

            label.AddThemeFontOverride("normal_font", RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontOverride("bold_font", RitsuShellTheme.Current.Font.BodyBold);
            label.AddThemeFontSizeOverride("normal_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_font_size", fontSize);
            label.AddThemeFontSizeOverride("italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("mono_font_size", fontSize);
            label.MinFontSize = Math.Max(14, fontSize - 3);
            label.MaxFontSize = fontSize;
            label.SetTextAutoSize(text);
            return label;
        }


        private static StyleBoxFlat CreateZeroMarginRichTextStyle()
        {
            return new()
            {
                BgColor = Colors.Transparent,
                DrawCenter = false,
                ContentMarginLeft = 0f,
                ContentMarginTop = 0f,
                ContentMarginRight = 0f,
                ContentMarginBottom = 0f,
            };
        }


        /// <summary>
        ///     <para xml:lang="en">Creates a left-aligned inline description label using the secondary text color.</para>
        ///     <para xml:lang="zh-CN">创建使用次要文本颜色、左对齐的行内说明标签。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The description text.</para>
        ///     <para xml:lang="zh-CN">说明文本。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The configured description label.</para>
        ///     <para xml:lang="zh-CN">配置完成的说明标签。</para>
        /// </returns>
        public static MegaRichTextLabel CreateInlineDescription(string text)
        {
            var label = CreateHeaderLabel(text, 16, HorizontalAlignment.Left, null,
                RitsuShellTheme.Current.Text.RichSecondary);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            return label;
        }
    }
}
