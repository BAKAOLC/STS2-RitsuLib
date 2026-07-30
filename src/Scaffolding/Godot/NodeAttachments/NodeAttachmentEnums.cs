using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies how a registered attachment handles an existing direct child with the configured name.
    ///     </para>
    ///     <para xml:lang="zh-CN">指定已注册的附加项如何处理现有的同名直接子节点。</para>
    /// </summary>
    public enum NodeAttachmentDuplicatePolicy
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates and attaches the registered node even when another direct child already has the same name.
        ///     </para>
        ///     <para xml:lang="zh-CN">即使已有同名直接子节点，也仍然创建并附加已注册的节点。</para>
        /// </summary>
        AllowDuplicateName,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reuses the existing direct child when its type is compatible with the registered node type.
        ///     </para>
        ///     <para xml:lang="zh-CN">当现有直接子节点的类型与已注册节点类型兼容时复用该节点。</para>
        /// </summary>
        ReuseExistingByName,

        /// <summary>
        ///     <para xml:lang="en">Skips this attachment when a direct child with the configured name already exists.</para>
        ///     <para xml:lang="zh-CN">当已有指定名称的直接子节点时跳过本次附加。</para>
        /// </summary>
        SkipIfExistingByName,

        /// <summary>
        ///     <para xml:lang="en">Removes the existing direct child before creating the registered node.</para>
        ///     <para xml:lang="zh-CN">在创建已注册节点之前移除现有的直接子节点。</para>
        /// </summary>
        ReplaceExistingByName,

        /// <summary>
        ///     <para xml:lang="en">Throws when a direct child with the configured name already exists.</para>
        ///     <para xml:lang="zh-CN">当已有指定名称的直接子节点时抛出异常。</para>
        /// </summary>
        ThrowIfExistingByName,
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies how a newly created attachment node is added to its parent.</para>
    ///     <para xml:lang="zh-CN">指定如何将新建的附加节点添加到其父节点。</para>
    /// </summary>
    public enum NodeAttachmentAddMode
    {
        /// <summary>
        ///     <para xml:lang="en">Uses <see cref="RitsuGodotTreeCompat.AddChildSafely" />.</para>
        ///     <para xml:lang="zh-CN">使用 <see cref="RitsuGodotTreeCompat.AddChildSafely" />。</para>
        /// </summary>
        AddChildSafely,

        /// <summary>
        ///     <para xml:lang="en">Calls <see cref="Node.AddChild(Node, bool, Node.InternalMode)" /> immediately.</para>
        ///     <para xml:lang="zh-CN">立即调用 <see cref="Node.AddChild(Node, bool, Node.InternalMode)" />。</para>
        /// </summary>
        AddChildDirect,
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies when <see cref="NodeAttachmentDefinition.Setup" /> is invoked.</para>
    ///     <para xml:lang="zh-CN">指定调用 <see cref="NodeAttachmentDefinition.Setup" /> 的时机。</para>
    /// </summary>
    public enum NodeAttachmentSetupTiming
    {
        /// <summary>
        ///     <para xml:lang="en">Runs setup after creation and before adding the child to the parent.</para>
        ///     <para xml:lang="zh-CN">在创建子节点后、将其添加到父节点之前执行配置。</para>
        /// </summary>
        BeforeAdd,

        /// <summary>
        ///     <para xml:lang="en">Runs setup after the child has been added to the parent.</para>
        ///     <para xml:lang="zh-CN">在子节点添加到父节点之后执行配置。</para>
        /// </summary>
        AfterAdd,
    }
}
