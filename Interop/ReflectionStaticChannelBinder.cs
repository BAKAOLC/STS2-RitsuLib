using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Builds <see cref="ReflectionStaticChannel" /> instances from static method naming conventions.
    /// </summary>
    public static class ReflectionStaticChannelBinder
    {
        /// <summary>
        ///     Binds optional JSON tiers and required object resolvers described by <paramref name="convention" />.
        /// </summary>
        /// <param name="providerType">Static-method provider type to reflect against.</param>
        /// <param name="convention">Method names for object resolvers and optional JSON DOM hooks.</param>
        /// <returns>A channel with compiled delegates.</returns>
        /// <exception cref="InvalidOperationException">Required object resolver methods are missing.</exception>
        public static ReflectionStaticChannel Bind(Type providerType, ReflectionInteropConvention convention)
        {
            ArgumentNullException.ThrowIfNull(providerType);
            ArgumentNullException.ThrowIfNull(convention);

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            var getObject = providerType.GetMethod(convention.ObjectGetMethodName, flags, [typeof(string)]);
            var setObject = providerType.GetMethod(convention.ObjectSetMethodName, flags,
                [typeof(string), typeof(object)]);

            if (getObject == null || setObject == null)
                throw new InvalidOperationException(
                    $"Provider {providerType.FullName} requires static {convention.ObjectGetMethodName}(string) and {convention.ObjectSetMethodName}(string, object).");

            var mergePatchGet = string.IsNullOrWhiteSpace(convention.MergePatchGetMethodName)
                ? null
                : providerType.GetMethod(convention.MergePatchGetMethodName.Trim(), flags, [typeof(string)]);
            var mergePatchApply = string.IsNullOrWhiteSpace(convention.MergePatchApplyMethodName)
                ? null
                : providerType.GetMethod(convention.MergePatchApplyMethodName.Trim(), flags,
                    [typeof(string), typeof(JsonObject)]);
            var nodeGet = string.IsNullOrWhiteSpace(convention.NodeGetMethodName)
                ? null
                : providerType.GetMethod(convention.NodeGetMethodName.Trim(), flags,
                    [typeof(string), typeof(string)]);
            var nodeSet = string.IsNullOrWhiteSpace(convention.NodeSetMethodName)
                ? null
                : providerType.GetMethod(convention.NodeSetMethodName.Trim(), flags,
                    [typeof(string), typeof(string), typeof(JsonNode)]);
            var mergeAt = string.IsNullOrWhiteSpace(convention.ObjectMergeAtMethodName)
                ? null
                : providerType.GetMethod(convention.ObjectMergeAtMethodName.Trim(), flags,
                    [typeof(string), typeof(string), typeof(JsonObject)]);
            var getRootObj = string.IsNullOrWhiteSpace(convention.TypedGetJsonObjectMethodName)
                ? null
                : providerType.GetMethod(convention.TypedGetJsonObjectMethodName.Trim(), flags, [typeof(string)]);
            var setRootObj = string.IsNullOrWhiteSpace(convention.TypedSetJsonObjectMethodName)
                ? null
                : providerType.GetMethod(convention.TypedSetJsonObjectMethodName.Trim(), flags,
                    [typeof(string), typeof(JsonObject)]);
            var getJson = string.IsNullOrWhiteSpace(convention.TypedGetJsonMethodName)
                ? null
                : providerType.GetMethod(convention.TypedGetJsonMethodName.Trim(), flags, [typeof(string)]);
            var setJson = string.IsNullOrWhiteSpace(convention.TypedSetJsonMethodName)
                ? null
                : providerType.GetMethod(convention.TypedSetJsonMethodName.Trim(), flags,
                    [typeof(string), typeof(string)]);

            var json = new JsonDomChannelDelegates(
                TryBindMergePatchGetter(mergePatchGet),
                TryBindRootJsonGetter(getRootObj),
                TryBindNodeGetter(nodeGet),
                TryBindMergePatchApply(mergePatchApply),
                TryBindRootJsonSetter(setRootObj),
                TryBindNodeSetter(nodeSet),
                TryBindMergeAt(mergeAt),
                getJson == null ? null : CompileStaticStringToNullableStringGetter(getJson),
                setJson == null
                    ? null
                    : (Action<string, string>)Delegate.CreateDelegate(typeof(Action<string, string>), setJson));

            return new(
                providerType,
                CompileStaticStringToObjectGetter(getObject),
                CompileStaticStringObjectSetter(setObject),
                json);
        }

        private static Func<string, JsonObject?>? TryBindMergePatchGetter(MethodInfo? method)
        {
            if (method == null ||
                method.GetParameters().Length != 1 ||
                method.GetParameters()[0].ParameterType != typeof(string))
                return null;

            var rt = method.ReturnType;
            if (rt == typeof(JsonObject))
                return (Func<string, JsonObject?>)Delegate.CreateDelegate(typeof(Func<string, JsonObject?>), method);

            if (!typeof(JsonNode).IsAssignableFrom(rt))
                return null;

            return key =>
            {
                var n = method.Invoke(null, [key]) as JsonNode;
                return n as JsonObject;
            };
        }

        private static Func<string, JsonObject?>? TryBindRootJsonGetter(MethodInfo? method)
        {
            if (method == null ||
                method.GetParameters().Length != 1 ||
                method.GetParameters()[0].ParameterType != typeof(string))
                return null;

            var rt = method.ReturnType;
            if (rt == typeof(JsonObject))
                return (Func<string, JsonObject?>)Delegate.CreateDelegate(typeof(Func<string, JsonObject?>), method);

            return typeof(JsonNode).IsAssignableFrom(rt) ? CompileJsonNodeRootGetter(method) : null;
        }

        private static Action<string, JsonObject>? TryBindRootJsonSetter(MethodInfo? method)
        {
            if (method == null || method.ReturnType != typeof(void))
                return null;

            var ps = method.GetParameters();
            if (ps.Length != 2 || ps[0].ParameterType != typeof(string) || ps[1].ParameterType != typeof(JsonObject))
                return null;

            return (Action<string, JsonObject>)Delegate.CreateDelegate(typeof(Action<string, JsonObject>), method);
        }

        private static Action<string, JsonObject>? TryBindMergePatchApply(MethodInfo? method)
        {
            if (method == null || method.ReturnType != typeof(void))
                return null;

            var ps = method.GetParameters();
            if (ps.Length != 2 || ps[0].ParameterType != typeof(string) || ps[1].ParameterType != typeof(JsonObject))
                return null;

            return (Action<string, JsonObject>)Delegate.CreateDelegate(typeof(Action<string, JsonObject>), method);
        }

        private static Func<string, string, JsonNode?>? TryBindNodeGetter(MethodInfo? method)
        {
            if (method == null ||
                method.GetParameters().Length != 2 ||
                method.GetParameters()[0].ParameterType != typeof(string) ||
                method.GetParameters()[1].ParameterType != typeof(string))
                return null;

            if (!typeof(JsonNode).IsAssignableFrom(method.ReturnType))
                return null;

            return (Func<string, string, JsonNode?>)Delegate.CreateDelegate(typeof(Func<string, string, JsonNode?>),
                method);
        }

        private static Action<string, string, JsonNode?>? TryBindNodeSetter(MethodInfo? method)
        {
            if (method == null || method.ReturnType != typeof(void))
                return null;

            var ps = method.GetParameters();
            if (ps.Length != 3 ||
                ps[0].ParameterType != typeof(string) ||
                ps[1].ParameterType != typeof(string) ||
                ps[2].ParameterType != typeof(JsonNode))
                return null;

            return (Action<string, string, JsonNode?>)Delegate.CreateDelegate(
                typeof(Action<string, string, JsonNode?>), method);
        }

        private static Action<string, string, JsonObject>? TryBindMergeAt(MethodInfo? method)
        {
            if (method == null || method.ReturnType != typeof(void))
                return null;

            var ps = method.GetParameters();
            if (ps.Length != 3 ||
                ps[0].ParameterType != typeof(string) ||
                ps[1].ParameterType != typeof(string) ||
                ps[2].ParameterType != typeof(JsonObject))
                return null;

            return (Action<string, string, JsonObject>)Delegate.CreateDelegate(
                typeof(Action<string, string, JsonObject>),
                method);
        }

        private static Func<string, JsonObject?> CompileJsonNodeRootGetter(MethodInfo method)
        {
            var param = Expression.Parameter(typeof(string), "k");
            var call = Expression.Call(method, param);
            var coerce = typeof(ReflectionStaticChannelBinder).GetMethod(nameof(CoerceRootJsonNode),
                BindingFlags.NonPublic | BindingFlags.Static)!;
            var converted = Expression.Convert(call, typeof(JsonNode));
            var body = Expression.Call(coerce, converted);
            return Expression.Lambda<Func<string, JsonObject?>>(body, param).Compile();
        }

        private static JsonObject CoerceRootJsonNode(JsonNode? node)
        {
            if (node == null)
                return new();

            return node as JsonObject ?? new JsonObject();
        }

        private static Func<string, object?> CompileStaticStringToObjectGetter(MethodInfo method)
        {
            var param = Expression.Parameter(typeof(string), "k");
            var call = Expression.Call(method, param);
            Expression body = method.ReturnType == typeof(object)
                ? call
                : Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<string, object?>>(body, param).Compile();
        }

        private static Action<string, object?> CompileStaticStringObjectSetter(MethodInfo method)
        {
            var p1 = Expression.Parameter(typeof(string), "k");
            var p2 = Expression.Parameter(typeof(object), "v");
            var arg2 = method.GetParameters()[1].ParameterType == typeof(object)
                ? (Expression)p2
                : Expression.Convert(p2, method.GetParameters()[1].ParameterType);
            var body = Expression.Call(method, p1, arg2);
            return Expression.Lambda<Action<string, object?>>(body, p1, p2).Compile();
        }

        private static Func<string, string?> CompileStaticStringToNullableStringGetter(MethodInfo method)
        {
            var param = Expression.Parameter(typeof(string), "k");
            var call = Expression.Call(method, param);
            var body = method.ReturnType == typeof(string)
                ? (Expression)call
                : Expression.TypeAs(Expression.Convert(call, typeof(object)), typeof(string));
            return Expression.Lambda<Func<string, string?>>(body, param).Compile();
        }
    }
}
