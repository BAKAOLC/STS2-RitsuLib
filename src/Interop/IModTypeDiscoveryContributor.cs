using System.Reflection;
using HarmonyLib;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Runs once per mod-defined CLR type after all mods are loaded (see <see cref="ModTypeDiscoveryHub" />).
    ///     Used for cross-mod interop code generation and similar post-load reflection passes.
    ///     所有 mod 加载后，对每个 mod 定义的 CLR 类型运行一次（参见 <see cref="ModTypeDiscoveryHub" />）。
    ///     用于跨 mod 互操作代码生成以及类似的加载后反射流程。
    /// </summary>
    public interface IModTypeDiscoveryContributor
    {
        /// <summary>
        ///     Invoked once per concrete mod entry type so contributors can emit patches or rewrite types.
        ///     对每个具体 mod 条目类型调用一次，使贡献器可以发出补丁或重写类型。
        /// </summary>
        /// <param name="harmony">
        ///     Harmony instance owned by the discovery pipeline.
        ///     discovery 管线拥有的 Harmony 实例。
        /// </param>
        /// <param name="modAssembliesByManifestId">
        ///     Loaded mod assemblies keyed by manifest id.
        ///     按 manifest id 索引的已加载 mod assembly。
        /// </param>
        /// <param name="modType">
        ///     The mod’s attributed entry or discovery root type.
        ///     mod 的带注解条目或发现根类型。
        /// </param>
        void Contribute(Harmony harmony, IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId, Type modType);
    }
}
