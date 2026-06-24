using System.Reflection;
using HarmonyLib;

namespace STS2RitsuLib.Patching
{
    /// <summary>
    ///     Small helpers for resolving private game members used by Harmony patches.
    ///     用于 Harmony patch 解析私有游戏成员的小型辅助方法。
    /// </summary>
    public static class PrivateAccess
    {
        /// <summary>
        ///     Resolves a field, including inherited fields.
        ///     解析字段，包括继承字段。
        /// </summary>
        public static FieldInfo Field<TTarget>(string fieldName)
        {
            return Field(typeof(TTarget), fieldName);
        }

        /// <summary>
        ///     Resolves a field, including inherited fields.
        ///     解析字段，包括继承字段。
        /// </summary>
        public static FieldInfo Field(Type targetType, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            return AccessTools.Field(targetType, fieldName)
                   ?? throw new MissingFieldException(targetType.FullName, fieldName);
        }

        /// <summary>
        ///     Resolves a field declared directly on the target type.
        ///     解析直接声明在目标类型上的字段。
        /// </summary>
        public static FieldInfo DeclaredField<TTarget>(string fieldName)
        {
            return DeclaredField(typeof(TTarget), fieldName);
        }

        /// <summary>
        ///     Resolves a field declared directly on the target type.
        ///     解析直接声明在目标类型上的字段。
        /// </summary>
        public static FieldInfo DeclaredField(Type targetType, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            return AccessTools.DeclaredField(targetType, fieldName)
                   ?? throw new MissingFieldException(targetType.FullName, fieldName);
        }

        /// <summary>
        ///     Creates a fast field-ref accessor for a private field.
        ///     为私有字段创建快速 field-ref 访问器。
        /// </summary>
        public static AccessTools.FieldRef<TTarget, TField> FieldRef<TTarget, TField>(string fieldName)
        {
            _ = Field<TTarget>(fieldName);
            return AccessTools.FieldRefAccess<TTarget, TField>(fieldName);
        }

        /// <summary>
        ///     Resolves a method, including inherited methods.
        ///     解析方法，包括继承方法。
        /// </summary>
        public static MethodInfo Method<TTarget>(string methodName)
        {
            return Method(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     Resolves a method, including inherited methods.
        ///     解析方法，包括继承方法。
        /// </summary>
        public static MethodInfo Method(Type targetType, string methodName)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            return AccessTools.Method(targetType, methodName)
                   ?? throw new MissingMethodException(targetType.FullName, methodName);
        }

        /// <summary>
        ///     Resolves a method using an exact parameter signature, including inherited methods.
        ///     使用精确参数签名解析方法，包括继承方法。
        /// </summary>
        public static MethodInfo Method<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return Method(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     Resolves a method using an exact parameter signature, including inherited methods.
        ///     使用精确参数签名解析方法，包括继承方法。
        /// </summary>
        public static MethodInfo Method(Type targetType, string methodName, params Type[] parameterTypes)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
            ArgumentNullException.ThrowIfNull(parameterTypes);

            return AccessTools.Method(targetType, methodName, parameterTypes)
                   ?? throw new MissingMethodException(targetType.FullName,
                       FormatSignature(methodName, parameterTypes));
        }

        /// <summary>
        ///     Resolves a method declared directly on the target type.
        ///     解析直接声明在目标类型上的方法。
        /// </summary>
        public static MethodInfo DeclaredMethod<TTarget>(string methodName)
        {
            return DeclaredMethod(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     Resolves a method declared directly on the target type.
        ///     解析直接声明在目标类型上的方法。
        /// </summary>
        public static MethodInfo DeclaredMethod(Type targetType, string methodName)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            return AccessTools.DeclaredMethod(targetType, methodName)
                   ?? throw new MissingMethodException(targetType.FullName, methodName);
        }

        /// <summary>
        ///     Resolves a method using an exact parameter signature, declared directly on the target type.
        ///     使用精确参数签名解析直接声明在目标类型上的方法。
        /// </summary>
        public static MethodInfo DeclaredMethod<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return DeclaredMethod(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     Resolves a method using an exact parameter signature, declared directly on the target type.
        ///     使用精确参数签名解析直接声明在目标类型上的方法。
        /// </summary>
        public static MethodInfo DeclaredMethod(Type targetType, string methodName, params Type[] parameterTypes)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
            ArgumentNullException.ThrowIfNull(parameterTypes);

            return AccessTools.DeclaredMethod(targetType, methodName, parameterTypes)
                   ?? throw new MissingMethodException(targetType.FullName,
                       FormatSignature(methodName, parameterTypes));
        }

        /// <summary>
        ///     Creates a delegate for a resolved method.
        ///     为已解析方法创建委托。
        /// </summary>
        public static TDelegate MethodDelegate<TDelegate>(MethodInfo method) where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(method);
            return AccessTools.MethodDelegate<TDelegate>(method);
        }

        /// <summary>
        ///     Resolves a method and creates a delegate for it.
        ///     解析方法并为其创建委托。
        /// </summary>
        public static TDelegate MethodDelegate<TTarget, TDelegate>(string methodName) where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(Method<TTarget>(methodName));
        }

