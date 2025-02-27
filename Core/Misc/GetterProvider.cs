using System;
using System.Linq;
using System.Reflection;
using Core.Configs;
using Core.Logic.Getters;

namespace Core.Misc;

public static class GetterProvider {
    public static GetterBase Get(BookGetterConfig config, Uri url) {
        return Assembly.GetAssembly(typeof(GetterBase))!.GetTypes()
                   .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(GetterBase)))
                   .Select(type => (GetterBase) Activator.CreateInstance(type, config))
                   .FirstOrDefault(g => g!.IsSameUrl(url)) ??
               throw new ArgumentException($"Сайт {url.Host} не поддерживается");
    }
    
    public static bool IsLibSocial( GetterBase builder ) {
        return builder is Core.Logic.Getters.LibSocial.NewLibSocialGetterBase;
    }
}