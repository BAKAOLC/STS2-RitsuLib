using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.Transforms
{
    /// <summary>
    ///     Per-mod registration surface for vanilla card transform listeners.
    ///     原版卡牌转换监听器的按 mod 注册入口。
    /// </summary>
    public sealed class ModCardTransformRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModCardTransformRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<(string ModId, string ListenerId), ListenerEntry> Listeners = [];
        private static long _nextRegistrationOrder;

        private readonly string _modId;

        private ModCardTransformRegistry(string modId)
        {
            _modId = modId;
        }

        /// <summary>
        ///     Returns the singleton transform registry for <paramref name="modId" />, creating it on first use.
        ///     返回 <paramref name="modId" /> 对应的单例转换注册表，并在首次使用时创建。
        /// </summary>
        public static ModCardTransformRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var existing))
                    return existing;

                var created = new ModCardTransformRegistry(modId);
                Registries[modId] = created;
                return created;
            }
        }

        /// <summary>
        ///     Registers or replaces a listener that receives every completed vanilla card transform.
        ///     注册或替换一个监听器，以接收每次完成的原版卡牌转换。
        /// </summary>
        public void Register(string listenerId, Action<ModCardTransformContext> listener)
        {
            Register(listenerId, _ => true, listener);
        }

        /// <summary>
        ///     Registers or replaces a listener with a custom predicate.
        ///     注册或替换一个带自定义谓词的监听器。
        /// </summary>
        public void Register(
            string listenerId,
            Func<ModCardTransformContext, bool> predicate,
            Action<ModCardTransformContext> listener)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(listenerId);
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(listener);

            lock (SyncRoot)
            {
                var key = (_modId, listenerId);
                var registrationOrder = Listeners.TryGetValue(key, out var existing)
                    ? existing.RegistrationOrder
                    : _nextRegistrationOrder++;

                Listeners[key] = new(_modId, listenerId, predicate, listener, registrationOrder);
            }
        }

        /// <summary>
        ///     Registers or replaces a typed listener for a card transform pair.
        ///     注册或替换一个针对卡牌转换类型对的类型化监听器。
        /// </summary>
        public void Register<TOriginal, TReplacement>(
            string listenerId,
            Action<TOriginal, TReplacement> listener)
            where TOriginal : CardModel
            where TReplacement : CardModel
        {
            ArgumentNullException.ThrowIfNull(listener);

            Register(
                listenerId,
                context => context is { Original: TOriginal, Replacement: TReplacement },
                context => listener((TOriginal)context.Original, (TReplacement)context.Replacement));
        }

        /// <summary>
        ///     Registers or replaces a typed listener for cards transformed away from <typeparamref name="TOriginal" />.
        ///     注册或替换一个监听器，用于处理从 <typeparamref name="TOriginal" /> 转换出去的卡牌。
        /// </summary>
        public void RegisterFrom<TOriginal>(
            string listenerId,
            Action<TOriginal, CardModel> listener)
            where TOriginal : CardModel
        {
            ArgumentNullException.ThrowIfNull(listener);

            Register(
                listenerId,
                context => context.Original is TOriginal,
                context => listener((TOriginal)context.Original, context.Replacement));
        }

        /// <summary>
        ///     Registers or replaces a typed listener for cards transformed into <typeparamref name="TReplacement" />.
        ///     注册或替换一个监听器，用于处理转换成 <typeparamref name="TReplacement" /> 的卡牌。
        /// </summary>
        public void RegisterTo<TReplacement>(
            string listenerId,
            Action<CardModel, TReplacement> listener)
            where TReplacement : CardModel
        {
            ArgumentNullException.ThrowIfNull(listener);

            Register(
                listenerId,
                context => context.Replacement is TReplacement,
                context => listener(context.Original, (TReplacement)context.Replacement));
        }

        /// <summary>
        ///     Removes a previously registered listener from this registry's mod.
        ///     从当前注册表 mod 移除先前注册的监听器。
        /// </summary>
        public bool Unregister(string listenerId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(listenerId);

            lock (SyncRoot)
            {
                return Listeners.Remove((_modId, listenerId));
            }
        }

        internal static void NotifyTransformed(
            CardModel original,
            CardModel replacement,
            CardPile originalPile,
            int originalPileIndex)
        {
            ArgumentNullException.ThrowIfNull(original);
            ArgumentNullException.ThrowIfNull(replacement);
            ArgumentNullException.ThrowIfNull(originalPile);

            var context = new ModCardTransformContext(original, replacement, originalPile, originalPileIndex);
            ListenerEntry[] listeners;
            lock (SyncRoot)
            {
                listeners = Listeners.Values.OrderBy(static entry => entry.RegistrationOrder).ToArray();
            }

            foreach (var entry in listeners)
                try
                {
                    if (entry.Predicate(context))
                        entry.Listener(context);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[ModCardTransformRegistry] Listener '{entry.ModId}/{entry.ListenerId}' failed for {original.Id}: {ex.Message}");
                }
        }

        private sealed record ListenerEntry(
            string ModId,
            string ListenerId,
            Func<ModCardTransformContext, bool> Predicate,
            Action<ModCardTransformContext> Listener,
            long RegistrationOrder);
    }
}
