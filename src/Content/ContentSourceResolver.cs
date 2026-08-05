using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves stable content-source metadata independently of content-source hover-tip preferences.
    ///     </para>
    ///     <para xml:lang="zh-CN">独立于内容来源悬浮提示偏好，解析稳定的内容来源元数据。</para>
    /// </summary>
    public static class ContentSourceResolver
    {
        /// <summary>
        ///     <para xml:lang="en">Resolves the source of a model, including a model-supplied override when present.</para>
        ///     <para xml:lang="zh-CN">解析模型的来源；模型提供来源覆盖时优先使用该覆盖。</para>
        /// </summary>
        /// <param name="model">
        ///     <para xml:lang="en">The model to inspect.</para>
        ///     <para xml:lang="zh-CN">要检查的模型。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         A normalized descriptor. Unresolved sources use <c>Unknown</c> as both the ID and display name.
        ///     </para>
        ///     <para xml:lang="zh-CN">规范化后的描述；无法解析的来源会使用 <c>Unknown</c> 作为 ID 和显示名称。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="model" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="model" /> 为 null 时抛出。</para>
        /// </exception>
        public static ContentSourceDescriptor Resolve(AbstractModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            var source = model is IContentSourceSupplier supplier
                ? ContentSourceHoverTipFactory.Resolve(supplier)
                : ContentSourceHoverTipFactory.Resolve(model.GetType());
            return new(source.Id, source.DisplayName);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves the source associated with an <see cref="AbstractModel" /> type.</para>
        ///     <para xml:lang="zh-CN">解析与 <see cref="AbstractModel" /> 类型关联的来源。</para>
        /// </summary>
        /// <param name="modelType">
        ///     <para xml:lang="en">A concrete or abstract model type to inspect.</para>
        ///     <para xml:lang="zh-CN">要检查的具体或抽象模型类型。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         A normalized descriptor. Unresolved sources use <c>Unknown</c> as both the ID and display name.
        ///     </para>
        ///     <para xml:lang="zh-CN">规范化后的描述；无法解析的来源会使用 <c>Unknown</c> 作为 ID 和显示名称。</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="modelType" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="modelType" /> 为 null 时抛出。</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">Thrown when <paramref name="modelType" /> is not an <see cref="AbstractModel" /> type.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="modelType" /> 不是 <see cref="AbstractModel" /> 类型时抛出。</para>
        /// </exception>
        public static ContentSourceDescriptor Resolve(Type modelType)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            if (!typeof(AbstractModel).IsAssignableFrom(modelType))
                throw new ArgumentException("The type must derive from AbstractModel.", nameof(modelType));

            var source = ContentSourceHoverTipFactory.Resolve(modelType);
            return new(source.Id, source.DisplayName);
        }
    }
}
