using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;

namespace Elib2Ebook.Logic.Builders; 

public class JsonLiteBuilder : BuilderBase {
    private readonly Book _book = new(null);
    
    public static BuilderBase Create() {
        return new JsonLiteBuilder();
    }
    
    public override BuilderBase AddAuthor(Author author) {
        _book.Author = author;
        return this;
    }

    public override BuilderBase WithTitle(string title) {
        _book.Title = title;
        return this;
    }

    public override BuilderBase WithCover(Image cover) {
        _book.Cover = cover;
        return this;
    }

    public override BuilderBase WithBookUrl(Uri url) {
        _book.Url = url;
        return this;
    }

    public override BuilderBase WithAnnotation(string annotation) {
        _book.Annotation = annotation;
        return this;
    }

    public override BuilderBase WithFiles(string directory, string searchPattern) {
        return this;
    }

    public override BuilderBase WithChapters(IEnumerable<Chapter> chapters) {
        var lite_chapters = new List<Chapter>();
        foreach (var chapter in chapters) {
            var content = "";
            var images = new List<Image>();
            if( chapter.IsValid )
            {
                content = "*";
            }
            var lite_chapter = new Chapter {};
            lite_chapter.Title = chapter.Title;
            lite_chapter.Content = content;
            lite_chapter.Images = images;
            lite_chapters.Add(lite_chapter);
        }
        _book.Chapters = lite_chapters;
        return this;
    }

    public override BuilderBase WithSeria(Seria seria) {
        _book.Seria = seria;
        return this;
    }

    public override BuilderBase WithLang(string lang) {
        _book.Lang = lang;
        return this;
    }

    protected override async Task BuildInternal(string name) {
        await using var file = File.Create(name);
        var jsonSerializerOptions = new JsonSerializerOptions {
            WriteIndented = true
        };
        
        await JsonSerializer.SerializeAsync(file, _book, jsonSerializerOptions);
    }

    protected override string GetFileName(string name) {
        return $"{name}.json".RemoveInvalidChars();
    }
}