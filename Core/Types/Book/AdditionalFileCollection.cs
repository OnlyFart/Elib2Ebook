using System.Collections.Generic;
using Core.Misc;
using Core.Types.Common;

namespace Core.Types.Book;

public class AdditionalFileCollection {
    public Dictionary<AdditionalTypeEnum, List<TempFile>> Collection { get; set; } = new();

    public void Add(AdditionalTypeEnum type, TempFile file) {
        if (!Collection.TryGetValue(type, out var files)) {
            files = new();
            Collection[type] = files;
        }
        
        files.Add(file);
    }
    
    public void Add(AdditionalTypeEnum type, IEnumerable<TempFile> files) {
        foreach (var file in files) {
            Add(type, file);
        }
    }

    public List<TempFile> Get(AdditionalTypeEnum type) {
        return Collection.TryGetValue(type, out var result) ? result : [];
    } 
}