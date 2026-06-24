using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Relics;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Registers an ancient card candidate that <see cref="DustyTome" /> should prefer for the specified character.
        ///     注册一个 <see cref="DustyTome" /> 应为指定角色优先选择的 ancient 卡牌候选。
        /// </summary>
        /// <param name="registeringModId">
        ///     Optional mod id for diagnostics.
        ///     用于诊断消息的可选 Mod id。
        /// </param>
        public static void RegisterDustyTomeCard<TCharacter, TAncientCard>(string? registeringModId = null)
            where TCharacter : CharacterModel
            where TAncientCard : CardModel
        {
            RegisterDustyTomeCard(typeof(TCharacter), typeof(TAncientCard), registeringModId);
        }

        /// <summary>
        ///     Registers a <see cref="DustyTome" /> card candidate using CLR types for both character and ancient card.
        ///     使用角色和 ancient 卡牌的 CLR 类型注册 <see cref="DustyTome" /> 卡牌候选。
        /// </summary>
        public static void RegisterDustyTomeCard(Type characterType, Type ancientCardType,
            string? registeringModId = null)
        {
            DustyTomeCardRegistry.Register(characterType, ancientCardType, registeringModId);
        }

        /// <summary>
        ///     Registers a <see cref="DustyTome" /> card candidate using an explicit character id and ancient card type.
        ///     使用显式角色 id 和 ancient 卡牌类型注册 <see cref="DustyTome" /> 卡牌候选。
        /// </summary>
        public static void RegisterDustyTomeCard(ModelId characterId, Type ancientCardType,
            string? registeringModId = null)
        {
            DustyTomeCardRegistry.Register(characterId, ancientCardType, registeringModId);
        }
    }
}
