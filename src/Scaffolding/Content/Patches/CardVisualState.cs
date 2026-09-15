using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    internal sealed class CardVisualState(NCard card)
    {
        private readonly Dictionary<CanvasItem, NodeState> _nodes = [];
        private int _depth;
        private bool _reset;
        private bool _updating;

        internal NCard Card { get; } = card;

        internal bool Enter()
        {
            if (_updating || _reset)
                return false;
            if (_depth != 0)
            {
                _depth++;
                return true;
            }

            _updating = true;
            try
            {
                Restore();
                _depth = 1;
                return true;
            }
            finally
            {
                _updating = false;
            }
        }

        internal void Leave(bool succeeded)
        {
            if (_reset || --_depth != 0 || !succeeded || !GodotObject.IsInstanceValid(Card) ||
                Card.IsQueuedForDeletion() || Card.Model == null)
                return;
            _updating = true;
            try
            {
                CardVisualOverrides.Apply(this, Card.Model);
            }
            catch
            {
                Restore();
                throw;
            }
            finally
            {
                _updating = false;
            }
        }

        internal void Reset()
        {
            _reset = true;
            Restore();
            _nodes.Clear();
        }

        internal void SetTexture(string path, Texture2D? value, Func<Texture2D?>? restore = null)
        {
            if (Card.GetNodeOrNull<TextureRect>(path) is not { } node)
                return;
            GetState(node).Texture.Apply(node.Texture, value, texture => node.Texture = texture, restore);
        }

        internal void SetMaterial(string path, Material? value, Func<Material?>? restore = null)
        {
            if (Card.GetNodeOrNull<CanvasItem>(path) is not { } node)
                return;
            GetState(node).Material.Apply(node.Material, value, material => node.Material = material, restore);
        }

        internal void SetVisible(string path, bool value)
        {
            if (Card.GetNodeOrNull<CanvasItem>(path) is not { } node)
                return;
            var state = GetState(node);
            if (!state.HasVisibility)
            {
                state.BaseVisibility = node.Visible;
                state.HasVisibility = true;
            }

            node.Visible = value;
            state.AppliedVisibility = value;
        }

        private NodeState GetState(CanvasItem node)
        {
            if (_nodes.TryGetValue(node, out var state))
                return state;
            state = new();
            _nodes.Add(node, state);
            return state;
        }

        private void Restore()
        {
            foreach (var (node, state) in _nodes)
            {
                if (!GodotObject.IsInstanceValid(node))
                    continue;
                if (node is TextureRect texture)
                    state.Texture.Restore(texture.Texture, value => texture.Texture = value);
                state.Material.Restore(node.Material, value => node.Material = value);
                if (state.HasVisibility && node.Visible == state.AppliedVisibility)
                    node.Visible = state.BaseVisibility;
                state.HasVisibility = false;
            }
        }

        private sealed class NodeState
        {
            internal ResourceState<Texture2D> Texture { get; } = new();
            internal ResourceState<Material> Material { get; } = new();
            internal bool HasVisibility { get; set; }
            internal bool BaseVisibility { get; set; }
            internal bool AppliedVisibility { get; set; }
        }

        private sealed class ResourceState<T> where T : Resource
        {
            private T? _baseline;
            private T? _applied;
            private bool _owned;
            private Func<T?>? _restore;

            internal void Apply(T? current, T? value, Action<T?> setter, Func<T?>? restore)
            {
                if (!_owned)
                {
                    _baseline = current;
                    _owned = true;
                }

                if (restore != null)
                    _restore = restore;
                setter(value);
                _applied = value;
            }

            internal void Restore(T? current, Action<T?> setter)
            {
                if (!_owned)
                    return;
                var baseline = _baseline;
                var restore = _restore;
                var applied = _applied;
                _baseline = null;
                _applied = null;
                _restore = null;
                _owned = false;
                if (!Same(current, applied))
                    return;
                if (restore != null)
                    baseline = restore();
                setter(baseline == null || GodotObject.IsInstanceValid(baseline) ? baseline : null);
            }

            private static bool Same(T? left, T? right)
            {
                return ReferenceEquals(left, right) ||
                       left != null && right != null &&
                       GodotObject.IsInstanceValid(left) && GodotObject.IsInstanceValid(right) &&
                       left.GetInstanceId() == right.GetInstanceId();
            }
        }
    }
}
