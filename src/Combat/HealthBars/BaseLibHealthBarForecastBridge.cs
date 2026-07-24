using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     When BaseLib is loaded, registers <see cref="HealthBarForecastRegistry.GetSegments" /> with BaseLib's
    ///     <c>HealthBarForecastRegistry.RegisterForeign</c> so a single renderer can consume Ritsu-typed segments.
    ///     加载 BaseLib 时，将 <see cref="HealthBarForecastRegistry.GetSegments" /> 注册到 BaseLib 的
    ///     <c>HealthBarForecastRegistry.RegisterForeign</c>，使单个渲染器可以消费 Ritsu 类型的片段。
    /// </summary>
    /// <remarks>
    ///     <see cref="ShouldRitsuRendererStandDown" /> becomes true after a successful bridge so duplicate overlays are
    ///     not drawn.
    ///     成功桥接后，<see cref="ShouldRitsuRendererStandDown" /> 变为 true，从而不绘制重复覆盖层。
    /// </remarks>
    internal static class BaseLibHealthBarForecastBridge
    {
        private const string SourceId = "ritsulib.registry";
        private static bool _registered;
        private static bool _legacyImportMode;
        private static bool _baselibSupportsForecastInterop;
        private static bool _baselibSupportsRitsuRenderProtocol;
        private static bool _loggedMissingInterop;
        private static bool _loggedMissingRegisterForeign;
        private static bool _loggedLegacyImportMode;
        private static bool _loggedMissingLegacyImport;
        private static bool _loggedLegacyImportFailure;
        private static Action<string, string, Func<Creature, IEnumerable<object>>>? _registerForeign;
        private static MethodInfo? _getSegmentsMethod;

        private static readonly ConcurrentDictionary<Type, LegacyImportReader> LegacyImportReaders = new();

        /// <summary>
        ///     When <see langword="true" />, Ritsu's <c>NHealthBar</c> forecast postfixes should skip drawing because BaseLib
        ///     already merged this mod's segments.
        ///     为 <see langword="true" /> 时，Ritsu 的 <c>NHealthBar</c> forecast postfix 应跳过绘制，因为 BaseLib
        ///     已经合并了此 mod 的片段。
        /// </summary>
        public static bool ShouldRitsuRendererStandDown()
        {
            return _registered && _baselibSupportsForecastInterop && _baselibSupportsRitsuRenderProtocol;
        }

        public static bool ShouldSuppressBaseLibRenderer()
        {
            return !ShouldRitsuRendererStandDown() && (_legacyImportMode || TryResolveLegacyImportApi(out _));
        }

        internal static IReadOnlyList<BaseLibImportedHealthBarForecastSegment> GetImportedSegments(Creature creature)
        {
            if (ShouldRitsuRendererStandDown() || !TryResolveLegacyImportApi(out var getSegments))
                return [];

            try
            {
                var rawEntries = FastMethodInvoker.InvokeStatic1(getSegments, creature);
                if (rawEntries == null)
                    return [];

                var reader = LegacyImportReaders.GetOrAdd(rawEntries.GetType(), CreateLegacyImportReader);
                return reader.Read(rawEntries);
            }
            catch (Exception ex)
            {
                if (_loggedLegacyImportFailure)
                    return [];

                _loggedLegacyImportFailure = true;
                RitsuLibFramework.Logger.Warn($"[HealthBarForecast] Failed to import BaseLib forecast segments: {ex}");
                return [];
            }
        }

        /// <summary>
        ///     Attempts foreign registration from <c>NHealthBar._Ready</c> (early load path).
        ///     从 <c>NHealthBar._Ready</c> 尝试 foreign 注册（早期加载路径）。
        /// </summary>
        public static void TryRegisterPrimary()
        {
            if (_registered || _legacyImportMode)
                return;
            TryRegisterCore();
        }

        /// <summary>
        ///     Attempts foreign registration from forecast render path if <see cref="TryRegisterPrimary" /> did not run yet.
        ///     如果 <see cref="TryRegisterPrimary" /> 尚未运行，则从 forecast 渲染路径尝试 foreign 注册。
        /// </summary>
        public static void TryRegisterSecondary()
        {
            if (_registered || _legacyImportMode)
                return;
            TryRegisterCore();
        }

        /// <summary>
        ///     Alias for <see cref="TryRegisterPrimary" />.
        ///     <see cref="TryRegisterPrimary" /> 的别名。
        /// </summary>
        public static void TryRegister()
        {
            TryRegisterPrimary();
        }

        private static void TryRegisterCore()
        {
            if (_registered || _legacyImportMode)
                return;
            if (!ExternalFrameworkRegistry.IsFrameworkPresent(ExternalFrameworkIds.BaseLib))
                return;

            try
            {
                var registryType = ResolveBaseLibRegistryType();
                if (registryType == null)
                    return;

                var registerForeign = registryType.GetMethod(
                    "RegisterForeign",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(string), typeof(string), typeof(Func<Creature, IEnumerable<object>>)],
                    null);

                if (registerForeign == null)
                {
                    _baselibSupportsForecastInterop = false;
                    _legacyImportMode = TryResolveLegacyImportApi(registryType, out _);
                    if (_loggedMissingRegisterForeign) return;
                    _loggedMissingRegisterForeign = true;
                    RitsuLibFramework.Logger.Warn(
                        $"[HealthBarForecast] BaseLib registry type '{registryType.FullName}' does not expose " +
                        "RegisterForeign(string, string, Func<Creature, IEnumerable<object>>); forecast interop unavailable.");

                    return;
                }

                if (!BaseLibSupportsRitsuRenderProtocol(registryType))
                {
                    _baselibSupportsForecastInterop = true;
                    _baselibSupportsRitsuRenderProtocol = false;
                    _legacyImportMode = TryResolveLegacyImportApi(registryType, out _);
                    if (_loggedLegacyImportMode) return;
                    _loggedLegacyImportMode = true;
                    RitsuLibFramework.Logger.Info(
                        "[HealthBarForecast] BaseLib forecast registry uses the legacy render protocol; " +
                        "RitsuLib will import BaseLib segments and keep its own renderer active.");

                    return;
                }

                var provider = GetSegmentsForCreature;
                _registerForeign ??=
                    registerForeign.CreateDelegate<Action<string, string, Func<Creature, IEnumerable<object>>>>();
                _registerForeign(Const.ModId, SourceId, provider);
                _registered = true;
                _baselibSupportsForecastInterop = true;
                _baselibSupportsRitsuRenderProtocol = true;
                RitsuLibFramework.Logger.Info("[HealthBarForecast] Registered BaseLib bridge provider.");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[HealthBarForecast] Failed to register BaseLib bridge provider: {ex}");
            }
        }

        private static IEnumerable<object> GetSegmentsForCreature(Creature creature)
        {
            return
            [
                .. HealthBarForecastRegistry.GetSegments(creature)
                    .Select(registered => (object)registered.Segment),
            ];
        }

        private static Type? ResolveBaseLibRegistryType()
        {
            var registryType = ResolveRegistryType();
            _baselibSupportsForecastInterop = registryType != null;

            if (!_baselibSupportsForecastInterop)
            {
                if (_loggedMissingInterop)
                    return null;
                _loggedMissingInterop = true;
                RitsuLibFramework.Logger.Info(
                    "[HealthBarForecast] BaseLib detected but forecast interop API is unavailable.");
                return null;
            }

            _loggedMissingInterop = false;

            return registryType;
        }

        private static bool TryResolveLegacyImportApi(out MethodInfo getSegments)
        {
            getSegments = null!;
            if (_getSegmentsMethod != null)
            {
                getSegments = _getSegmentsMethod;
                return true;
            }

            var registryType = ResolveRegistryType();
            return registryType != null && TryResolveLegacyImportApi(registryType, out getSegments);
        }

        private static bool TryResolveLegacyImportApi(Type registryType, out MethodInfo getSegments)
        {
            getSegments = null!;
            if (_getSegmentsMethod != null)
            {
                getSegments = _getSegmentsMethod;
                return true;
            }

            var method = registryType.GetMethod(
                "GetSegments",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                [typeof(Creature)],
                null);
            if (method == null || !typeof(IEnumerable).IsAssignableFrom(method.ReturnType))
            {
                if (_loggedMissingLegacyImport) return false;
                _loggedMissingLegacyImport = true;
                RitsuLibFramework.Logger.Warn(
                    "[HealthBarForecast] BaseLib legacy forecast registry does not expose importable GetSegments.");

                return false;
            }

            _getSegmentsMethod = method;
            getSegments = method;
            return true;
        }

        private static bool BaseLibSupportsRitsuRenderProtocol(Type registryType)
        {
            var segmentType = registryType.Assembly.GetType("BaseLib.Hooks.HealthBarForecastSegment") ??
                              ExternalFrameworkRegistry.ResolveType("BaseLib.Hooks.HealthBarForecastSegment");

            _baselibSupportsRitsuRenderProtocol =
                segmentType != null &&
                HasPublicInstanceProperty(segmentType, "LeftOriginLayout") &&
                HasPublicInstanceProperty(segmentType, "LeftExclusiveZGroup") &&
                HasPublicInstanceProperty(segmentType, "AffectsHpLabel");

            return _baselibSupportsRitsuRenderProtocol;
        }

        private static bool HasPublicInstanceProperty(Type type, string name)
        {
            return type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public) != null;
        }

        private static LegacyImportReader CreateLegacyImportReader(Type collectionType)
        {
            var entryType = ResolveEnumerableElementType(collectionType);
            if (entryType == null)
                return LegacyImportReader.Unavailable;

            var segment = entryType.GetProperty("Segment", BindingFlags.Instance | BindingFlags.Public);
            var sequenceOrder = entryType.GetProperty("SequenceOrder", BindingFlags.Instance | BindingFlags.Public);
            if (segment == null || sequenceOrder == null)
                return LegacyImportReader.Unavailable;

            var method = typeof(BaseLibHealthBarForecastBridge)
                .GetMethod(nameof(CreateTypedLegacyImportReader), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(entryType, segment.PropertyType);
            return (LegacyImportReader)method.Invoke(null, [segment, sequenceOrder])!;
        }

        private static LegacyImportReader CreateTypedLegacyImportReader<TEntry, TSegment>(
            PropertyInfo segmentProperty,
            PropertyInfo sequenceOrderProperty)
        {
            var segmentReader = CreateTypedSegmentReader<TSegment>();
            if (segmentReader == null)
                return LegacyImportReader.Unavailable;

            var readSegment = FastMethodInvoker.CreateInstanceGetter<TEntry, TSegment>(segmentProperty);
            var readSequenceOrder = FastMethodInvoker.CreateInstanceGetter<TEntry, long>(sequenceOrderProperty);

            return new(entries => ImportTypedSegments((IEnumerable<TEntry>)entries, readSegment, readSequenceOrder,
                segmentReader));
        }

        private static IReadOnlyList<BaseLibImportedHealthBarForecastSegment> ImportTypedSegments<TEntry, TSegment>(
            IEnumerable<TEntry> entries,
            Func<TEntry, TSegment> readSegment,
            Func<TEntry, long> readSequenceOrder,
            TypedSegmentReader<TSegment> segmentReader)
        {
            List<BaseLibImportedHealthBarForecastSegment> segments = [];

            if (entries is IReadOnlyList<TEntry> list)
                foreach (var entry in list)
                    AddImportedSegment(entry);
            else
                foreach (var entry in entries)
                    AddImportedSegment(entry);

            return segments;

            void AddImportedSegment(TEntry entry)
            {
                var segment = readSegment(entry);
                var amount = segmentReader.ReadAmount(segment);
                if (amount <= 0)
                    return;

                segments.Add(new(
                    amount,
                    segmentReader.ReadColor(segment),
                    ToGrowthDirection(segmentReader.ReadDirection(segment)),
                    segmentReader.ReadOrder(segment),
                    readSequenceOrder(entry),
                    segmentReader.ReadOverlayMaterial(segment),
                    segmentReader.ReadOverlaySelfModulate(segment),
                    ToLeftOriginLayout(segmentReader.ReadLeftOriginLayout(segment)),
                    segmentReader.ReadLeftExclusiveZGroup(segment),
                    segmentReader.ReadAffectsHpLabel(segment)));
            }
        }

        private static TypedSegmentReader<TSegment>? CreateTypedSegmentReader<TSegment>()
        {
            var type = typeof(TSegment);
            var amount = type.GetProperty("Amount", BindingFlags.Instance | BindingFlags.Public);
            var color = type.GetProperty("Color", BindingFlags.Instance | BindingFlags.Public);
            var direction = type.GetProperty("Direction", BindingFlags.Instance | BindingFlags.Public);
            var order = type.GetProperty("Order", BindingFlags.Instance | BindingFlags.Public);
            var overlayMaterial = type.GetProperty("OverlayMaterial", BindingFlags.Instance | BindingFlags.Public);
            var overlaySelfModulate =
                type.GetProperty("OverlaySelfModulate", BindingFlags.Instance | BindingFlags.Public);
            var leftOriginLayout = type.GetProperty("LeftOriginLayout", BindingFlags.Instance | BindingFlags.Public);
            var leftExclusiveZGroup =
                type.GetProperty("LeftExclusiveZGroup", BindingFlags.Instance | BindingFlags.Public);
            var affectsHpLabel = type.GetProperty("AffectsHpLabel", BindingFlags.Instance | BindingFlags.Public);

            if (amount?.PropertyType != typeof(int) ||
                color?.PropertyType != typeof(Color) ||
                direction == null ||
                !CanReadEnumOrdinal(direction))
                return null;

            var readAmount = FastMethodInvoker.CreateInstanceGetter<TSegment, int>(amount);
            var readColor = FastMethodInvoker.CreateInstanceGetter<TSegment, Color>(color);
            var readDirection = FastMethodInvoker.CreateInstanceGetter<TSegment, int>(direction);
            var readOrder = order?.PropertyType == typeof(int)
                ? FastMethodInvoker.CreateInstanceGetter<TSegment, int>(order)
                : null;
            var readOverlayMaterial =
                overlayMaterial != null && typeof(Material).IsAssignableFrom(overlayMaterial.PropertyType)
                    ? FastMethodInvoker.CreateInstanceGetter<TSegment, Material?>(overlayMaterial)
                    : null;
            var readOverlaySelfModulate = overlaySelfModulate?.PropertyType == typeof(Color?)
                ? FastMethodInvoker.CreateInstanceGetter<TSegment, Color?>(overlaySelfModulate)
                : null;
            var readLeftOriginLayout = leftOriginLayout != null && CanReadEnumOrdinal(leftOriginLayout)
                ? FastMethodInvoker.CreateInstanceGetter<TSegment, int>(leftOriginLayout)
                : null;
            var readLeftExclusiveZGroup = leftExclusiveZGroup?.PropertyType == typeof(int)
                ? FastMethodInvoker.CreateInstanceGetter<TSegment, int>(leftExclusiveZGroup)
                : null;
            var readAffectsHpLabel = affectsHpLabel?.PropertyType == typeof(bool)
                ? FastMethodInvoker.CreateInstanceGetter<TSegment, bool>(affectsHpLabel)
                : null;

            return new(
                readAmount,
                readColor,
                readDirection,
                readOrder ?? (_ => 0),
                readOverlayMaterial ?? (_ => null),
                readOverlaySelfModulate ?? (_ => null),
                readLeftOriginLayout ?? (_ => 0),
                readLeftExclusiveZGroup ?? (_ => 0),
                readAffectsHpLabel ?? (_ => true));
        }

        private static Type? ResolveEnumerableElementType(Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();

            return collectionType.GetInterfaces()
                .Append(collectionType)
                .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(type => type.GetGenericArguments()[0])
                .FirstOrDefault();
        }

        private static bool CanReadEnumOrdinal(PropertyInfo property)
        {
            return property.PropertyType.IsEnum || property.PropertyType == typeof(int);
        }

        private static HealthBarForecastGrowthDirection ToGrowthDirection(int value)
        {
            return value == 1
                ? HealthBarForecastGrowthDirection.FromLeft
                : HealthBarForecastGrowthDirection.FromRight;
        }

        private static HealthBarForecastLeftOriginLayout ToLeftOriginLayout(int value)
        {
            return value == 1
                ? HealthBarForecastLeftOriginLayout.OverlapFromOrigin
                : HealthBarForecastLeftOriginLayout.Chained;
        }

        private static Type? ResolveRegistryType()
        {
            return ExternalFrameworkRegistry.ResolveType("BaseLib.Hooks.HealthBarForecastRegistry");
        }

        internal readonly record struct BaseLibImportedHealthBarForecastSegment(
            int Amount,
            Color Color,
            HealthBarForecastGrowthDirection Direction,
            int Order,
            long SequenceOrder,
            Material? OverlayMaterial,
            Color? OverlaySelfModulate,
            HealthBarForecastLeftOriginLayout LeftOriginLayout,
            int LeftExclusiveZGroup,
            bool AffectsHpLabel);

        private sealed class LegacyImportReader(
            Func<object, IReadOnlyList<BaseLibImportedHealthBarForecastSegment>>? read)
        {
            public static LegacyImportReader Unavailable { get; } = new(null);

            public IReadOnlyList<BaseLibImportedHealthBarForecastSegment> Read(object entries)
            {
                return read?.Invoke(entries) ?? [];
            }
        }

        private sealed record TypedSegmentReader<TSegment>(
            Func<TSegment, int> ReadAmount,
            Func<TSegment, Color> ReadColor,
            Func<TSegment, int> ReadDirection,
            Func<TSegment, int> ReadOrder,
            Func<TSegment, Material?> ReadOverlayMaterial,
            Func<TSegment, Color?> ReadOverlaySelfModulate,
            Func<TSegment, int> ReadLeftOriginLayout,
            Func<TSegment, int> ReadLeftExclusiveZGroup,
            Func<TSegment, bool> ReadAffectsHpLabel);
    }
}
