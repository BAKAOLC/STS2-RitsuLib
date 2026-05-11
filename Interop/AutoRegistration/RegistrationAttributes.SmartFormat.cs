namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Registers the annotated type as a SmartFormat formatter for game localization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSmartFormatterAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a SmartFormat selector source for game localization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSmartFormatSourceAttribute : AutoRegistrationAttribute;
}