        /// <summary>
        ///     Resolves a method and creates a delegate for it.
        ///     解析方法并为其创建委托。
        /// </summary>
        public static TDelegate MethodDelegate<TDelegate>(Type targetType, string methodName)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(Method(targetType, methodName));
        }

        /// <summary>
        ///     Resolves a method using an exact parameter signature and creates a delegate for it.
        ///     使用精确参数签名解析方法并为其创建委托。
        /// </summary>
        public static TDelegate MethodDelegate<TTarget, TDelegate>(string methodName, params Type[] parameterTypes)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(Method<TTarget>(methodName, parameterTypes));
        }

        /// <summary>
        ///     Resolves a method using an exact parameter signature and creates a delegate for it.
        ///     使用精确参数签名解析方法并为其创建委托。
        /// </summary>
        public static TDelegate MethodDelegate<TDelegate>(
            Type targetType,
            string methodName,
            params Type[] parameterTypes)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(Method(targetType, methodName, parameterTypes));
        }

        /// <summary>
        ///     Resolves a directly declared method and creates a delegate for it.
        ///     解析直接声明的方法并为其创建委托。
        /// </summary>
        public static TDelegate DeclaredMethodDelegate<TTarget, TDelegate>(string methodName)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(DeclaredMethod<TTarget>(methodName));
        }

        /// <summary>
        ///     Resolves a directly declared method and creates a delegate for it.
        ///     解析直接声明的方法并为其创建委托。
        /// </summary>
        public static TDelegate DeclaredMethodDelegate<TDelegate>(Type targetType, string methodName)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(DeclaredMethod(targetType, methodName));
        }

        /// <summary>
        ///     Resolves a directly declared method using an exact parameter signature and creates a delegate for it.
        ///     使用精确参数签名解析直接声明的方法并为其创建委托。
        /// </summary>
        public static TDelegate DeclaredMethodDelegate<TTarget, TDelegate>(
            string methodName,
            params Type[] parameterTypes)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(DeclaredMethod<TTarget>(methodName, parameterTypes));
        }

        /// <summary>
        ///     Resolves a directly declared method using an exact parameter signature and creates a delegate for it.
        ///     使用精确参数签名解析直接声明的方法并为其创建委托。
        /// </summary>
        public static TDelegate DeclaredMethodDelegate<TDelegate>(
            Type targetType,
            string methodName,
            params Type[] parameterTypes)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(DeclaredMethod(targetType, methodName, parameterTypes));
        }

        /// <summary>
        ///     Resolves a directly declared property getter and creates a delegate for it.
        ///     解析直接声明的属性 getter 并为其创建委托。
        /// </summary>
        public static TDelegate DeclaredGetterDelegate<TTarget, TDelegate>(string propertyName)
            where TDelegate : Delegate
        {
            return DeclaredGetterDelegate<TDelegate>(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     Resolves a directly declared property getter and creates a delegate for it.
        ///     解析直接声明的属性 getter 并为其创建委托。
        /// </summary>
        public static TDelegate DeclaredGetterDelegate<TDelegate>(Type targetType, string propertyName)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

            var getter = AccessTools.DeclaredPropertyGetter(targetType, propertyName)
                         ?? throw new MissingMethodException(targetType.FullName, $"get_{propertyName}");
            return MethodDelegate<TDelegate>(getter);
        }

        /// <summary>
        ///     Resolves a directly declared property setter and creates a delegate for it.
        ///     解析直接声明的属性 setter 并为其创建委托。
        /// </summary>
        public static TDelegate DeclaredSetterDelegate<TTarget, TDelegate>(string propertyName)
            where TDelegate : Delegate
        {
            return DeclaredSetterDelegate<TDelegate>(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     Resolves a directly declared property setter and creates a delegate for it.
        ///     解析直接声明的属性 setter 并为其创建委托。
        /// </summary>
        public static TDelegate DeclaredSetterDelegate<TDelegate>(Type targetType, string propertyName)
            where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

            var setter = AccessTools.DeclaredPropertySetter(targetType, propertyName)
                         ?? throw new MissingMethodException(targetType.FullName, $"set_{propertyName}");
            return MethodDelegate<TDelegate>(setter);
        }

        private static string FormatSignature(string methodName, IReadOnlyList<Type> parameterTypes)
        {
            var parameters = parameterTypes.Count == 0
                ? "no parameters"
                : string.Join(", ", parameterTypes.Select(static type => type.Name));
            return $"{methodName}({parameters})";
        }
    }
}
