using System.Reflection;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Patching.Rules;

namespace STS2RitsuLib.Patching.Core
{
    /// <summary>
    ///     <para xml:lang="en">Provides extensions for registering and applying patches through <see cref="ModPatcher" />.</para>
    ///     <para xml:lang="zh-CN">提供通过 <see cref="ModPatcher" /> 注册和应用补丁的扩展方法。</para>
    /// </summary>
    public static class ModPatcherExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">Registers patches generated from <paramref name="rule" />.</para>
        ///     <para xml:lang="zh-CN">注册由 <paramref name="rule" /> 生成的补丁。</para>
        /// </summary>
        public static void RegisterFromRule(this ModPatcher patcher, ModPatchRule rule,
            params ReadOnlySpan<Assembly> assemblies)
        {
            var patches = rule.GeneratePatches(assemblies);
            patcher.RegisterPatches(patches);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers patches provided by <typeparamref name="T" />.</para>
        ///     <para xml:lang="zh-CN">注册由 <typeparamref name="T" /> 提供的补丁。</para>
        /// </summary>
        public static void RegisterPatches<T>(this ModPatcher patcher) where T : IModPatches
        {
            T.AddTo(patcher);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers the patches declared by <typeparamref name="TPatch" />.</para>
        ///     <para xml:lang="zh-CN">注册由 <typeparamref name="TPatch" /> 声明的补丁。</para>
        /// </summary>
        public static void RegisterPatch<TPatch>(this ModPatcher patcher) where TPatch : IPatchMethod
        {
            var patchInfos = IPatchMethod.CreatePatchInfos<TPatch>();
            patcher.RegisterPatches(patchInfos);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies the runtime-discovered patches collected by <paramref name="builder" />.</para>
        ///     <para xml:lang="zh-CN">应用 <paramref name="builder" /> 收集的运行时补丁。</para>
        /// </summary>
        public static bool ApplyDynamic(this ModPatcher patcher, DynamicPatchBuilder builder,
            bool rollbackOnCriticalFailure = false)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return patcher.ApplyDynamicPatches(builder.Patches, rollbackOnCriticalFailure);
        }
    }
}
