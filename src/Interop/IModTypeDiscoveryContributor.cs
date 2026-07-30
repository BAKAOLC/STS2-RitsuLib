using System.Reflection;
using HarmonyLib;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Contributes work for each mod-defined CLR type after all mods are loaded. Typical uses include
    ///         cross-mod interop generation and other post-load reflection passes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在所有模组加载后，为每个由模组定义的 CLR 类型贡献处理逻辑。典型用途包括跨模组互操作生成
    ///         以及其他加载后反射流程。
    ///     </para>
    /// </summary>
    public interface IModTypeDiscoveryContributor
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Processes one discovered mod type. Contributors may emit Harmony patches or rewrite interop stubs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         处理一个已发现的模组类型。贡献器可以生成 Harmony 补丁或重写互操作存根。
        ///     </para>
        /// </summary>
        /// <param name="harmony">
        ///     <para xml:lang="en">Harmony instance owned by the discovery pipeline.</para>
        ///     <para xml:lang="zh-CN">类型发现管线持有的 Harmony 实例。</para>
        /// </param>
        /// <param name="modAssembliesByManifestId">
        ///     <para xml:lang="en">Loaded mod assemblies keyed by manifest ID.</para>
        ///     <para xml:lang="zh-CN">以清单 ID 为键的已加载模组程序集。</para>
        /// </param>
        /// <param name="modType">
        ///     <para xml:lang="en">Discovered CLR type from a mod assembly.</para>
        ///     <para xml:lang="zh-CN">从模组程序集中发现的 CLR 类型。</para>
        /// </param>
        void Contribute(Harmony harmony, IReadOnlyDictionary<string, Assembly> modAssembliesByManifestId, Type modType);
    }
}
