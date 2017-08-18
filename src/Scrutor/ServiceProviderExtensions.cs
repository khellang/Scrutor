using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrutor
{
    public static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider provider)
        {
            return (T)provider.GetService(typeof(T));
        }
    }
}
