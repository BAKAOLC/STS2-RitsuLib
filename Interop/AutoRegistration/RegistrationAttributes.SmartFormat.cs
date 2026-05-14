namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Registers the annotated type as a SmartFormat formatter for game localization.
    ///     将带注解的类型注册为游戏本地化使用的 SmartFormat formatter。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSmartFormatterAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a SmartFormat selector source for game localization.
    ///     将带注解的类型注册为游戏本地化使用的 SmartFormat 选择器来源。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSmartFormatSourceAttribute : AutoRegistrationAttribute;
}
