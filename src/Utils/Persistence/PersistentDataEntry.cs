using System.Text.Json;
using STS2RitsuLib.Utils.Persistence.Context;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Typed JSON persistence wrapper with optional migrations, backup recovery, and change
    ///         notifications.
    ///     </para>
    ///     <para xml:lang="zh-CN">支持可选迁移、备份恢复和变更通知的强类型 JSON 持久化封装。</para>
    /// </summary>
    public class PersistentDataEntry<T> where T : class, new()
    {
        private readonly bool _autoCreateIfMissing;
        private readonly Func<StorageContext>? _contextProvider;
        private readonly T _defaultValues;
        private readonly string _fileName;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly MigrationManager _migrationManager;
        private readonly string _modId;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Configures the persistent entry and initializes its in-memory data with a deep copy of
        ///         <paramref name="defaultValues" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">配置持久化条目，并以 <paramref name="defaultValues" /> 的深层副本初始化其内存数据。</para>
        /// </summary>
        public PersistentDataEntry(
            string modId,
            string fileName,
            SaveScope scope,
            T defaultValues,
            JsonSerializerOptions jsonOptions,
            MigrationManager migrationManager,
            bool autoCreateIfMissing = false,
            Func<StorageContext>? contextProvider = null)
        {
            _modId = modId;
            _fileName = fileName;
            Scope = scope;
            _defaultValues = defaultValues;
            _jsonOptions = jsonOptions;
            _migrationManager = migrationManager;
            _autoCreateIfMissing = autoCreateIfMissing;
            _contextProvider = contextProvider;
            Data = DeepClone(defaultValues);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Current deserialized data object; mutate it through <see cref="Modify" /> to raise change
        ///         notifications.
        ///     </para>
        ///     <para xml:lang="zh-CN">当前反序列化的数据对象；请通过 <see cref="Modify" /> 修改它以触发变更通知。</para>
        /// </summary>
        public T Data { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolved Godot user-data path for this entry, using the active profile unless its context
        ///         supplies another profile ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">此条目解析后的 Godot 用户数据路径；除非上下文提供其他档案 ID，否则使用活动档案。</para>
        /// </summary>
        public string FilePath =>
            StoragePathResolver.ResolveFilePathUser(_modId, _fileName, Scope, _contextProvider?.Invoke());

        /// <summary>
        ///     <para xml:lang="en">Configured storage scope for this entry.</para>
        ///     <para xml:lang="zh-CN">此条目配置的存储作用域。</para>
        /// </summary>
        public SaveScope Scope { get; }

        /// <summary>
        ///     <para xml:lang="en">Raised after a load attempt or an in-memory modification through <see cref="Modify" />.</para>
        ///     <para xml:lang="zh-CN">尝试加载后，或通过 <see cref="Modify" /> 修改内存数据后触发。</para>
        /// </summary>
        public event Action? Changed;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reads JSON from disk (with backup fallback), applies migrations, and updates
        ///         <see cref="Data" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">从磁盘读取 JSON（带备份回退），应用迁移，并更新 <see cref="Data" />。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if data was loaded successfully; <see langword="false" /> if defaults
        ///         were used because the file was missing or invalid.
        ///     </para>
        ///     <para xml:lang="zh-CN">成功加载数据时为 <see langword="true" />；因文件缺失或无效而使用默认值时为 <see langword="false" />。</para>
        /// </returns>
        public bool Load()
        {
            var currentPath = FilePath;
            RitsuLibFramework.Logger.Debug($"[Persistence] [{_fileName}] Loading from: {currentPath}");

            var result = FileOperations.ReadTextWithBackupFallback(currentPath, _fileName);

            if (!result.Success || string.IsNullOrEmpty(result.Content))
            {
                RitsuLibFramework.Logger.Info(
                    $"[Persistence] [{_fileName}] Using default values: {result.ErrorMessage}");
                Data = DeepClone(_defaultValues);

                if (_autoCreateIfMissing && !FileOperations.FileExists(currentPath))
                    Save();

                Changed?.Invoke();
                return false;
            }

            var migrationResult = _migrationManager.Migrate<T>(result.Content, _jsonOptions);

            if (!migrationResult.Success)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Persistence] [{_fileName}] Migration failed: {migrationResult.ErrorMessage}");

                if (migrationResult.RequiresRecovery)
                    MarkCorrupt(currentPath);

                Data = DeepClone(_defaultValues);
                Changed?.Invoke();
                return false;
            }

            Data = migrationResult.Data!;

            if (migrationResult.WasMigrated)
            {
                RitsuLibFramework.Logger.Info(
                    $"[Persistence] [{_fileName}] Data migrated to version {migrationResult.FinalVersion}");
                Save();
            }

            if (result.LoadedFromBackup)
                Save();

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Serializes <see cref="Data" /> to <see cref="FilePath" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="Data" /> 序列化到 <see cref="FilePath" />。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the file was written successfully; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">文件写入成功时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool Save()
        {
            return SaveTo(FilePath);
        }

        /// <summary>
        ///     <para xml:lang="en">Serializes <see cref="Data" /> to an explicit path, such as for an export or test.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="Data" /> 序列化到显式指定的路径，例如用于导出或测试。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the file was written successfully; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">文件写入成功时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool SaveTo(string path)
        {
            try
            {
                RitsuLibFramework.Logger.Debug($"[Persistence] [{_fileName}] Saving to: {path}");
                var json = JsonSerializer.Serialize(Data, _jsonOptions);
                var result = FileOperations.WriteText(path, json, _fileName);
                if (result.Success)
                    ModDataCloudMirror.MirrorLocalFileAfterWriteIfEnabled(path);

                return result.Success;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[Persistence] [{_fileName}] Save to '{path}' failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Applies an in-place mutation to <see cref="Data" /> and raises <see cref="Changed" />.</para>
        ///     <para xml:lang="zh-CN">对 <see cref="Data" /> 应用原地修改，并触发 <see cref="Changed" />。</para>
        /// </summary>
        public void Modify(Action<T> modifier)
        {
            modifier(Data);
            Changed?.Invoke();
        }

        private void MarkCorrupt(string path)
        {
            try
            {
                var corruptPath = path + ".corrupt";
                FileOperations.RenameFile(path, corruptPath, _fileName);
                RitsuLibFramework.Logger.Warn($"[Persistence] [{_fileName}] Corrupt file renamed to {corruptPath}");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[Persistence] [{_fileName}] Failed to mark corrupt: {ex.Message}");
            }
        }

        private T DeepClone(T source)
        {
            try
            {
                var json = JsonSerializer.Serialize(source, _jsonOptions);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
            }
            catch
            {
                return new();
            }
        }
    }
}
