using System.Collections.Generic;
using Core.Types.Common;

namespace Core.Types.Book;

public class AdditionalFileCollection {
    public const string BOOKS_KEY = "Books";
    public const string AUDIOS_KEY = "Audio";
    public const string IMAGES_KEY = "Images";
    
    public Dictionary<string, List<TempFile>> Collection { get; set; } = new();

    public void AddBook(TempFile file) => Add(BOOKS_KEY, file);

    public void AddBook(IEnumerable<TempFile> files) => Add(BOOKS_KEY, files);

    public void AddImage(TempFile file) => Add(IMAGES_KEY, file);

    public void AddImage(IEnumerable<TempFile> files) => Add(IMAGES_KEY, files);

    public void AddAudio(TempFile file) => Add(AUDIOS_KEY, file);

    public void AddAudio(IEnumerable<TempFile> files) => Add(AUDIOS_KEY, files);

    public List<TempFile> GetBooks() => Get(BOOKS_KEY);

    public List<TempFile> GetAudios() => Get(AUDIOS_KEY);

    private void Add(string directory, TempFile file) {
        if (!Collection.TryGetValue(directory, out var files)) {
            files = new();
            Collection[directory] = files;
        }
        
        files.Add(file);
    }
    
    private void Add(string directory, IEnumerable<TempFile> files) {
        foreach (var file in files) {
            Add(directory, file);
        }
    }

    private List<TempFile> Get(string key) {
        return Collection.TryGetValue(key, out var result) ? result : [];
    } 
}