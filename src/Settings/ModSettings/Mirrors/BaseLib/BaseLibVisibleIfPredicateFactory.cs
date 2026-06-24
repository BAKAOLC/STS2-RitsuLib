using System.Globalization;
using System.Reflection;

namespace STS2RitsuLib.Settings
{
    internal static class BaseLibVisibleIfPredicateFactory
    {
        public static Func<bool>? TryCreate(MemberInfo annotatedMember, object instance, Type configType,
            Type modConfigType, Type? visibleIfAttrType)
        {
            if (visibleIfAttrType == null)
                return null;

            var visibleIfAttr = annotatedMember.GetCustomAttribute(visibleIfAttrType, false);
            if (visibleIfAttr == null)
                return null;

            var targetName = visibleIfAttrType.GetProperty("TargetName")?.GetValue(visibleIfAttr) as string;
            if (string.IsNullOrWhiteSpace(targetName))
                return null;

            var args = visibleIfAttrType.GetProperty("Args")?.GetValue(visibleIfAttr) as object[] ?? [];
            var invert = visibleIfAttrType.GetProperty("Invert")?.GetValue(visibleIfAttr) as bool? ?? false;
            var condition = BuildCondition(targetName, args, invert);
            return condition;

            Func<bool>? BuildCondition(string target, object?[] conditionArgs, bool isInverted)
            {
                const BindingFlags bindingFlags =
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
                var targetProperty = configType.GetProperty(target, bindingFlags);
                if (targetProperty != null)
                    return BuildPropertyCondition(targetProperty, conditionArgs, isInverted);

                var targetMethod = configType.GetMethod(target, bindingFlags);
                if (targetMethod is not null && targetMethod.ReturnType == typeof(bool))
                    return BuildMethodCondition(targetMethod, conditionArgs, isInverted);

                return null;
            }

            Func<bool>? BuildPropertyCondition(PropertyInfo property, object?[] conditionArgs, bool isInverted)
            {
                if (conditionArgs.Length == 0 && property.PropertyType != typeof(bool))
                    return null;

                var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                object?[] convertedArgs;
                try
                {
                    convertedArgs = conditionArgs.Select(arg => ConvertArgument(arg, propertyType)).ToArray();
                }
                catch
                {
                    return null;
                }

                var staticInstance = property.GetMethod?.IsStatic == true ? null : instance;
                return () =>
                {
                    try
                    {
                        var currentValue = property.GetValue(staticInstance);
                        var conditionMet = currentValue switch
                        {
                            null => convertedArgs.Any(static a => a == null),
                            _ when convertedArgs.Length == 0 => currentValue is true,
                            _ => convertedArgs.Any(currentValue.Equals),
                        };
                        return isInverted ? !conditionMet : conditionMet;
                    }
                    catch
                    {
                        return true;
                    }
                };
            }

            Func<bool>? BuildMethodCondition(MethodInfo method, object?[] conditionArgs, bool isInverted)
            {
                var argsQueue = new Queue<object?>(conditionArgs);
                object?[] resolvedArgs;
                try
                {
                    resolvedArgs = method.GetParameters()
                        .Select(ResolveMethodParameter)
                        .ToArray();
                }
                catch
                {
                    return null;
                }

                var staticInstance = method.IsStatic ? null : instance;
                return () =>
                {
                    try
                    {
                        var result = method.Invoke(staticInstance, resolvedArgs) as bool? ?? false;
                        return isInverted ? !result : result;
                    }
                    catch
                    {
                        return true;
                    }
                };

                object? ResolveMethodParameter(ParameterInfo parameter)
                {
                    var parameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                    if (modConfigType.IsAssignableFrom(parameterType))
                        return instance;
                    if (parameterType == typeof(MemberInfo))
                        return annotatedMember;
                    if (parameterType == typeof(PropertyInfo))
                        return annotatedMember as PropertyInfo
                               ?? throw new ArgumentException("Visibility method requires PropertyInfo.");
                    if (parameterType == typeof(MethodInfo))
                        return annotatedMember as MethodInfo
                               ?? throw new ArgumentException("Visibility method requires MethodInfo.");
                    return !argsQueue.TryDequeue(out var rawArg)
                        ? throw new ArgumentException("Visibility method missing required argument.")
                        : ConvertArgument(rawArg, parameterType);
                }
            }
        }

        private static object? ConvertArgument(object? rawArg, Type targetType)
        {
            if (rawArg == null)
                return null;

            if (targetType.IsInstanceOfType(rawArg))
                return rawArg;

            if (targetType.IsEnum)
                return rawArg switch
                {
                    string enumName => Enum.Parse(targetType, enumName, true),
                    _ => Enum.ToObject(targetType, rawArg),
                };

            return Convert.ChangeType(rawArg, targetType, CultureInfo.InvariantCulture);
        }
    }
}
