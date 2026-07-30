using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Builds <see cref="ReflectionStaticChannel" /> instances from static method naming conventions.
    ///     </para>
    ///     <para xml:lang="zh-CN">根据静态方法命名约定构建 <see cref="ReflectionStaticChannel" /> 实例。</para>
    /// </summary>
    public static class ReflectionStaticChannelBinder
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Binds the required object accessors and any compatible optional JSON operations described by
        ///         <paramref name="convention" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         绑定 <paramref name="convention" /> 描述的必需对象访问器及所有签名兼容的可选 JSON 操作。
        ///     </para>
        /// </summary>
        /// <param name="providerType">
        ///     <para xml:lang="en">Static-method provider type to inspect.</para>
        ///     <para xml:lang="zh-CN">要检查的静态方法提供方类型。</para>
        /// </param>
        /// <param name="convention">
        ///     <para xml:lang="en">Method names for required object accessors and optional JSON operations.</para>
        ///     <para xml:lang="zh-CN">必需对象访问器和可选 JSON 操作的方法名。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A channel containing the compiled delegates.</para>
        ///     <para xml:lang="zh-CN">包含已编译委托的通道。</para>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     <para xml:lang="en">The required object methods are missing or have incompatible signatures.</para>
        ///     <para xml:lang="zh-CN">必需的对象方法缺失或签名不兼容。</para>
        /// </exception>
        public static ReflectionStaticChannel Bind(Type providerType, ReflectionInteropConvention convention)
        {
            ArgumentNullException.ThrowIfNull(providerType);
            ArgumentNullException.ThrowIfNull(convention);
            ArgumentException.ThrowIfNullOrWhiteSpace(convention.ObjectGetMethodName);
            ArgumentException.ThrowIfNullOrWhiteSpace(convention.ObjectSetMethodName);

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            var objectGetMethodName = convention.ObjectGetMethodName.Trim();
            var objectSetMethodName = convention.ObjectSetMethodName.Trim();
            var getObject = providerType.GetMethod(objectGetMethodName, flags, [typeof(string)]);
            var setObject = providerType.GetMethod(objectSetMethodName, flags,
                [typeof(string), typeof(object)]);

            if (getObject == null || setObject == null)
                throw new InvalidOperationException(
                    $"Provider {providerType.FullName} requires static {objectGetMethodName}(string) and " +
                    $"{objectSetMethodName}(string, object).");
            if (getObject.ReturnType == typeof(void) || getObject.ContainsGenericParameters)
                throw new InvalidOperationException(
                    $"Provider method {providerType.FullName}.{objectGetMethodName}(string) must return a value.");
            if (setObject.ReturnType != typeof(void) || setObject.ContainsGenericParameters)
                throw new InvalidOperationException(
                    $"Provider method {providerType.FullName}.{objectSetMethodName}(string, object) must return void.");

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
                      [typeof(string), typeof(JsonArray)]) ??
                  providerType.GetMethod(convention.JsonPatchApplyMethodName.Trim(), flags,
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
                TryBindJsonPatchGetter(jsonPatchGet),
                TryBindRootJsonSetter(setRootObj),
                TryBindNodeSetter(nodeSet),
                TryBindMergeAt(mergeAt),
                TryBindJsonTextGetter(getJson),
                TryBindJsonTextSetter(setJson),
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
            return (k, n) => objDelegate(
                k,
                n as JsonObject ?? throw new InvalidOperationException(
                    $"{method.DeclaringType?.FullName}.{method.Name} only accepts object merge patches."));
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
                return (k, n) => arrDelegate(
                    k,
                    n as JsonArray ?? throw new InvalidOperationException(
                        $"{method.DeclaringType?.FullName}.{method.Name} requires a JSON Patch array."));
            }

            if (ps[1].ParameterType != typeof(JsonObject))
                return null;

            var objDelegate =
                (Action<string, JsonObject>)Delegate.CreateDelegate(typeof(Action<string, JsonObject>), method);
            return (k, n) => objDelegate(
                k,
                n as JsonObject ?? throw new InvalidOperationException(
                    $"{method.DeclaringType?.FullName}.{method.Name} only accepts a JSON Patch object."));
        }

        private static Func<string, string?>? TryBindJsonTextGetter(MethodInfo? method)
        {
            if (method == null ||
                method.ContainsGenericParameters ||
                method.ReturnType != typeof(string))
                return null;

            var ps = method.GetParameters();
            return ps.Length == 1 && ps[0].ParameterType == typeof(string)
                ? (Func<string, string?>)Delegate.CreateDelegate(typeof(Func<string, string?>), method)
                : null;
        }

        private static Action<string, string>? TryBindJsonTextSetter(MethodInfo? method)
        {
            if (method == null ||
                method.ContainsGenericParameters ||
                method.ReturnType != typeof(void))
                return null;

            var ps = method.GetParameters();
            return ps.Length == 2 &&
                   ps[0].ParameterType == typeof(string) &&
                   ps[1].ParameterType == typeof(string)
                ? (Action<string, string>)Delegate.CreateDelegate(typeof(Action<string, string>), method)
                : null;
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

            return node as JsonObject
                   ?? throw new InvalidOperationException(
                       $"The configured root JSON getter returned {node.GetType().Name}; a JsonObject was required.");
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

    }
}
