using System.Reflection;
using HarmonyLib;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Runs once per mod-defined CLR type after all mods are loaded (see <see cref="ModTypeDiscoveryHub" />).
    ///     Used for cross-mod interop code generation and similar post-load reflection passes.
    /// </summary>
    public interface IModTypeDiscoveryContributor
    {
        /// <summary>
        ///     Invoked once per concrete mod entry type so contributors can emit patches or rewrite types.
        /// </summary>
        /// <param name="harmony">Harmony instance owned by the discovery pipeline.</param>
        /// <param name="modAssembliesByManifestId">Loaded mod assemblies keyed by manifest id.</param>
        /// <param name="modType">The mod’s attributed entry or discovery root type.</param>
        void Contribute(Harmony harmony, IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId, Type modType);
    }
}
