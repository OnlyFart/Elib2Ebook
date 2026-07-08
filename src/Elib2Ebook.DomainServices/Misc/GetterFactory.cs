using System.Reflection;
using Elib2Ebook.DomainServices.Configs;
using Elib2Ebook.DomainServices.Getters;

namespace Elib2Ebook.DomainServices.Misc;

public static class GetterFactory
{
    private static IEnumerable<Type> GetGetterTypes()
    {
        LoadAllAssemblies();

        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch
                {
                    return [];
                }
            })
            .Where(myType => myType is { IsClass: true, IsAbstract: false } && myType.IsSubclassOf(typeof(GetterBase)));
    }

    private static void LoadAllAssemblies()
    {
        var loadedAssemblyPaths = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.Location)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        foreach (var dllPath in Directory.EnumerateFiles(baseDir, "Elib2Ebook.ExternalServices.*.dll", SearchOption.AllDirectories))
        {
            if (loadedAssemblyPaths.Contains(dllPath))
                continue;

            try
            {
                Assembly.LoadFrom(dllPath);
            }
            catch
            {
                // ignore assemblies that fail to load
            }
        }
    }

    public static GetterBase Get(BookGetterConfig config, Uri url)
    {
        return GetGetterTypes()
                   .Select(type => (GetterBase)Activator.CreateInstance(type, config))
                   .FirstOrDefault(g => g!.IsSameUrl(url)) ??
               throw new ArgumentException($"Сайт {url.Host} не поддерживается");
    }
}
