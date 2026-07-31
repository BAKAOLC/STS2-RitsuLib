using System.Reflection;
using HarmonyLib;

namespace STS2RitsuLib.Patching
{
    /// <summary>
    ///     <para xml:lang="en">Provides helpers for resolving non-public game members used by Harmony patches.</para>
    ///     <para xml:lang="zh-CN">提供用于解析 Harmony 补丁所需非公开游戏成员的辅助方法。</para>
    /// </summary>
    public static class PrivateAccess
    {
        /// <summary>
        ///     <para xml:lang="en">Resolves a field, including inherited fields.</para>
        ///     <para xml:lang="zh-CN">解析字段，包括继承的字段。</para>
        /// </summary>
        public static FieldInfo Field<TTarget>(string fieldName)
        {
            return Field(typeof(TTarget), fieldName);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a field, including inherited fields.</para>
        ///     <para xml:lang="zh-CN">解析字段，包括继承的字段。</para>
        /// </summary>
        public static FieldInfo Field(Type targetType, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            return AccessTools.Field(targetType, fieldName)
                   ?? throw new MissingFieldException(targetType.FullName, fieldName);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a field declared directly on the target type.</para>
        ///     <para xml:lang="zh-CN">解析直接声明在目标类型上的字段。</para>
        /// </summary>
        public static FieldInfo DeclaredField<TTarget>(string fieldName)
        {
            return DeclaredField(typeof(TTarget), fieldName);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a field declared directly on the target type.</para>
        ///     <para xml:lang="zh-CN">解析直接声明在目标类型上的字段。</para>
        /// </summary>
        public static FieldInfo DeclaredField(Type targetType, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            return AccessTools.DeclaredField(targetType, fieldName)
                   ?? throw new MissingFieldException(targetType.FullName, fieldName);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a by-reference accessor for a field.</para>
        ///     <para xml:lang="zh-CN">为字段创建按引用访问器。</para>
        /// </summary>
        public static AccessTools.FieldRef<TTarget, TField> FieldRef<TTarget, TField>(string fieldName)
        {
            _ = Field<TTarget>(fieldName);
            return AccessTools.FieldRefAccess<TTarget, TField>(fieldName);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a method, including inherited methods.</para>
        ///     <para xml:lang="zh-CN">解析方法，包括继承的方法。</para>
        /// </summary>
        public static MethodInfo Method<TTarget>(string methodName)
        {
            return Method(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a method, including inherited methods.</para>
        ///     <para xml:lang="zh-CN">解析方法，包括继承的方法。</para>
        /// </summary>
        public static MethodInfo Method(Type targetType, string methodName)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            return AccessTools.Method(targetType, methodName)
                   ?? throw new MissingMethodException(targetType.FullName, methodName);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a method by exact parameter signature, including inherited methods.</para>
        ///     <para xml:lang="zh-CN">按精确参数签名解析方法，包括继承的方法。</para>
        /// </summary>
        public static MethodInfo Method<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return Method(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a method by exact parameter signature, including inherited methods.</para>
        ///     <para xml:lang="zh-CN">按精确参数签名解析方法，包括继承的方法。</para>
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
        ///     <para xml:lang="en">Resolves a method declared directly on the target type.</para>
        ///     <para xml:lang="zh-CN">解析直接声明在目标类型上的方法。</para>
        /// </summary>
        public static MethodInfo DeclaredMethod<TTarget>(string methodName)
        {
            return DeclaredMethod(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a method declared directly on the target type.</para>
        ///     <para xml:lang="zh-CN">解析直接声明在目标类型上的方法。</para>
        /// </summary>
        public static MethodInfo DeclaredMethod(Type targetType, string methodName)
        {
            ArgumentNullException.ThrowIfNull(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            return AccessTools.DeclaredMethod(targetType, methodName)
                   ?? throw new MissingMethodException(targetType.FullName, methodName);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a directly declared method by exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">按精确参数签名解析直接声明在目标类型上的方法。</para>
        /// </summary>
        public static MethodInfo DeclaredMethod<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return DeclaredMethod(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a directly declared method by exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">按精确参数签名解析直接声明在目标类型上的方法。</para>
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
        ///     <para xml:lang="en">Creates a delegate for a resolved method.</para>
        ///     <para xml:lang="zh-CN">为已解析的方法创建委托。</para>
        /// </summary>
        public static TDelegate MethodDelegate<TDelegate>(MethodInfo method) where TDelegate : Delegate
        {
            ArgumentNullException.ThrowIfNull(method);
            return AccessTools.MethodDelegate<TDelegate>(method);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a method and creates a delegate for it.</para>
        ///     <para xml:lang="zh-CN">解析方法并为其创建委托。</para>
        /// </summary>
        public static TDelegate MethodDelegate<TTarget, TDelegate>(string methodName) where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(Method<TTarget>(methodName));
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a method and creates a delegate for it.</para>
        ///     <para xml:lang="zh-CN">解析方法并为其创建委托。</para>
        /// </summary>
        public static TDelegate MethodDelegate<TDelegate>(Type targetType, string methodName)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(Method(targetType, methodName));
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a method by exact parameter signature and creates a delegate for it.</para>
        ///     <para xml:lang="zh-CN">按精确参数签名解析方法并为其创建委托。</para>
        /// </summary>
        public static TDelegate MethodDelegate<TTarget, TDelegate>(string methodName, params Type[] parameterTypes)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(Method<TTarget>(methodName, parameterTypes));
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a method by exact parameter signature and creates a delegate for it.</para>
        ///     <para xml:lang="zh-CN">按精确参数签名解析方法并为其创建委托。</para>
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
        ///     <para xml:lang="en">Resolves a directly declared method and creates a delegate for it.</para>
        ///     <para xml:lang="zh-CN">解析直接声明的方法并为其创建委托。</para>
        /// </summary>
        public static TDelegate DeclaredMethodDelegate<TTarget, TDelegate>(string methodName)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(DeclaredMethod<TTarget>(methodName));
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a directly declared method and creates a delegate for it.</para>
        ///     <para xml:lang="zh-CN">解析直接声明的方法并为其创建委托。</para>
        /// </summary>
        public static TDelegate DeclaredMethodDelegate<TDelegate>(Type targetType, string methodName)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(DeclaredMethod(targetType, methodName));
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a directly declared method by exact parameter signature and creates a delegate.</para>
        ///     <para xml:lang="zh-CN">按精确参数签名解析直接声明的方法并创建委托。</para>
        /// </summary>
        public static TDelegate DeclaredMethodDelegate<TTarget, TDelegate>(
            string methodName,
            params Type[] parameterTypes)
            where TDelegate : Delegate
        {
            return MethodDelegate<TDelegate>(DeclaredMethod<TTarget>(methodName, parameterTypes));
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a directly declared method by exact parameter signature and creates a delegate.</para>
        ///     <para xml:lang="zh-CN">按精确参数签名解析直接声明的方法并创建委托。</para>
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
        ///     <para xml:lang="en">Resolves a directly declared property getter and creates a delegate for it.</para>
        ///     <para xml:lang="zh-CN">解析直接声明的属性 getter 并为其创建委托。</para>
        /// </summary>
        public static TDelegate DeclaredGetterDelegate<TTarget, TDelegate>(string propertyName)
            where TDelegate : Delegate
        {
            return DeclaredGetterDelegate<TDelegate>(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a directly declared property getter and creates a delegate for it.</para>
        ///     <para xml:lang="zh-CN">解析直接声明的属性 getter 并为其创建委托。</para>
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
        ///     <para xml:lang="en">Resolves a directly declared property setter and creates a delegate for it.</para>
        ///     <para xml:lang="zh-CN">解析直接声明的属性 setter 并为其创建委托。</para>
        /// </summary>
        public static TDelegate DeclaredSetterDelegate<TTarget, TDelegate>(string propertyName)
            where TDelegate : Delegate
        {
            return DeclaredSetterDelegate<TDelegate>(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a directly declared property setter and creates a delegate for it.</para>
        ///     <para xml:lang="zh-CN">解析直接声明的属性 setter 并为其创建委托。</para>
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
