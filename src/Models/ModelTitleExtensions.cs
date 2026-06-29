using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     Helpers for resolving display titles from known vanilla model families.
    ///     用于从已知原版模型族解析显示标题的辅助方法。
    /// </summary>
    public static class ModelTitleExtensions
    {
        private static readonly ModelLocStringSource[] TitleSources =
        [
            TitleSource<CardModel>("cards", model => model.Id.Entry + ".title", model => model.TitleLocString),
            TitleSource<PotionModel>("potions", model => model.Id.Entry + ".title", model => model.Title),
            TitleSource<RelicModel>("relics", model => model.Id.Entry + ".title", model => model.Title),
            TitleSource<PowerModel>("powers", model => model.Id.Entry + ".title", model => model.Title),
            TitleSource<OrbModel>("orbs", model => model.Id.Entry + ".title", model => model.Title),
            TitleSource<CharacterModel>("characters", model => model.Id.Entry + ".title", model => model.Title),
            TitleSource<MonsterModel>("monsters", model => model.Id.Entry + ".name", model => model.Title),
            TitleSource<EnchantmentModel>("enchantments", model => model.Id.Entry + ".title", model => model.Title),
            TitleSource<AfflictionModel>("afflictions", model => model.Id.Entry + ".title", model => model.Title),
            TitleSource<ModifierModel>("modifiers", model => model.Id.Entry + ".title", model => model.Title),
            TitleSource<AncientEventModel>("ancients", model => model.Id.Entry + ".title", model => model.Title),
            TitleSource<EventModel>("events", model => model.Id.Entry + ".title", model => model.Title),
            TitleSource<ActModel>("acts", model => model.Id.Entry + ".title", model => model.Title),
            TitleSource<EncounterModel>("encounters", model => model.Id.Entry + ".title", model => model.Title),
        ];

        /// <summary>
        ///     Known title LocString sources, ordered from more-specific model families to broader ones.
        ///     已知标题 LocString 来源，按更具体到更宽泛的模型族排序。
        /// </summary>
        public static IReadOnlyList<ModelLocStringSource> KnownTitleSources => TitleSources;

        /// <summary>
        ///     Resolves a display title for <paramref name="model" />, or returns <paramref name="fallback" /> when the
        ///     model family has no known title surface.
        ///     解析 <paramref name="model" /> 的显示标题；若该模型族没有已知标题 surface，则返回
        ///     <paramref name="fallback" />。
        /// </summary>
        public static LocString ResolveTitleOr(this AbstractModel model, LocString fallback)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(fallback);

            return model.TryResolveTitle(out var title) ? title : fallback;
        }

        /// <summary>
        ///     Attempts to resolve a display title for known vanilla model families.
        ///     尝试为已知原版模型族解析显示标题。
        /// </summary>
        public static bool TryResolveTitle(this AbstractModel model, [NotNullWhen(true)] out LocString? title)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (!model.TryGetTitleLocStringSource(out var source))
            {
                title = null;
                return false;
            }

            title = source.Resolve(model);
            return true;
        }

        /// <summary>
        ///     Attempts to resolve the title LocString source mapping for <paramref name="model" />.
        ///     尝试解析 <paramref name="model" /> 的标题 LocString 来源映射。
        /// </summary>
        public static bool TryGetTitleLocStringSource(
            this AbstractModel model,
            [NotNullWhen(true)] out ModelLocStringSource? source)
        {
            ArgumentNullException.ThrowIfNull(model);

            source = TitleSources.FirstOrDefault(source => source.Matches(model));
            return source != null;
        }

        /// <summary>
        ///     Creates the default title LocString from the mapped table and key, ignoring model property overrides.
        ///     从映射的表和 key 创建默认标题 LocString，不读取模型属性覆写。
        /// </summary>
        public static LocString CreateDefaultTitleLocString(this AbstractModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return model.TryGetTitleLocStringSource(out var source)
                ? source.CreateDefault(model)
                : new(model.Id.Category.ToLowerInvariant(), model.Id.Entry + ".title");
        }

        private static ModelLocStringSource TitleSource<TModel>(
            string table,
            Func<TModel, string> key,
            Func<TModel, LocString> resolve)
            where TModel : AbstractModel
        {
            return new(
                typeof(TModel),
                table,
                model => key((TModel)model),
                model => resolve((TModel)model));
        }
    }
}
