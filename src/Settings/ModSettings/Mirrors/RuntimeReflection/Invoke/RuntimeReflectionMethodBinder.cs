using System.Reflection;

namespace STS2RitsuLib.Settings
{
    internal static class RuntimeReflectionMethodBinder
    {
        public static MethodInfo Resolve(MemberInfo member, object? instance, string methodName, string propertyName)
        {
            var owner = member.DeclaringType ??
                        throw ModSettingsMirrorDiagnostics.InvalidConfig("Member has no declaring type.");
            var method = owner.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
                throw ModSettingsMirrorDiagnostics.InvalidConfig(
                    $"'{propertyName}' references missing method '{owner.FullName}.{methodName}'.");
            if (!method.IsStatic && instance == null)
                throw ModSettingsMirrorDiagnostics.InvalidConfig(
                    $"'{propertyName}' references instance method '{owner.FullName}.{methodName}', but provider instance is null.");
            return method;
        }
    }
}
