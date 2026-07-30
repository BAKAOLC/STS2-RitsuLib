using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Relics;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     <para xml:lang="en">Registers an Ancient card candidate that <see cref="DustyTome" /> should prefer for the specified character.</para>
        ///     <para xml:lang="zh-CN">注册供 <see cref="DustyTome" /> 为指定角色优先选择的先古卡牌候选项。</para>
        /// </summary>
        /// <param name="registeringModId">
        ///     <para xml:lang="en">Optional mod ID used for diagnostics.</para>
        ///     <para xml:lang="zh-CN">用于诊断的可选模组 ID。</para>
        /// </param>
        public static void RegisterDustyTomeCard<TCharacter, TAncientCard>(string? registeringModId = null)
            where TCharacter : CharacterModel
            where TAncientCard : CardModel
        {
            RegisterDustyTomeCard(typeof(TCharacter), typeof(TAncientCard), registeringModId);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a <see cref="DustyTome" /> card candidate from CLR types for the character and Ancient card.</para>
        ///     <para xml:lang="zh-CN">使用角色和先古卡牌的 CLR 类型注册 <see cref="DustyTome" /> 卡牌候选项。</para>
        /// </summary>
        public static void RegisterDustyTomeCard(Type characterType, Type ancientCardType,
            string? registeringModId = null)
        {
            DustyTomeCardRegistry.Register(characterType, ancientCardType, registeringModId);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a <see cref="DustyTome" /> card candidate from an explicit character ID and Ancient-card type.</para>
        ///     <para xml:lang="zh-CN">使用显式角色 ID 和先古卡牌类型注册 <see cref="DustyTome" /> 卡牌候选项。</para>
        /// </summary>
        public static void RegisterDustyTomeCard(ModelId characterId, Type ancientCardType,
            string? registeringModId = null)
        {
            DustyTomeCardRegistry.Register(characterId, ancientCardType, registeringModId);
        }
    }
}
