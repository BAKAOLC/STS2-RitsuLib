using Godot;
using HarmonyLib;
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
    ///     <para xml:lang="en">Scopes character artwork to card rendering without replacing hover tips or card models.</para>
    ///     <para xml:lang="zh-CN">将角色美术上下文限定于卡牌绘制，不替换悬浮提示或卡牌模型。</para>
    /// </summary>
    internal sealed class HoverTipCardVisualContextPatch : IPatchMethod
    {
        [ThreadStatic] private static (CardModel? Card, CharacterModel? Character) _current;

        public static string PatchId => "hover_tip_card_visual_context";
        public static string Description => "Preserve character artwork for ownerless hover-tip card previews";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCard), "Reload"),
#if STS2_AT_LEAST_0_108_0
                new(typeof(NCard), "UpdatePortrait"),
#endif
                new(typeof(NCard), "ReloadOverlay"),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(NCard __instance, out (CardModel? Card, CharacterModel? Character)? __state)
        {
            __state = _current;
            _current = default;

            var card = __instance.Model;
            if (card is not { IsMutable: true })
                return;

            if (card.Owner != null)
                return;

            for (var node = __instance.GetParent(); node != null; node = node.GetParent())
            {
                if (node is not NHoverTipSet tipSet)
                    continue;

                _current = (card, ResolveSourceCharacter(tipSet._owner));
                return;
            }
        }

        [HarmonyPriority(Priority.Last)]
        public static void Finalizer((CardModel? Card, CharacterModel? Character)? __state)
        {
            if (__state is { } previous)
                _current = previous;
        }

        internal static CharacterModel? ResolveCardCharacter(CardModel card)
        {
            if (!card.IsMutable)
                return null;

            var owner = card.Owner;
            if (owner != null)
                return owner.Character;

            return ReferenceEquals(_current.Card, card) ? _current.Character : null;
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
            if (model is not { IsMutable: true })
                return null;

            return model switch
            {
                CardModel card => ResolveCardCharacter(card),
                EventModel eventModel => eventModel.Owner?.Character,
                RelicModel relic => relic.Owner?.Character,
                PotionModel potion => potion.Owner?.Character,
                PowerModel power => power.Owner?.Player?.Character,
                _ => null,
            };
        }
    }
}
