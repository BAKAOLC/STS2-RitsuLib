using System.Collections.Concurrent;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Localization
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers virtual localization-table IDs that expose RitsuLib <see cref="I18N" /> dictionaries
    ///         through the game's <c>LocString</c> and <c>LocTable</c> pipeline.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         注册虚拟本地化表 ID，使 RitsuLib 的 <see cref="I18N" /> 字典可通过游戏的 <c>LocString</c> 和 <c>LocTable</c>
    ///         管线访问。
    ///     </para>
    /// </summary>
    public static class I18NLocTableBridge
    {
        private static readonly ConcurrentDictionary<string, I18NLocTableRegistration> LocTables =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Lock RegistrationLock = new();

        /// <summary>
        ///     <para xml:lang="en">Builds a virtual localization-table ID in the standard <c>MODID_I18N_STEM</c> form.</para>
        ///     <para xml:lang="zh-CN">按标准的 <c>MODID_I18N_STEM</c> 形式构建虚拟本地化表 ID。</para>
        /// </summary>
        public static string GetTableId(string modId, string stem = "DEFAULT")
        {
            return ModContentRegistry.GetCompoundId(modId, "I18N", stem);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="i18N" /> as the translation source for the virtual table ID
        ///         <c>MODID_I18N_STEM</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">将 <paramref name="i18N" /> 注册为虚拟表 ID <c>MODID_I18N_STEM</c> 的翻译来源。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the table was registered or replaced; <see langword="false" /> if
        ///         the ID already existed and <paramref name="replaceExisting" /> was <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         成功注册或替换表时为 <see langword="true" />；ID 已存在且 <paramref name="replaceExisting" /> 为
        ///         <see langword="false" /> 时为 <see langword="false" />。
        ///     </para>
        /// </returns>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         The bridge owns a localization snapshot outside the game's table collection. It refreshes that
        ///         snapshot after <see cref="I18N.Changed" /> and unregisters it when <paramref name="i18N" /> is disposed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         桥接在游戏表集合之外持有本地化快照；<see cref="I18N.Changed" /> 触发后会刷新快照，且
        ///         <paramref name="i18N" /> 释放时会自动注销。
        ///     </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        ///     <para xml:lang="en"><paramref name="i18N" /> has already been disposed.</para>
        ///     <para xml:lang="zh-CN"><paramref name="i18N" /> 已被释放。</para>
        /// </exception>
        public static bool TryRegister(string modId, I18N i18N, string stem = "DEFAULT", bool replaceExisting = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(stem);
            ArgumentNullException.ThrowIfNull(i18N);

            var tableId = GetTableId(modId, stem);
            I18NLocTableRegistration? previous;
            I18NLocTableRegistration registration;

            lock (RegistrationLock)
            {
                if (LocTables.TryGetValue(tableId, out previous) && !replaceExisting)
                    return false;

                registration = new(tableId, i18N);
                LocTables[tableId] = registration;
            }

            previous?.Dispose();

            if (!i18N.IsDisposed)
                return true;

            RemoveIfCurrent(tableId, registration);
            throw new ObjectDisposedException(nameof(i18N));
        }

        /// <summary>
        ///     <para xml:lang="en">Unregisters a virtual table ID previously registered through <see cref="TryRegister" />.</para>
        ///     <para xml:lang="zh-CN">注销先前通过 <see cref="TryRegister" /> 注册的虚拟表 ID。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> if the table was removed; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">成功移除表时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool TryUnregister(string modId, string stem = "DEFAULT")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(stem);

            var tableId = GetTableId(modId, stem);
            I18NLocTableRegistration? registration;

            lock (RegistrationLock)
            {
                if (!LocTables.TryRemove(tableId, out registration))
                    return false;
            }

            registration.Dispose();
            return true;
        }

        internal static bool TryGetLocTable(string tableId, out LocTable locTable)
        {
            lock (RegistrationLock)
            {
                if (!LocTables.TryGetValue(tableId, out var registration))
                {
                    locTable = null!;
                    return false;
                }

                locTable = registration.CurrentTable;
                return true;
            }
        }

        internal static void RemoveIfCurrent(string tableId, I18NLocTableRegistration registration)
        {
            I18NLocTableRegistration? removed = null;

            lock (RegistrationLock)
            {
                if (LocTables.TryGetValue(tableId, out var current) && ReferenceEquals(current, registration))
                    LocTables.TryRemove(tableId, out removed);
            }

            removed?.Dispose();
        }
    }
}
