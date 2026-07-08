using System.Reflection;
using System.Runtime.Loader;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Getters;
using Microsoft.Extensions.DependencyModel;

namespace Elib2Ebook.DomainServices.Misc;

public static class GetterFactory
{
    private static IReadOnlySet<Assembly> GetAssemblies()
    {
        var result = AppDomain.CurrentDomain.GetAssemblies().ToHashSet();
        var entry = Assembly.GetEntryAssembly();
        if (entry == null)
        {
            return result;
        }

        var dependencyContext = DependencyContext.Load(entry);

        const string names = "Elib2Ebook.ExternalServices";
        var assemblyNames =
            dependencyContext != null ?
                dependencyContext.RuntimeLibraries
                    .Where(a => a.Name.StartsWith(names))
                    .Select(l => new AssemblyName(l.Name)) :
                entry.GetReferencedAssemblies()
                    .Where(a => a.Name != null && a.Name.StartsWith(names));

        var alc = AssemblyLoadContext.Default;
        foreach (var assemblyName in assemblyNames)
        {
            try
            {
                result.Add(alc.LoadFromAssemblyName(assemblyName));
            }
            catch
            {
                // игнорируем
            }
        }

        return result;
    }

    private static IEnumerable<Type> GetGetterTypesFromAssembly(Assembly asm)
    {
        try
        {
            return asm.GetTypes()
                .Where(myType => myType is { IsClass: true, IsAbstract: false } && myType.IsSubclassOf(typeof(GetterBase)));
        }
        catch
        {
            return [];
        }
    }

    public static GetterBase Get(BookGetterConfig config, Uri url)
    {
        return GetAssemblies()
                   .SelectMany(GetGetterTypesFromAssembly)
                   .Select(type => (GetterBase)Activator.CreateInstance(type, config))
                   .FirstOrDefault(g => g!.IsSameUrl(url)) ??
               throw new ArgumentException($"Сайт {url.Host} не поддерживается");
    }
}
