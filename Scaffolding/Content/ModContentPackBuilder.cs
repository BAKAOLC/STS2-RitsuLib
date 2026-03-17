using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Unlocks;

namespace STS2RitsuLib.Scaffolding.Content
{
    public readonly record struct ModContentPackContext(
        string ModId,
        ModContentRegistry Content,
        ModKeywordRegistry Keywords,
        ModTimelineRegistry Timeline,
        ModUnlockRegistry Unlocks);

    /// <summary>
    ///     Fluent registration helper that batches common mod-author setup into a single readable flow.
    /// </summary>
    public sealed class ModContentPackBuilder
    {
        private readonly string _modId;
        private readonly List<Action<ModContentPackContext>> _steps = [];

        private ModContentPackBuilder(string modId)
        {
            _modId = modId;
        }

        public static ModContentPackBuilder For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            return new(modId);
        }

        public ModContentPackBuilder Character<TCharacter>() where TCharacter : CharacterModel
        {
            return AddStep(ctx => ctx.Content.RegisterCharacter<TCharacter>());
        }

        public ModContentPackBuilder Act<TAct>() where TAct : ActModel
        {
            return AddStep(ctx => ctx.Content.RegisterAct<TAct>());
        }

        public ModContentPackBuilder Card<TPool, TCard>()
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCard<TPool, TCard>());
        }

        public ModContentPackBuilder Relic<TPool, TRelic>()
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            return AddStep(ctx => ctx.Content.RegisterRelic<TPool, TRelic>());
        }

        public ModContentPackBuilder Potion<TPool, TPotion>()
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            return AddStep(ctx => ctx.Content.RegisterPotion<TPool, TPotion>());
        }

        public ModContentPackBuilder Power<TPower>() where TPower : PowerModel
        {
            return AddStep(ctx => ctx.Content.RegisterPower<TPower>());
        }

        public ModContentPackBuilder Orb<TOrb>() where TOrb : OrbModel
        {
            return AddStep(ctx => ctx.Content.RegisterOrb<TOrb>());
        }

        public ModContentPackBuilder SharedEvent<TEvent>() where TEvent : EventModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedEvent<TEvent>());
        }

        public ModContentPackBuilder ActEvent<TAct, TEvent>()
            where TAct : ActModel
            where TEvent : EventModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEvent<TAct, TEvent>());
        }

        public ModContentPackBuilder SharedAncient<TAncient>() where TAncient : AncientEventModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedAncient<TAncient>());
        }

        public ModContentPackBuilder ActAncient<TAct, TAncient>()
            where TAct : ActModel
            where TAncient : AncientEventModel
        {
            return AddStep(ctx => ctx.Content.RegisterActAncient<TAct, TAncient>());
        }

        public ModContentPackBuilder CardKeyword(string id, string? locKeyPrefix = null, string? iconPath = null)
        {
            return AddStep(ctx => ctx.Keywords.RegisterCardKeyword(id, locKeyPrefix, iconPath));
        }

        public ModContentPackBuilder Keyword(
            string id,
            string titleTable = "card_keywords",
            string? titleKey = null,
            string? descriptionTable = null,
            string? descriptionKey = null,
            string? iconPath = null)
        {
            return AddStep(ctx =>
                ctx.Keywords.Register(id, titleTable, titleKey, descriptionTable, descriptionKey, iconPath));
        }

        public ModContentPackBuilder Epoch<TEpoch>() where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Timeline.RegisterEpoch<TEpoch>());
        }

        public ModContentPackBuilder Story<TStory>() where TStory : StoryModel, new()
        {
            return AddStep(ctx => ctx.Timeline.RegisterStory<TStory>());
        }

        public ModContentPackBuilder RequireEpoch<TModel, TEpoch>()
            where TModel : AbstractModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.RequireEpoch<TModel, TEpoch>());
        }

        public ModContentPackBuilder UnlockEpochAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterRunAs<TCharacter, TEpoch>());
        }

        public ModContentPackBuilder UnlockEpochAfterWinAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterWinAs<TCharacter, TEpoch>());
        }

        public ModContentPackBuilder UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(int ascensionLevel)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(ascensionLevel));
        }

        public ModContentPackBuilder UnlockEpochAfterRunCount<TEpoch>(int requiredRuns, bool requireVictory = false)
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterRunCount<TEpoch>(requiredRuns, requireVictory));
        }

        public ModContentPackBuilder Entry(IContentRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.Content));
        }

        public ModContentPackBuilder Custom(Action<ModContentPackContext> step)
        {
            return AddStep(step);
        }

        public ModContentPackContext BuildContext()
        {
            return new(
                _modId,
                RitsuLibFramework.GetContentRegistry(_modId),
                RitsuLibFramework.GetKeywordRegistry(_modId),
                RitsuLibFramework.GetTimelineRegistry(_modId),
                RitsuLibFramework.GetUnlockRegistry(_modId));
        }

        public ModContentPackContext Apply()
        {
            var context = BuildContext();
            foreach (var step in _steps)
                step(context);

            RitsuLibFramework.Logger.Info($"[ContentPack] Applied {_steps.Count} registration step(s) for '{_modId}'.");
            return context;
        }

        private ModContentPackBuilder AddStep(Action<ModContentPackContext> step)
        {
            ArgumentNullException.ThrowIfNull(step);
            _steps.Add(step);
            return this;
        }
    }
}
