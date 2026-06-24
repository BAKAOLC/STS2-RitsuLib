using HarmonyLib;

namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Factory methods for common <see cref="ModPatchTarget" /> declarations.
    ///     常见 <see cref="ModPatchTarget" /> 声明的工厂方法。
    /// </summary>
    public static class PatchTarget
    {
        /// <summary>
        ///     Targets a method by name.
        ///     按名称定位方法。
        /// </summary>
        public static ModPatchTarget Method<TTarget>(string methodName)
        {
            return Method(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     Targets a method by name.
        ///     按名称定位方法。
        /// </summary>
        public static ModPatchTarget Method(Type targetType, string methodName)
        {
            return new(targetType, methodName);
        }

        /// <summary>
        ///     Targets a method using an exact parameter signature.
        ///     使用精确参数签名定位方法。
        /// </summary>
        public static ModPatchTarget Method<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return Method(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     Targets a method using an exact parameter signature.
        ///     使用精确参数签名定位方法。
        /// </summary>
        public static ModPatchTarget Method(Type targetType, string methodName, params Type[] parameterTypes)
        {
            return new(targetType, methodName, parameterTypes);
        }

        /// <summary>
        ///     Targets an optional method by name. Missing targets are ignored by the patcher.
        ///     按名称定位可选方法。目标缺失时由 patcher 忽略。
        /// </summary>
        public static ModPatchTarget OptionalMethod<TTarget>(string methodName)
        {
            return OptionalMethod(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     Targets an optional method by name. Missing targets are ignored by the patcher.
        ///     按名称定位可选方法。目标缺失时由 patcher 忽略。
        /// </summary>
        public static ModPatchTarget OptionalMethod(Type targetType, string methodName)
        {
            return new(targetType, methodName, true);
        }

        /// <summary>
        ///     Targets an optional method using an exact parameter signature. Missing targets are ignored by the patcher.
        ///     使用精确参数签名定位可选方法。目标缺失时由 patcher 忽略。
        /// </summary>
        public static ModPatchTarget OptionalMethod<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return OptionalMethod(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     Targets an optional method using an exact parameter signature. Missing targets are ignored by the patcher.
        ///     使用精确参数签名定位可选方法。目标缺失时由 patcher 忽略。
        /// </summary>
        public static ModPatchTarget OptionalMethod(Type targetType, string methodName, params Type[] parameterTypes)
        {
            return new(targetType, methodName, parameterTypes, true);
        }

        /// <summary>
        ///     Targets a property getter.
        ///     定位属性 getter。
        /// </summary>
        public static ModPatchTarget Getter<TTarget>(string propertyName)
        {
            return Getter(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     Targets a property getter.
        ///     定位属性 getter。
        /// </summary>
        public static ModPatchTarget Getter(Type targetType, string propertyName)
        {
            return new(targetType, propertyName, MethodType.Getter);
        }

        /// <summary>
        ///     Targets an optional property getter. Missing targets are ignored by the patcher.
        ///     定位可选属性 getter。目标缺失时由 patcher 忽略。
        /// </summary>
        public static ModPatchTarget OptionalGetter<TTarget>(string propertyName)
        {
            return OptionalGetter(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     Targets an optional property getter. Missing targets are ignored by the patcher.
        ///     定位可选属性 getter。目标缺失时由 patcher 忽略。
        /// </summary>
        public static ModPatchTarget OptionalGetter(Type targetType, string propertyName)
        {
            return new(targetType, propertyName, null, true, MethodType.Getter);
        }

        /// <summary>
        ///     Targets a property setter.
        ///     定位属性 setter。
        /// </summary>
        public static ModPatchTarget Setter<TTarget>(string propertyName)
        {
            return Setter(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     Targets a property setter.
        ///     定位属性 setter。
        /// </summary>
        public static ModPatchTarget Setter(Type targetType, string propertyName)
        {
            return new(targetType, propertyName, MethodType.Setter);
        }

        /// <summary>
        ///     Targets an optional property setter. Missing targets are ignored by the patcher.
        ///     定位可选属性 setter。目标缺失时由 patcher 忽略。
        /// </summary>
        public static ModPatchTarget OptionalSetter<TTarget>(string propertyName)
        {
            return OptionalSetter(typeof(TTarget), propertyName);
        }

        /// <summary>
        ///     Targets an optional property setter. Missing targets are ignored by the patcher.
        ///     定位可选属性 setter。目标缺失时由 patcher 忽略。
        /// </summary>
        public static ModPatchTarget OptionalSetter(Type targetType, string propertyName)
        {
            return new(targetType, propertyName, null, true, MethodType.Setter);
        }

        /// <summary>
        ///     Targets a constructor using an exact parameter signature.
        ///     使用精确参数签名定位构造函数。
        /// </summary>
        public static ModPatchTarget Constructor<TTarget>(params Type[] parameterTypes)
        {
            return Constructor(typeof(TTarget), parameterTypes);
        }

        /// <summary>
        ///     Targets a constructor using an exact parameter signature.
        ///     使用精确参数签名定位构造函数。
        /// </summary>
        public static ModPatchTarget Constructor(Type targetType, params Type[] parameterTypes)
        {
            return new(targetType, ".ctor", parameterTypes, false, MethodType.Constructor);
        }

        /// <summary>
        ///     Targets an optional constructor using an exact parameter signature. Missing targets are ignored by the patcher.
        ///     使用精确参数签名定位可选构造函数。目标缺失时由 patcher 忽略。
        /// </summary>
        public static ModPatchTarget OptionalConstructor<TTarget>(params Type[] parameterTypes)
        {
            return OptionalConstructor(typeof(TTarget), parameterTypes);
        }

        /// <summary>
        ///     Targets an optional constructor using an exact parameter signature. Missing targets are ignored by the patcher.
        ///     使用精确参数签名定位可选构造函数。目标缺失时由 patcher 忽略。
        /// </summary>
        public static ModPatchTarget OptionalConstructor(Type targetType, params Type[] parameterTypes)
        {
            return new(targetType, ".ctor", parameterTypes, true, MethodType.Constructor);
        }

        /// <summary>
        ///     Targets an async method's compiler-generated MoveNext method.
        ///     定位 async 方法的编译器生成 MoveNext 方法。
        /// </summary>
        public static ModPatchTarget AsyncMethod<TTarget>(string methodName)
        {
            return AsyncMethod(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     Targets an async method's compiler-generated MoveNext method.
        ///     定位 async 方法的编译器生成 MoveNext 方法。
        /// </summary>
        public static ModPatchTarget AsyncMethod(Type targetType, string methodName)
        {
            return new(targetType, methodName, MethodType.Async);
        }

        /// <summary>
        ///     Targets an async method's compiler-generated MoveNext method using an exact parameter signature.
        ///     使用精确参数签名定位 async 方法的编译器生成 MoveNext 方法。
        /// </summary>
        public static ModPatchTarget AsyncMethod<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return AsyncMethod(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     Targets an async method's compiler-generated MoveNext method using an exact parameter signature.
        ///     使用精确参数签名定位 async 方法的编译器生成 MoveNext 方法。
        /// </summary>
        public static ModPatchTarget AsyncMethod(Type targetType, string methodName, params Type[] parameterTypes)
        {
            return new(targetType, methodName, parameterTypes, MethodType.Async);
        }

        /// <summary>
        ///     Targets an iterator method's compiler-generated MoveNext method.
        ///     定位 iterator 方法的编译器生成 MoveNext 方法。
        /// </summary>
        public static ModPatchTarget EnumeratorMethod<TTarget>(string methodName)
        {
            return EnumeratorMethod(typeof(TTarget), methodName);
        }

        /// <summary>
        ///     Targets an iterator method's compiler-generated MoveNext method.
        ///     定位 iterator 方法的编译器生成 MoveNext 方法。
        /// </summary>
        public static ModPatchTarget EnumeratorMethod(Type targetType, string methodName)
        {
            return new(targetType, methodName, MethodType.Enumerator);
        }

        /// <summary>
        ///     Targets an iterator method's compiler-generated MoveNext method using an exact parameter signature.
        ///     使用精确参数签名定位 iterator 方法的编译器生成 MoveNext 方法。
        /// </summary>
        public static ModPatchTarget EnumeratorMethod<TTarget>(string methodName, params Type[] parameterTypes)
        {
            return EnumeratorMethod(typeof(TTarget), methodName, parameterTypes);
        }

        /// <summary>
        ///     Targets an iterator method's compiler-generated MoveNext method using an exact parameter signature.
        ///     使用精确参数签名定位 iterator 方法的编译器生成 MoveNext 方法。
        /// </summary>
        public static ModPatchTarget EnumeratorMethod(Type targetType, string methodName, params Type[] parameterTypes)
        {
            return new(targetType, methodName, parameterTypes, MethodType.Enumerator);
        }
    }
}
