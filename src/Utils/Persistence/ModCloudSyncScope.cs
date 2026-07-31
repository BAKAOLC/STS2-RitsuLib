namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     <para xml:lang="en">Selects which registered mod-data scopes participate in a cloud synchronization operation.</para>
    ///     <para xml:lang="zh-CN">选择哪些已注册模组数据作用域参与云同步操作。</para>
    /// </summary>
    internal enum ModCloudSyncScope
    {
        GlobalOnly,
        ProfileOnly,
        All,
    }
}
