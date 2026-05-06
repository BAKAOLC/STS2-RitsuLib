using System.Collections.Concurrent;
using STS2RitsuLib.Content;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Localization
{
    /// <summary>
    ///     Bridges the framework-provided <see cref="I18N" /> helper localization into the game-native
    ///     <c>LocString</c>/<c>LocTable</c> pipeline by registering virtual table ids.
    /// </summary>
    public static class I18NLocTableBridge
    {
        private static readonly ConcurrentDictionary<string, I18N> Tables =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Builds a virtual localization table id using the framework's standard three-segment convention:
        ///     <c>MODID_I18N_STEM</c>.
        /// </summary>
        public static string GetTableId(string modId, string stem = "DEFAULT")
        {
            return ModContentRegistry.GetCompoundId(modId, "I18N", stem);
        }

        /// <summary>
        ///     Registers <paramref name="i18N" /> as the backing translation source for the virtual table id
        ///     <c>MODID_I18N_STEM</c>.
        /// </summary>
        public static bool TryRegister(string modId, I18N i18N, string stem = "DEFAULT", bool replaceExisting = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(stem);
            ArgumentNullException.ThrowIfNull(i18N);

            var tableId = GetTableId(modId, stem);

            if (!replaceExisting)
                return Tables.TryAdd(tableId, i18N);

            Tables[tableId] = i18N;
            return true;
        }

        /// <summary>
        ///     Unregisters the virtual table id <c>MODID_I18N_STEM</c> previously registered via <see cref="TryRegister" />.
        /// </summary>
        public static bool TryUnregister(string modId, string stem = "DEFAULT")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(stem);
            return Tables.TryRemove(GetTableId(modId, stem), out _);
        }

        internal static bool TryGet(string tableId, out I18N i18N)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableId);
            return Tables.TryGetValue(tableId, out i18N!);
        }
    }
}
