using System.Reflection;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Non-generic helper class to cache reflection information per type.
    ///     Avoids static fields in generic types while maintaining performance.
    ///     用于按类型缓存反射信息的非泛型辅助类。
    ///     用于按类型缓存反射信息的非泛型辅助类。
    ///     在保持性能的同时避免泛型类型中的静态字段。
    ///     在保持性能的同时避免泛型类型中的静态字段。
    /// </summary>
    internal static class PropertyCache
    {
        private static readonly Dictionary<Type, PropertyInfo[]> Cache = [];
        private static readonly Lock Lock = new();

        public static PropertyInfo[] GetProperties(Type type)
        {
            lock (Lock)
            {
                if (Cache.TryGetValue(type, out var cached))
                    return cached;

                var properties = type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p is { CanRead: true, CanWrite: true })
                    .ToArray();

                Cache[type] = properties;
                return properties;
            }
        }
    }
}
