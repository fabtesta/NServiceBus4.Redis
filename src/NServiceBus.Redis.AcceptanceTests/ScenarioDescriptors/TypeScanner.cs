using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Hosting.Helpers;

namespace NServiceBus.Redis.AcceptanceTests.ScenarioDescriptors
{
    public class TypeScanner
    {
        public static IEnumerable<Type> GetAllTypesAssignableTo<T>()
        {
            return AvailableAssemblies.SelectMany(a => a.GetTypes())
                .Where(t => typeof(T).IsAssignableFrom(t) && t != typeof(T))
                .ToList();
        }

        static IEnumerable<Assembly> AvailableAssemblies
        {
            get
            {
                if (assemblies == null)
                {
                    var result = new AssemblyScanner().GetScannableAssemblies();

                    assemblies = result.Assemblies;
                }

                return assemblies;
            }
        }

        static List<Assembly> assemblies;
    }
}
