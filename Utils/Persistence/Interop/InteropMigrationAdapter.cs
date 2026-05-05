using System.Reflection;
using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Utils.Persistence.Interop
{
    /// <summary>
    ///     Adapts a duck-typed migration instance (FromVersion/ToVersion + Migrate(JsonObject)) into
    ///     <see cref="IMigration" /> without requiring the migration type to reference RitsuLib types.
    /// </summary>
    public sealed class InteropMigrationAdapter : IMigration
    {
        private readonly Func<JsonObject, bool> _migrate;

        /// <summary>
        ///     Creates an adapter from an existing migration instance (must expose FromVersion, ToVersion, Migrate).
        /// </summary>
        public InteropMigrationAdapter(object instance)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var type = instance.GetType();

            FromVersion = ReadIntMember(type, instance, "FromVersion");
            ToVersion = ReadIntMember(type, instance, "ToVersion");

            var migrate = type.GetMethod(
                "Migrate",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                [typeof(JsonObject)]);

            if (migrate == null || migrate.ReturnType != typeof(bool))
                throw new InvalidOperationException(
                    $"Migration type '{type.FullName}' must declare 'bool Migrate(JsonObject data)'.");

            _migrate = (Func<JsonObject, bool>)Delegate.CreateDelegate(typeof(Func<JsonObject, bool>), instance,
                migrate);
        }

        /// <inheritdoc />
        public int FromVersion { get; }

        /// <inheritdoc />
        public int ToVersion { get; }

        /// <inheritdoc />
        public bool Migrate(JsonObject data)
        {
            return _migrate(data);
        }

        /// <summary>
        ///     Attempts to create a migration instance via parameterless ctor and wrap it.
        /// </summary>
        public static bool TryCreateFromType(Type migrationType, out InteropMigrationAdapter? adapter)
        {
            adapter = null;
            try
            {
                if (migrationType is not { IsClass: true } || migrationType.IsAbstract)
                    return false;

                var ctor = migrationType.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                    return false;

                var instance = Activator.CreateInstance(migrationType);
                if (instance == null)
                    return false;

                adapter = new(instance);
                return true;
            }
            catch
            {
                adapter = null;
                return false;
            }
        }

        private static int ReadIntMember(Type type, object instance, string name)
        {
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(int))
                return (int)(prop.GetValue(instance) ?? 0);

            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(int))
                return (int)(field.GetValue(instance) ?? 0);

            throw new InvalidOperationException(
                $"Migration type '{type.FullName}' must expose int '{name}' as property or field.");
        }
    }
}
