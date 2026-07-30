using System.Reflection;
using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Utils.Persistence.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adapts an object that defines <c>FromVersion</c>, <c>ToVersion</c>, and
    ///         <c>Migrate(JsonObject)</c> to <see cref="IMigration" /> without requiring its type to reference RitsuLib.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将定义了 <c>FromVersion</c>、<c>ToVersion</c> 和 <c>Migrate(JsonObject)</c> 的对象适配为
    ///         <see cref="IMigration" />，而无需其类型引用 RitsuLib。
    ///     </para>
    /// </summary>
    public sealed class InteropMigrationAdapter : IMigration
    {
        private readonly Func<JsonObject, bool> _migrate;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an adapter for an existing migration instance that defines the required version members
        ///         and migration method.
        ///     </para>
        ///     <para xml:lang="zh-CN">为定义了所需版本成员和迁移方法的现有迁移实例创建适配器。</para>
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
        ///     <para xml:lang="en">
        ///         Attempts to invoke a parameterless constructor for the migration type and wrap the resulting
        ///         instance.
        ///     </para>
        ///     <para xml:lang="zh-CN">尝试调用迁移类型的无参构造函数，并包装所创建的实例。</para>
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
