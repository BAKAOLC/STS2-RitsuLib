namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns model types owned by <paramref name="modId" /> and registered in
        ///         <paramref name="poolType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回由 <paramref name="modId" /> 拥有且已注册到 <paramref name="poolType" /> 的模型类型。
        ///     </para>
        /// </summary>
        public static IReadOnlyList<Type> GetRegisteredModelsInPool(string modId, Type poolType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(poolType);

            lock (SyncRoot)
            {
                return
                [
                    .. RegisteredPoolContent
                        .Where(e => e.PoolType == poolType &&
                                    TryGetOwnerModId(e.ModelType, out var oid) &&
                                    string.Equals(oid, modId, StringComparison.OrdinalIgnoreCase))
                        .Select(static e => e.ModelType)
                        .Distinct(),
                ];
            }
        }
    }
}
