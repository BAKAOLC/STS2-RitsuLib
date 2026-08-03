using System.Collections;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace STS2RitsuLib.Settings
{
    internal static class RitsuDebugIntentGraphInterop
    {
        private const float EmbeddedGraphScale = 0.68f;
        private const float GridSize = 80f;
        private const string GraphScenePath = "res://intentgraph2/scenes/intent_graph.tscn";
        private const string GeneratorTypeName = "IntentGraph2.Utils.GraphGenerator.IntentGraphGenerator";

        internal static bool TryCreate(
            Creature creature,
            out RitsuDebugIntentGraphView? graphView)
        {
            ArgumentNullException.ThrowIfNull(creature);
            graphView = null;
            try
            {
                var generatorType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(static assembly => assembly.GetType(GeneratorTypeName, false))
                    .FirstOrDefault(static type => type != null);
                if (generatorType == null || !ResourceLoader.Exists(GraphScenePath))
                    return false;

                var graph = ResolveGraph(generatorType, creature);
                if (graph == null || ResourceLoader.Load<PackedScene>(GraphScenePath) is not { } scene ||
                    scene.Instantiate() is not Control graphControl)
                    return false;

                var settings = ResolveDisplaySettings(generatorType.Assembly);
                var graphType = graphControl.GetType();
                if (!TrySetProperty(graphType, graphControl, "Graph", graph) ||
                    !TrySetProperty(graphType, graphControl, "GraphScale", Vector2.One * EmbeddedGraphScale) ||
                    !TrySetProperty(graphType, graphControl, "Monster", creature.Monster) ||
                    !TrySetProperty(graphType, graphControl, "AnimatedIcons", settings.AnimatedIcons) ||
                    !TrySetProperty(graphType, graphControl, "ShowCurrentMove", settings.ShowCurrentMove))
                {
                    graphControl.QueueFree();
                    return false;
                }

                graphControl.ProcessMode = settings.AnimatedIcons
                    ? Node.ProcessModeEnum.Inherit
                    : Node.ProcessModeEnum.Disabled;
                IgnoreMouseInput(graphControl);
                if (!TryReadGraphLayout(graph, EmbeddedGraphScale, out var minimumSize, out var moveRects))
                {
                    graphControl.QueueFree();
                    return false;
                }

                graphView = new(graphControl, minimumSize, moveRects);
                return true;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DebugToolsUi] Intent Graph integration is unavailable: {ex.Message}");
                return false;
            }
        }

        private static object? ResolveGraph(Type generatorType, Creature creature)
        {
            var modType = generatorType.Assembly.GetType("IntentGraph2.IntentGraphMod", false);
            var generatedGraphs = modType?
                .GetField("GeneratedGraphs", BindingFlags.Public | BindingFlags.Static)?
                .GetValue(null);
            if (generatedGraphs != null)
            {
                var tryGetValue = generatedGraphs.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(static method =>
                        method.Name == "TryGetValue" && method.GetParameters().Length == 2);
                if (tryGetValue != null)
                {
                    object?[] arguments = [creature.Monster, null];
                    if (tryGetValue.Invoke(generatedGraphs, arguments) is true && arguments[1] != null)
                        return arguments[1];
                }
            }

            return generatorType
                .GetMethod(
                    "GenerateAndCacheGraphForCreature",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(Creature)],
                    null)?
                .Invoke(null, [creature]);
        }

        private static RitsuDebugIntentGraphDisplaySettings ResolveDisplaySettings(Assembly assembly)
        {
            var config = assembly.GetType("IntentGraph2.IntentGraphMod", false)?
                .GetProperty("Config", BindingFlags.Public | BindingFlags.Static)?
                .GetValue(null);
            if (config == null)
                return new(false, true);

            var type = config.GetType();
            var animatedIcons = TryReadBool(type, config, "UseAnimatedIntentIcon", out var animated) && animated;
            var showCurrentMove = !TryReadBool(type, config, "ShowCurrentMove", out var showCurrent) || showCurrent;
            return new(animatedIcons, showCurrentMove);
        }

        private static bool TryReadGraphLayout(
            object graph,
            float graphScale,
            out Vector2 minimumSize,
            out IReadOnlyDictionary<string, Rect2> moveRects)
        {
            minimumSize = Vector2.Zero;
            moveRects = new Dictionary<string, Rect2>(StringComparer.Ordinal);
            var graphType = graph.GetType();
            if (!TryReadFloat(graphType, graph, "Width", out var width) ||
                !TryReadFloat(graphType, graph, "Height", out var height) ||
                graphType.GetProperty("Moves", BindingFlags.Public | BindingFlags.Instance)?.GetValue(graph)
                    is not IEnumerable moves)
                return false;

            var rects = new Dictionary<string, Rect2>(StringComparer.Ordinal);
            foreach (var move in moves)
            {
                if (move == null || !TryReadMoveRect(move, graphScale, out var ids, out var rect))
                    continue;
                foreach (var id in ids.Where(static id => !string.IsNullOrWhiteSpace(id)))
                    rects[id] = rects.TryGetValue(id, out var existing) ? existing.Merge(rect) : rect;
            }

            minimumSize = new(Math.Max(GridSize * graphScale, width * GridSize * graphScale),
                Math.Max(GridSize * graphScale, height * GridSize * graphScale));
            moveRects = rects;
            return true;
        }

        private static bool TryReadMoveRect(
            object move,
            float graphScale,
            out IReadOnlyList<string> ids,
            out Rect2 rect)
        {
            ids = [];
            rect = default;
            var type = move.GetType();
            var id = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)?.GetValue(move) as string;
            var alternateIds = type.GetProperty("Ids", BindingFlags.Public | BindingFlags.Instance)?.GetValue(move)
                as IEnumerable<string>;
            var resolvedIds = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(id))
                resolvedIds.Add(id);
            if (alternateIds != null)
                resolvedIds.UnionWith(alternateIds.Where(static value => !string.IsNullOrWhiteSpace(value)));

            if (type.GetProperty("Icons", BindingFlags.Public | BindingFlags.Instance)?.GetValue(move)
                is not IEnumerable icons)
                return false;

            var hasRect = false;
            foreach (var icon in icons)
            {
                if (icon == null || !TryReadFloat(icon.GetType(), icon, "X", out var x) ||
                    !TryReadFloat(icon.GetType(), icon, "Y", out var y))
                    continue;
                var iconRect = new Rect2(x * GridSize * graphScale, y * GridSize * graphScale,
                    GridSize * graphScale, GridSize * graphScale);
                rect = hasRect ? rect.Merge(iconRect) : iconRect;
                hasRect = true;
            }

            ids = [.. resolvedIds];
            return hasRect && ids.Count > 0;
        }

        private static bool TryReadFloat(Type type, object instance, string propertyName, out float value)
        {
            value = 0f;
            var raw = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
            if (raw is not IConvertible convertible)
                return false;
            value = convertible.ToSingle(null);
            return float.IsFinite(value);
        }

        private static bool TryReadBool(Type type, object instance, string propertyName, out bool value)
        {
            value = false;
            var raw = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
            if (raw is not bool result)
                return false;
            value = result;
            return true;
        }

        private static bool TrySetProperty(Type type, object instance, string propertyName, object? value)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanWrite != true)
                return false;
            property.SetValue(instance, value);
            return true;
        }

        private static void IgnoreMouseInput(Node node)
        {
            if (node is Control control)
                control.MouseFilter = Control.MouseFilterEnum.Ignore;
            foreach (var child in node.GetChildren())
                IgnoreMouseInput(child);
        }
    }

    internal sealed record RitsuDebugIntentGraphView(
        Control Control,
        Vector2 MinimumSize,
        IReadOnlyDictionary<string, Rect2> MoveRects);

    internal readonly record struct RitsuDebugIntentGraphDisplaySettings(
        bool AnimatedIcons,
        bool ShowCurrentMove);
}
