using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Keeps character artwork context on isolated hover-tip card previews without changing card ownership.</para>
    ///     <para xml:lang="zh-CN">为独立的悬浮提示卡牌预览保留角色美术上下文，不改变卡牌归属。</para>
    /// </summary>
    internal sealed class HoverTipCardVisualContextPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<CardModel, CharacterModel> PreviewCharacters = new();

        public static string PatchId => "hover_tip_card_visual_context";
        public static string Description => "Preserve character artwork for ownerless hover-tip card previews";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHoverTipCardContainer), nameof(NHoverTipCardContainer.Add), [typeof(CardHoverTip)])];
        }

        public static void Prefix(NHoverTipCardContainer __instance, ref CardHoverTip cardTip)
        {
            // Preview cards can be ownerless despite the game's non-nullable Owner annotation.
            // ReSharper disable once RedundantAlwaysMatchSubpattern
            if (cardTip.Card is { IsMutable: true, Owner: not null } ||
                __instance.GetParent() is not NHoverTipSet tipSet ||
                ResolveSourceCharacter(tipSet._owner) is not { } character)
                return;

            var preview = (CardModel)cardTip.Card.MutableClone();
            PreviewCharacters.Add(preview, character);
            cardTip = new(preview);
        }

        internal static CharacterModel? ResolveCardCharacter(CardModel card)
        {
            if (card is { IsMutable: true, Owner: { } owner })
                return owner.Character;

            return PreviewCharacters.TryGetValue(card, out var character) ? character : null;
        }

        private static CharacterModel? ResolveSourceCharacter(Node? source)
        {
            for (var node = source; node != null; node = node.GetParent())
            {
                var character = node switch
                {
                    NEventOptionButton button => ResolveModelCharacter(button.Event),
                    NCardHolder holder => ResolveModelCharacter(holder.CardModel),
                    NCard card => ResolveModelCharacter(card.Model),
                    NInspectCardScreen screen => ResolveModelCharacter(screen._card?.Model),
                    NRelicBasicHolder holder => ResolveModelCharacter(holder.Relic?.Model),
                    NRelic relic => ResolveModelCharacter(relic.Model),
                    NPotionHolder holder => ResolveModelCharacter(holder.Potion?.Model),
                    NPotion potion => ResolveModelCharacter(potion.Model),
                    NPower power => ResolveModelCharacter(power.Model),
                    NCreature creature => creature.Entity?.Player?.Character,
                    _ => null,
                };
                if (character != null)
                    return character;
            }

            return null;
        }

        private static CharacterModel? ResolveModelCharacter(AbstractModel? model)
        {
            return model switch
            {
                CardModel card => ResolveCardCharacter(card),
                EventModel { IsMutable: true } eventModel => eventModel.Owner?.Character,
                RelicModel { IsMutable: true } relic => relic.Owner?.Character,
                PotionModel { IsMutable: true } potion => potion.Owner?.Character,
                PowerModel { IsMutable: true } power => power.Owner?.Player?.Character,
                _ => null,
            };
        }
    }
}
