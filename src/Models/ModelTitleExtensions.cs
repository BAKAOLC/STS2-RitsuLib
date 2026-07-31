using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves display titles through registered resolvers and known base-game model families.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过已注册的解析器和已知游戏原版模型族解析显示标题。
    ///     </para>
    /// </summary>
    public static class ModelTitleExtensions
    {
        private static readonly Lock ResolverSync = new();
        private static readonly Dictionary<Type, Func<AbstractModel, LocString?>> RegisteredTitleResolvers = [];

        private static readonly ConcurrentDictionary<TitleResolverCacheKey, TitleResolverLookup> TitleResolverCache =
            new();

        private static int _titleResolverGeneration;

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
        ///     <para xml:lang="en">
        ///         Gets the known title <see cref="LocString" /> sources, ordered from more specific model families
        ///         to broader ones.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已知的标题 <see cref="LocString" /> 来源，并按模型族由具体到宽泛排列。
        ///     </para>
        /// </summary>
        public static IReadOnlyList<ModelLocStringSource> KnownTitleSources { get; } = Array.AsReadOnly(TitleSources);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers or replaces the title resolver for <typeparamref name="TModel" /> and its derived types.
        ///         More specific registered model types take precedence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TModel" /> 及其派生类型注册或替换标题解析器。
        ///         更具体的已注册模型类型优先。
        ///     </para>
        /// </summary>
        public static void RegisterTitleResolver<TModel>(Func<TModel, LocString?> resolver)
            where TModel : AbstractModel
        {
            ArgumentNullException.ThrowIfNull(resolver);

            RegisterTitleResolver(typeof(TModel), model => resolver((TModel)model));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers or replaces the title resolver for <paramref name="modelType" /> and its derived types.
        ///         More specific registered model types take precedence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="modelType" /> 及其派生类型注册或替换标题解析器。
        ///         更具体的已注册模型类型优先。
        ///     </para>
        /// </summary>
        public static void RegisterTitleResolver(
            Type modelType,
            Func<AbstractModel, LocString?> resolver)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentNullException.ThrowIfNull(resolver);

            if (!modelType.IsAssignableTo(typeof(AbstractModel)))
                throw new ArgumentException(
                    $"{modelType.FullName} must derive from {typeof(AbstractModel).FullName}.",
                    nameof(modelType));

            lock (ResolverSync)
            {
                RegisteredTitleResolvers[modelType] = resolver;
                Interlocked.Increment(ref _titleResolverGeneration);
                TitleResolverCache.Clear();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Unregisters the title resolver for exactly <typeparamref name="TModel" />.</para>
        ///     <para xml:lang="zh-CN">注销仅对应 <typeparamref name="TModel" /> 的标题解析器。</para>
        /// </summary>
        public static bool UnregisterTitleResolver<TModel>()
            where TModel : AbstractModel
        {
            return UnregisterTitleResolver(typeof(TModel));
        }

        /// <summary>
        ///     <para xml:lang="en">Unregisters the title resolver for exactly <paramref name="modelType" />.</para>
        ///     <para xml:lang="zh-CN">注销仅对应 <paramref name="modelType" /> 的标题解析器。</para>
        /// </summary>
        public static bool UnregisterTitleResolver(Type modelType)
        {
            ArgumentNullException.ThrowIfNull(modelType);

            lock (ResolverSync)
            {
                var removed = RegisteredTitleResolvers.Remove(modelType);
                if (!removed) return removed;
                Interlocked.Increment(ref _titleResolverGeneration);
                TitleResolverCache.Clear();

                return removed;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves a display title for <paramref name="model" />, or returns
        ///         <paramref name="fallback" /> when the model has no registered resolver and its family has no
        ///         known title source.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析 <paramref name="model" /> 的显示标题。若该模型没有已注册解析器，且所属模型族没有
        ///         已知标题来源，则返回 <paramref name="fallback" />。
        ///     </para>
        /// </summary>
        public static LocString ResolveTitleOr(this AbstractModel model, LocString fallback)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(fallback);

            return model.TryResolveTitle(out var title) ? title : fallback;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to resolve a display title from a registered resolver, then from a known base-game
        ///         model family.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试从已注册解析器获取显示标题；若未取得标题，再尝试已知的游戏原版模型族。
        ///     </para>
        /// </summary>
        public static bool TryResolveTitle(this AbstractModel model, [NotNullWhen(true)] out LocString? title)
        {
            ArgumentNullException.ThrowIfNull(model);

            var registeredResolver = GetRegisteredTitleResolver(model.GetType());
            if (registeredResolver?.Invoke(model) is { } registeredTitle)
            {
                title = registeredTitle;
                return true;
            }

            if (model.TryGetTitleLocStringSource(out var source))
            {
                title = source.Resolve(model);
                return true;
            }

            title = null;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to resolve the title <see cref="LocString" /> source mapping for
        ///         <paramref name="model" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试解析 <paramref name="model" /> 的标题 <see cref="LocString" /> 来源映射。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Creates the default title <see cref="LocString" /> from the mapped table and key without reading
        ///         model property overrides.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从映射的表与键创建默认标题 <see cref="LocString" />，不读取模型属性重写。
        ///     </para>
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

        private static Func<AbstractModel, LocString?>? GetRegisteredTitleResolver(Type modelType)
        {
            while (true)
            {
                var generation = Volatile.Read(ref _titleResolverGeneration);
                var lookup = TitleResolverCache.GetOrAdd(
                    new(modelType, generation),
                    static key => FindRegisteredTitleResolver(key.ModelType));
                if (generation == Volatile.Read(ref _titleResolverGeneration))
                    return lookup.Resolver;
            }
        }

        private static TitleResolverLookup FindRegisteredTitleResolver(Type modelType)
        {
            lock (ResolverSync)
            {
                for (var current = modelType;
                     current != null && typeof(AbstractModel).IsAssignableFrom(current);
                     current = current.BaseType)
                    if (RegisteredTitleResolvers.TryGetValue(current, out var resolver))
                        return new(resolver);
            }

            return default;
        }

        private readonly record struct TitleResolverCacheKey(Type ModelType, int Generation);

        private readonly record struct TitleResolverLookup(Func<AbstractModel, LocString?>? Resolver);
    }
}
