using System;

namespace Scrutor
{
    public interface IImplementationTypeSelector : IAssemblySelector
    {
        /// <summary>
        /// Adds all public, non-abstract classes from the selected assemblies that are annotated with
        /// the <see cref="ServiceDescriptorAttribute"/> to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
        /// </summary>
        [Obsolete("This method is obsolete and will be removed in the next major release. Use AddClasses().UsingAttributes() instead.")]
        void AddFromAttributes();

        /// <summary>
        /// Adds all non-abstract classes from the selected assemblies that are annotated with
        /// the <see cref="ServiceDescriptorAttribute"/> to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
        /// </summary>
        /// <param name="publicOnly">Specifies whether too look at public types only.</param>
        [Obsolete("This method is obsolete and will be removed in the next major release. Use AddClasses(publicOnly).UsingAttributes() instead.")]
        void AddFromAttributes(bool publicOnly);

        /// <summary>
        /// Adds all public, non-abstract classes from the selected assemblies that are annotated with
        /// the <see cref="ServiceDescriptorAttribute"/> and that matches the requirements specified
        /// in the <paramref name="action"/> to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
        /// </summary>
        /// <param name="action">The filtering action.</param>
        [Obsolete("This method is obsolete and will be removed in the next major release. Use AddClasses(action).UsingAttributes() instead.")]
        void AddFromAttributes(Action<IImplementationTypeFilter> action);

        /// <summary>
        /// Adds all non-abstract classes from the selected assemblies that are annotated with
        /// the <see cref="ServiceDescriptorAttribute"/> and that matches the requirements specified
        /// in the <paramref name="action"/> to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
        /// </summary>
        /// <param name="action">The filtering action.</param>
        /// <param name="publicOnly">Specifies whether too look at public types only.</param>
        [Obsolete("This method is obsolete and will be removed in the next major release. Use AddClasses(action, publicOnly).UsingAttributes() instead.")]
        void AddFromAttributes(Action<IImplementationTypeFilter> action, bool publicOnly);

        /// <summary>
        /// Adds all public, non-abstract classes from the selected assemblies to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
        /// </summary>
        IServiceTypeSelector AddClasses();

        /// <summary>
        /// Adds all non-abstract classes from the selected assemblies to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
        /// </summary>
        /// <param name="publicOnly">Specifies whether too add public types only.</param>
        IServiceTypeSelector AddClasses(bool publicOnly);

        /// <summary>
        /// Adds all public, non-abstract classes from the selected assemblies that
        /// matches the requirements specified in the <paramref name="action"/>
        /// to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
        /// </summary>
        /// <param name="action">The filtering action.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="action"/> argument is <c>null</c>.</exception>
        IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action);

        /// <summary>
        /// Adds all non-abstract classes from the selected assemblies that
        /// matches the requirements specified in the <paramref name="action"/>
        /// to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
        /// </summary>
        /// <param name="action">The filtering action.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="action"/> argument is <c>null</c>.</exception>
        /// <param name="publicOnly">Specifies whether too add public types only.</param>
        IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action, bool publicOnly);
    }
}
