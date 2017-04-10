using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor
{
    internal sealed class TypeSelector : ITypeSelector, ISelector
    {
        private List<ISelector> Selectors { get; } = new List<ISelector>();

        public IServiceTypeSelector AddTypes(params Type[] types)
        {
            return AddSelector(types);
        }

        public IServiceTypeSelector AddTypes(IEnumerable<Type> types)
        {
            return AddSelector(types);
        }

        void ISelector.Populate(IServiceCollection services, RegistrationStrategy registrationStrategy)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            foreach (var selector in Selectors)
            {
                selector.Populate(services, registrationStrategy);
            }
        }

        private IServiceTypeSelector AddSelector(IEnumerable<Type> types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var selector = new ServiceTypeSelector(types);

            Selectors.Add(selector);

            return selector;
        }
    }
}
