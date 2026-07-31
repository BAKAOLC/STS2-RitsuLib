namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a SmartFormat formatter for game localization.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为游戏本地化使用的 SmartFormat 格式化器。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSmartFormatterAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a SmartFormat selector source for game localization.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为游戏本地化使用的 SmartFormat 选择器源。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSmartFormatSourceAttribute : AutoRegistrationAttribute;
}
