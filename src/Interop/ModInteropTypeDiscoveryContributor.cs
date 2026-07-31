using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Interop.Internal;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Built-in contributor that processes stubs marked with <see cref="ModInteropAttribute" /> or
    ///         <see cref="AssemblyInteropAttribute" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         处理带有 <see cref="ModInteropAttribute" /> 或 <see cref="AssemblyInteropAttribute" />
    ///         标记之存根的内置贡献器。
    ///     </para>
    /// </summary>
    public sealed class ModInteropTypeDiscoveryContributor : IModTypeDiscoveryContributor
    {
        /// <inheritdoc />
        public void Contribute(
            Harmony harmony,
            IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId,
            Type modType)
        {
            ModInteropEmitter.TryProcessType(harmony, modAssembliesByManifestId, modType);
        }
    }
}
