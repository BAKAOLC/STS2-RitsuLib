using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace STS2RitsuLib.Utils
{
    internal static class FastMethodInvoker
    {
        private static readonly ConcurrentDictionary<MethodInfo, Func<object?, object?>> Invoke0Cache = new();
        private static readonly ConcurrentDictionary<MethodInfo, Func<object?, object?, object?>> Invoke1Cache = new();

        private static readonly ConcurrentDictionary<MethodInfo, Func<object?, object?, object?, object?>>
            Invoke2Cache = new();

        private static readonly ConcurrentDictionary<MethodInfo, Func<object?, object?, object?, object?, object?>>
            Invoke3Cache = new();

        private static readonly ConcurrentDictionary<MethodInfo, Action<object?, object?>> Invoke1VoidCache = new();
        private static readonly ConcurrentDictionary<MethodInfo, Action<object?>> Invoke0VoidCache = new();

        private static readonly ConcurrentDictionary<MethodInfo, Action<object?, object?, object?, object?>>
            Invoke3VoidCache = new();

        private static readonly ConcurrentDictionary<MethodInfo, Action<object?, object?, object?>> Invoke2VoidCache =
            new();

        private static readonly ConcurrentDictionary<ConstructorInfo, Func<object?>> CtorCache = new();

        public static TResult? Invoke0<TResult>(MethodInfo method, object? instance)
        {
            var result = Invoke0(method, instance);
            return result is null ? default : (TResult)result;
        }

        public static object? Invoke0(MethodInfo method, object? instance)
        {
            ArgumentNullException.ThrowIfNull(method);
            var invoker = Invoke0Cache.GetOrAdd(method, BuildInvoke0);
            return invoker(instance);
        }

        public static TResult? Invoke1<TArg, TResult>(MethodInfo method, object? instance, TArg arg)
        {
            var result = Invoke1(method, instance, arg);
            return result is null ? default : (TResult)result;
        }

        public static object? Invoke1(MethodInfo method, object? instance, object? arg)
        {
            ArgumentNullException.ThrowIfNull(method);
            var invoker = Invoke1Cache.GetOrAdd(method, BuildInvoke1);
            return invoker(instance, arg);
        }

        public static TResult? Invoke2<TArg1, TArg2, TResult>(MethodInfo method, object? instance, TArg1 arg1,
            TArg2 arg2)
        {
            var result = Invoke2(method, instance, arg1, arg2);
            return result is null ? default : (TResult)result;
        }

        public static object? Invoke2(MethodInfo method, object? instance, object? arg1, object? arg2)
        {
            ArgumentNullException.ThrowIfNull(method);
            var invoker = Invoke2Cache.GetOrAdd(method, BuildInvoke2);
            return invoker(instance, arg1, arg2);
        }

        public static object? Invoke3(MethodInfo method, object? instance, object? arg1, object? arg2, object? arg3)
        {
            ArgumentNullException.ThrowIfNull(method);
            var invoker = Invoke3Cache.GetOrAdd(method, BuildInvoke3);
            return invoker(instance, arg1, arg2, arg3);
        }

        public static void Invoke0Void(MethodInfo method, object? instance)
        {
            ArgumentNullException.ThrowIfNull(method);
            var invoker = Invoke0VoidCache.GetOrAdd(method, BuildInvoke0Void);
            invoker(instance);
        }

        public static void Invoke1Void(MethodInfo method, object? instance, object? arg)
        {
            ArgumentNullException.ThrowIfNull(method);
            var invoker = Invoke1VoidCache.GetOrAdd(method, BuildInvoke1Void);
            invoker(instance, arg);
        }

        public static void Invoke2Void(MethodInfo method, object? instance, object? arg1, object? arg2)
        {
            ArgumentNullException.ThrowIfNull(method);
            var invoker = Invoke2VoidCache.GetOrAdd(method, BuildInvoke2Void);
            invoker(instance, arg1, arg2);
        }

        public static void Invoke3Void(MethodInfo method, object? instance, object? arg1, object? arg2, object? arg3)
        {
            ArgumentNullException.ThrowIfNull(method);
            var invoker = Invoke3VoidCache.GetOrAdd(method, BuildInvoke3Void);
            invoker(instance, arg1, arg2, arg3);
        }

        public static object? InvokeStatic0(MethodInfo method)
        {
            return Invoke0(method, null);
        }

        public static object? InvokeStatic1(MethodInfo method, object? arg)
        {
            return Invoke1(method, null, arg);
        }

        public static object? InvokeStatic2(MethodInfo method, object? arg1, object? arg2)
        {
            return Invoke2(method, null, arg1, arg2);
        }

        public static object? InvokeStatic3(MethodInfo method, object? arg1, object? arg2, object? arg3)
        {
            return Invoke3(method, null, arg1, arg2, arg3);
        }

        public static void InvokeStatic0Void(MethodInfo method)
        {
            Invoke0Void(method, null);
        }

        public static void InvokeStatic1Void(MethodInfo method, object? arg)
        {
            Invoke1Void(method, null, arg);
        }

        public static void InvokeStatic2Void(MethodInfo method, object? arg1, object? arg2)
        {
            Invoke2Void(method, null, arg1, arg2);
        }

        public static void InvokeStatic3Void(MethodInfo method, object? arg1, object? arg2, object? arg3)
        {
            Invoke3Void(method, null, arg1, arg2, arg3);
        }

        public static object? CreateWithDefaultCtor(ConstructorInfo ctor)
        {
            ArgumentNullException.ThrowIfNull(ctor);
            var factory = CtorCache.GetOrAdd(ctor, BuildCtor);
            return factory();
        }

        private static Func<object?> BuildCtor(ConstructorInfo ctor)
        {
            var newExpr = Expression.New(ctor);
            var body = Expression.Convert(newExpr, typeof(object));
            return Expression.Lambda<Func<object?>>(body).Compile();
        }

        private static Func<object?, object?> BuildInvoke0(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instance, method.DeclaringType!),
                method);
            Expression body = method.ReturnType == typeof(void)
                ? Expression.Block(call, Expression.Constant(null, typeof(object)))
                : Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<object?, object?>>(body, instance).Compile();
        }

        private static Action<object?> BuildInvoke0Void(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instance, method.DeclaringType!),
                method);
            return Expression.Lambda<Action<object?>>(call, instance).Compile();
        }

        private static Func<object?, object?, object?> BuildInvoke1(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg0 = Expression.Parameter(typeof(object), "arg0");
            var p0 = method.GetParameters()[0].ParameterType;
            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instance, method.DeclaringType!),
                method,
                Expression.Convert(arg0, p0));
            Expression body = method.ReturnType == typeof(void)
                ? Expression.Block(call, Expression.Constant(null, typeof(object)))
                : Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<object?, object?, object?>>(body, instance, arg0).Compile();
        }

        private static Action<object?, object?> BuildInvoke1Void(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg0 = Expression.Parameter(typeof(object), "arg0");
            var p0 = method.GetParameters()[0].ParameterType;
            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instance, method.DeclaringType!),
                method,
                Expression.Convert(arg0, p0));
            return Expression.Lambda<Action<object?, object?>>(call, instance, arg0).Compile();
        }

        private static Func<object?, object?, object?, object?> BuildInvoke2(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg0 = Expression.Parameter(typeof(object), "arg0");
            var arg1 = Expression.Parameter(typeof(object), "arg1");
            var ps = method.GetParameters();
            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instance, method.DeclaringType!),
                method,
                Expression.Convert(arg0, ps[0].ParameterType),
                Expression.Convert(arg1, ps[1].ParameterType));
            Expression body = method.ReturnType == typeof(void)
                ? Expression.Block(call, Expression.Constant(null, typeof(object)))
                : Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<object?, object?, object?, object?>>(body, instance, arg0, arg1).Compile();
        }

        private static Func<object?, object?, object?, object?, object?> BuildInvoke3(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg0 = Expression.Parameter(typeof(object), "arg0");
            var arg1 = Expression.Parameter(typeof(object), "arg1");
            var arg2 = Expression.Parameter(typeof(object), "arg2");
            var ps = method.GetParameters();
            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instance, method.DeclaringType!),
                method,
                Expression.Convert(arg0, ps[0].ParameterType),
                Expression.Convert(arg1, ps[1].ParameterType),
                Expression.Convert(arg2, ps[2].ParameterType));
            Expression body = method.ReturnType == typeof(void)
                ? Expression.Block(call, Expression.Constant(null, typeof(object)))
                : Expression.Convert(call, typeof(object));
            return Expression
                .Lambda<Func<object?, object?, object?, object?, object?>>(body, instance, arg0, arg1, arg2)
                .Compile();
        }

        private static Action<object?, object?, object?> BuildInvoke2Void(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg0 = Expression.Parameter(typeof(object), "arg0");
            var arg1 = Expression.Parameter(typeof(object), "arg1");
            var ps = method.GetParameters();
            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instance, method.DeclaringType!),
                method,
                Expression.Convert(arg0, ps[0].ParameterType),
                Expression.Convert(arg1, ps[1].ParameterType));
            return Expression.Lambda<Action<object?, object?, object?>>(call, instance, arg0, arg1).Compile();
        }

        private static Action<object?, object?, object?, object?> BuildInvoke3Void(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arg0 = Expression.Parameter(typeof(object), "arg0");
            var arg1 = Expression.Parameter(typeof(object), "arg1");
            var arg2 = Expression.Parameter(typeof(object), "arg2");
            var ps = method.GetParameters();
            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instance, method.DeclaringType!),
                method,
                Expression.Convert(arg0, ps[0].ParameterType),
                Expression.Convert(arg1, ps[1].ParameterType),
                Expression.Convert(arg2, ps[2].ParameterType));
            return Expression.Lambda<Action<object?, object?, object?, object?>>(call, instance, arg0, arg1, arg2)
                .Compile();
        }
    }
}
