using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace STS2RitsuLib.Ui.Overlay
{
    internal sealed partial class RitsuOverlaySubmenuStack : NSubmenuStack
    {
        private readonly Dictionary<Type, NSubmenu> _registeredSubmenus = [];

        public override T PushSubmenuType<T>()
        {
            var submenu = GetSubmenuType<T>();
            if (!ReferenceEquals(Peek(), submenu))
                Push(submenu);
            return submenu;
        }

        public override T GetSubmenuType<T>()
        {
            return (T)GetSubmenuType(typeof(T));
        }

        public override NSubmenu PushSubmenuType(Type type)
        {
            var submenu = GetSubmenuType(type);
            if (!ReferenceEquals(Peek(), submenu))
                Push(submenu);
            return submenu;
        }

        public override NSubmenu GetSubmenuType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            if (!typeof(NSubmenu).IsAssignableFrom(type) || type.IsAbstract)
                throw new ArgumentException($"Type '{type}' must be a non-abstract NSubmenu.", nameof(type));
            if (_registeredSubmenus.TryGetValue(type, out var existing) && IsInstanceValid(existing))
                return existing;

            var submenu = Activator.CreateInstance(type) as NSubmenu ??
                          throw new InvalidOperationException($"Could not create submenu type '{type}'.");
            submenu.Name = type.Name;
            submenu.Visible = false;
            submenu.MouseFilter = MouseFilterEnum.Ignore;
            submenu.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(submenu);
            _registeredSubmenus[type] = submenu;
            return submenu;
        }
    }
}
