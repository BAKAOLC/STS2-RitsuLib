using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Ancients.Options;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an initial-option rule for <typeparamref name="TAncient" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TAncient" /> 注册初始选项规则。</para>
        /// </summary>
        public void RegisterAncientOption<TAncient>(ModAncientOptionRule rule)
            where TAncient : AncientEventModel
        {
            EnsureMutable("register ancient option rule");
            ModAncientOptionRegistry.Register<TAncient>(ModId, rule);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers an initial-option rule for <paramref name="ancientType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">为 <paramref name="ancientType" /> 注册初始选项规则。</para>
        /// </summary>
        public void RegisterAncientOption(Type ancientType, ModAncientOptionRule rule)
        {
            ArgumentNullException.ThrowIfNull(ancientType);
            EnsureMutable($"register ancient option rule for '{ancientType.Name}'");
            ModAncientOptionRegistry.Register(ancientType, ModId, rule);
        }
    }
}
