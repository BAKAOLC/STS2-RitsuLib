using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Builds <see cref="ReflectionStaticChannel" /> instances from static method naming conventions.
    ///     根据静态方法命名约定构建 <see cref="ReflectionStaticChannel" /> 实例。
    /// </summary>
    public static class ReflectionStaticChannelBinder
    {
        /// <summary>
        ///     Binds optional JSON tiers and required object resolvers described by <paramref name="convention" />.
        ///     绑定 <paramref name="convention" /> 描述的可选 JSON tier 和必需 object resolver。
        /// </summary>
        /// <param name="providerType">
        ///     Static-method provider type to reflect against.
        ///     要反射的静态方法提供方类型。
        /// </param>
        /// <param name="convention">
        ///     Method names for object resolvers and optional JSON DOM hooks.
        ///     object resolver 和可选 JSON DOM hook 的方法名。
        /// </param>
        /// <returns>
        ///     A channel with compiled delegates.
        ///     带已编译 delegate 的 channel。
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     Required object resolver methods are missing.
        ///     缺少必需的 object resolver 方法。
        /// </exception>
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
                      [typeof(string), typeof(JsonNode)]) ??
                  providerType.GetMethod(convention.MergePatchApplyMethodName.Trim(), flags,
                      [typeof(string), typeof(JsonObject)]);
            var jsonPatchGet = string.IsNullOrWhiteSpace(convention.JsonPatchGetMethodName)
                ? null
                : providerType.GetMethod(convention.JsonPatchGetMethodName.Trim(), flags, [typeof(string)]);
            var jsonPatchApply = string.IsNullOrWhiteSpace(convention.JsonPatchApplyMethodName)
                ? null
                : providerType.GetMethod(convention.JsonPatchApplyMethodName.Trim(), flags,
                      [typeof(string), typeof(JsonNode)]) ??
                  providerType.GetMethod(convention.JsonPatchApplyMethodName.Trim(), flags,
                      [typeof(string), typeof(JsonObject)]) ??
                  providerType.GetMethod(convention.JsonPatchApplyMethodName.Trim(), flags,
                      [typeof(string), typeof(JsonArray)]);
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
                TryBindJsonPatchGetter(jsonPatchGet),
                TryBindRootJsonSetter(setRootObj),
                TryBindNodeSetter(nodeSet),
                TryBindMergeAt(mergeAt),
                getJson == null ? null : CompileStaticStringToNullableStringGetter(getJson),
                setJson == null
                    ? null
                    : (Action<string, string>)Delegate.CreateDelegate(typeof(Action<string, string>), setJson),
                TryBindJsonPatchApply(jsonPatchApply));

            return new(
                providerType,
                CompileStaticStringToObjectGetter(getObject),
                CompileStaticStringObjectSetter(setObject),
                json);
        }

        private static Func<string, JsonNode?>? TryBindMergePatchGetter(MethodInfo? method)
        {
            if (method == null ||
                method.GetParameters().Length != 1 ||
                method.GetParameters()[0].ParameterType != typeof(string))
                return null;

            var rt = method.ReturnType;
            if (rt == typeof(JsonObject) || rt == typeof(JsonNode))
                return (Func<string, JsonNode?>)Delegate.CreateDelegate(typeof(Func<string, JsonNode?>), method);

            return typeof(JsonNode).IsAssignableFrom(rt)
                ? (Func<string, JsonNode?>)(key => method.Invoke(null, [key]) as JsonNode)
                : null;
        }

        private static Func<string, JsonNode?>? TryBindJsonPatchGetter(MethodInfo? method)
        {
            if (method == null ||
                method.GetParameters().Length != 1 ||
                method.GetParameters()[0].ParameterType != typeof(string))
                return null;

            var rt = method.ReturnType;
            if (rt == typeof(JsonNode) || rt == typeof(JsonArray) || rt == typeof(JsonObject))
                return (Func<string, JsonNode?>)Delegate.CreateDelegate(typeof(Func<string, JsonNode?>), method);

            return typeof(JsonNode).IsAssignableFrom(rt)
                ? (Func<string, JsonNode?>)(key => method.Invoke(null, [key]) as JsonNode)
                : null;
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

        private static Action<string, JsonNode?>? TryBindMergePatchApply(MethodInfo? method)
        {
            if (method == null || method.ReturnType != typeof(void))
                return null;

            var ps = method.GetParameters();
            if (ps.Length != 2 || ps[0].ParameterType != typeof(string))
                return null;

            if (ps[1].ParameterType == typeof(JsonNode))
                return (Action<string, JsonNode?>)Delegate.CreateDelegate(typeof(Action<string, JsonNode?>), method);

            if (ps[1].ParameterType != typeof(JsonObject))
                return null;

            var objDelegate =
                (Action<string, JsonObject>)Delegate.CreateDelegate(typeof(Action<string, JsonObject>), method);
            return (k, n) => objDelegate(k, n as JsonObject ?? new JsonObject());
        }

        private static Action<string, JsonNode?>? TryBindJsonPatchApply(MethodInfo? method)
        {
            if (method == null || method.ReturnType != typeof(void))
                return null;

            var ps = method.GetParameters();
            if (ps.Length != 2 || ps[0].ParameterType != typeof(string))
                return null;

            if (ps[1].ParameterType == typeof(JsonNode))
                return (Action<string, JsonNode?>)Delegate.CreateDelegate(typeof(Action<string, JsonNode?>), method);

            if (ps[1].ParameterType == typeof(JsonArray))
            {
                var arrDelegate =
                    (Action<string, JsonArray>)Delegate.CreateDelegate(typeof(Action<string, JsonArray>), method);
                return (k, n) => arrDelegate(k, n as JsonArray ?? []);
            }

            if (ps[1].ParameterType != typeof(JsonObject))
                return null;

            var objDelegate =
                (Action<string, JsonObject>)Delegate.CreateDelegate(typeof(Action<string, JsonObject>), method);
            return (k, n) => objDelegate(k, n as JsonObject ?? new JsonObject());
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
            Expression arg2 = method.GetParameters()[1].ParameterType == typeof(object)
                ? p2
                : Expression.Convert(p2, method.GetParameters()[1].ParameterType);
            var body = Expression.Call(method, p1, arg2);
            return Expression.Lambda<Action<string, object?>>(body, p1, p2).Compile();
        }

        private static Func<string, string?> CompileStaticStringToNullableStringGetter(MethodInfo method)
        {
            var param = Expression.Parameter(typeof(string), "k");
            var call = Expression.Call(method, param);
            Expression body = method.ReturnType == typeof(string)
                ? call
                : Expression.TypeAs(Expression.Convert(call, typeof(object)), typeof(string));
            return Expression.Lambda<Func<string, string?>>(body, param).Compile();
        }
    }
}
