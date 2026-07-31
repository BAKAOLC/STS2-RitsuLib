using HarmonyLib;

namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     <para xml:lang="en">Provides factory methods for common <see cref="ModPatchTarget" /> declarations.</para>
    ///     <para xml:lang="zh-CN">提供用于声明常见 <see cref="ModPatchTarget" /> 的工厂方法。</para>
    /// </summary>
    public static class PatchTarget
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a target for a required method resolved by name.</para>
        ///     <para xml:lang="zh-CN">创建按名称解析的必需方法目标。</para>
        /// </summary>
        public static ModPatchTarget Method<TTarget>(string methodName)
        {
            return Method(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for a required method resolved by name.</para>
        ///     <para xml:lang="zh-CN">创建按名称解析的必需方法目标。</para>
        /// </summary>
        public static ModPatchTarget Method(Type targetType, string methodName)
        {
            return new(targetType, methodName);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for a required method with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的必需方法目标。</para>
        /// </summary>
        public static ModPatchTarget Method<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return Method(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for a required method with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的必需方法目标。</para>
        /// </summary>
        public static ModPatchTarget Method(Type targetType, string methodName, params Type[] parameterTypes)
        {
            return new(targetType, methodName, parameterTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an optional method target resolved by name.</para>
        ///     <para xml:lang="zh-CN">创建按名称解析的可选方法目标。</para>
        /// </summary>
        public static ModPatchTarget OptionalMethod<TTarget>(string methodName)
        {
            return OptionalMethod(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an optional method target resolved by name.</para>
        ///     <para xml:lang="zh-CN">创建按名称解析的可选方法目标。</para>
        /// </summary>
        public static ModPatchTarget OptionalMethod(Type targetType, string methodName)
        {
            return new(targetType, methodName, true);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an optional method target with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的可选方法目标。</para>
        /// </summary>
        public static ModPatchTarget OptionalMethod<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return OptionalMethod(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an optional method target with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的可选方法目标。</para>
        /// </summary>
        public static ModPatchTarget OptionalMethod(Type targetType, string methodName, params Type[] parameterTypes)
        {
            return new(targetType, methodName, parameterTypes, true);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for a required property getter.</para>
        ///     <para xml:lang="zh-CN">创建必需属性 getter 的目标。</para>
        /// </summary>
        public static ModPatchTarget Getter<TTarget>(string propertyName)
        {
            return Getter(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for a required property getter.</para>
        ///     <para xml:lang="zh-CN">创建必需属性 getter 的目标。</para>
        /// </summary>
        public static ModPatchTarget Getter(Type targetType, string propertyName)
        {
            return new(targetType, propertyName, MethodType.Getter);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for an optional property getter.</para>
        ///     <para xml:lang="zh-CN">创建可选属性 getter 的目标。</para>
        /// </summary>
        public static ModPatchTarget OptionalGetter<TTarget>(string propertyName)
        {
            return OptionalGetter(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for an optional property getter.</para>
        ///     <para xml:lang="zh-CN">创建可选属性 getter 的目标。</para>
        /// </summary>
        public static ModPatchTarget OptionalGetter(Type targetType, string propertyName)
        {
            return new(targetType, propertyName, null, true, MethodType.Getter);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for a required property setter.</para>
        ///     <para xml:lang="zh-CN">创建必需属性 setter 的目标。</para>
        /// </summary>
        public static ModPatchTarget Setter<TTarget>(string propertyName)
        {
            return Setter(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for a required property setter.</para>
        ///     <para xml:lang="zh-CN">创建必需属性 setter 的目标。</para>
        /// </summary>
        public static ModPatchTarget Setter(Type targetType, string propertyName)
        {
            return new(targetType, propertyName, MethodType.Setter);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for an optional property setter.</para>
        ///     <para xml:lang="zh-CN">创建可选属性 setter 的目标。</para>
        /// </summary>
        public static ModPatchTarget OptionalSetter<TTarget>(string propertyName)
        {
            return OptionalSetter(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for an optional property setter.</para>
        ///     <para xml:lang="zh-CN">创建可选属性 setter 的目标。</para>
        /// </summary>
        public static ModPatchTarget OptionalSetter(Type targetType, string propertyName)
        {
            return new(targetType, propertyName, null, true, MethodType.Setter);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for a required constructor with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的必需构造函数目标。</para>
        /// </summary>
        public static ModPatchTarget Constructor<TTarget>(params Type[] parameterTypes)
        {
            return Constructor(typeof(TTarget), parameterTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for a required constructor with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的必需构造函数目标。</para>
        /// </summary>
        public static ModPatchTarget Constructor(Type targetType, params Type[] parameterTypes)
        {
            return new(targetType, ".ctor", parameterTypes, false, MethodType.Constructor);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for an optional constructor with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的可选构造函数目标。</para>
        /// </summary>
        public static ModPatchTarget OptionalConstructor<TTarget>(params Type[] parameterTypes)
        {
            return OptionalConstructor(typeof(TTarget), parameterTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for an optional constructor with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的可选构造函数目标。</para>
        /// </summary>
        public static ModPatchTarget OptionalConstructor(Type targetType, params Type[] parameterTypes)
        {
            return new(targetType, ".ctor", parameterTypes, true, MethodType.Constructor);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for the compiler-generated <c>MoveNext</c> method of an async method.</para>
        ///     <para xml:lang="zh-CN">创建异步方法中由编译器生成的 <c>MoveNext</c> 方法目标。</para>
        /// </summary>
        public static ModPatchTarget AsyncMethod<TTarget>(string methodName)
        {
            return AsyncMethod(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for the compiler-generated <c>MoveNext</c> method of an async method.</para>
        ///     <para xml:lang="zh-CN">创建异步方法中由编译器生成的 <c>MoveNext</c> 方法目标。</para>
        /// </summary>
        public static ModPatchTarget AsyncMethod(Type targetType, string methodName)
        {
            return new(targetType, methodName, MethodType.Async);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an async-method target with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的异步方法目标。</para>
        /// </summary>
        public static ModPatchTarget AsyncMethod<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return AsyncMethod(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an async-method target with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的异步方法目标。</para>
        /// </summary>
        public static ModPatchTarget AsyncMethod(Type targetType, string methodName, params Type[] parameterTypes)
        {
            return new(targetType, methodName, parameterTypes, MethodType.Async);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for the compiler-generated <c>MoveNext</c> method of an iterator.</para>
        ///     <para xml:lang="zh-CN">创建迭代器中由编译器生成的 <c>MoveNext</c> 方法目标。</para>
        /// </summary>
        public static ModPatchTarget EnumeratorMethod<TTarget>(string methodName)
        {
            return EnumeratorMethod(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a target for the compiler-generated <c>MoveNext</c> method of an iterator.</para>
        ///     <para xml:lang="zh-CN">创建迭代器中由编译器生成的 <c>MoveNext</c> 方法目标。</para>
        /// </summary>
        public static ModPatchTarget EnumeratorMethod(Type targetType, string methodName)
        {
            return new(targetType, methodName, MethodType.Enumerator);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an iterator-method target with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的迭代器方法目标。</para>
        /// </summary>
        public static ModPatchTarget EnumeratorMethod<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return EnumeratorMethod(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an iterator-method target with an exact parameter signature.</para>
        ///     <para xml:lang="zh-CN">创建按精确参数签名解析的迭代器方法目标。</para>
        /// </summary>
        public static ModPatchTarget EnumeratorMethod(Type targetType, string methodName, params Type[] parameterTypes)
        {
            return new(targetType, methodName, parameterTypes, MethodType.Enumerator);
        }
    }
}
