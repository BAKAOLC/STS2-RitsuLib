namespace STS2RitsuLib.Settings.RunSidecar
{
    /// <summary>
    ///     Result of validating and reading a run sidecar JSON file.
    ///     校验并读取跑局 sidecar JSON 文件的结果。
    /// </summary>
    public enum ModRunSidecarReadStatus
    {
        /// <summary>
        ///     Model loaded and fingerprint matched the active run.
        ///     模型已加载，且指纹与活动跑局匹配。
        /// </summary>
        Ok,

        /// <summary>
        ///     No run in progress (main menu / post-run).
        ///     没有进行中的跑局（主菜单 / 跑局后）。
        /// </summary>
        NoActiveRun,

        /// <summary>
        ///     Sidecar file does not exist yet.
        ///     sidecar 文件尚不存在。
        /// </summary>
        MissingFile,

        /// <summary>
        ///     JSON could not be parsed.
        ///     JSON 无法解析。
        /// </summary>
        InvalidJson,

        /// <summary>
        ///     Stored fingerprint does not match the active run — data is ignored to avoid wrong-run binding.
        ///     存储的指纹与活动跑局不匹配；忽略数据以避免绑定到错误跑局。
        /// </summary>
        FingerprintMismatch,
    }
}
