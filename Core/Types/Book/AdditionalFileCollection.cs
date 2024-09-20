using System;
using System.Collections.Generic;
using System.Linq;
using Core.Misc;
using Core.Types.Common;

namespace Core.Types.Book;

public class AdditionalFileCollection : IDisposable {
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

    public void Dispose() {
        foreach (var file in Collection.SelectMany(pair => pair.Value)) {
            file.Dispose();
        }
    }
}