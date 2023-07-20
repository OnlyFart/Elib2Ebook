using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;

namespace Elib2Ebook.Logic.Builders; 

public class TxtBuilder : BuilderBase {
    private IEnumerable<Chapter> _chapters;
    
    public static BuilderBase Create() {
        return new TxtBuilder();
    }

    public override BuilderBase AddAuthor(Author author) {
        return this;
    }

    public override BuilderBase AddCoAuthors(IEnumerable<Author> coAuthors) {
        return this;
    }

    public override BuilderBase WithTitle(string title) {
        return this;
    }

    public override BuilderBase WithCover(Image cover) {
        return this;
    }

    public override BuilderBase WithBookUrl(Uri url) {
        return this;
    }

    public override BuilderBase WithAnnotation(string annotation) {
        return this;
    }

    public override BuilderBase WithFiles(string directory, string searchPattern) {
        return this;
    }

    public override BuilderBase WithChapters(IEnumerable<Chapter> chapters) {
        _chapters = chapters;
        return this;
    }

    public override BuilderBase WithSeria(Seria seria) {
        return this;
    }

    public override BuilderBase WithLang(string lang) {
        return this;
    }

    protected override async Task BuildInternal(string name) {
        if (File.Exists(name)) {
            File.Delete(name);
        }
        
        await using var file = File.CreateText(name);
        
        foreach (var chapter in _chapters.Where(c => c.IsValid)) {
            await file.WriteLineAsync("   " + chapter.Title);
            await file.WriteLineAsync();

            var prettyText = chapter.Content.PrettyHtml().AsHtmlDoc().DocumentNode.GetText();
            using var sr = new StringReader(prettyText); 
            
            while (true) {
                var line = await sr.ReadLineAsync();
                if (line == null) {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                
                await file.WriteLineAsync("   " + line.Trim());
            }
            
            await file.WriteLineAsync();
        }
    }

    protected override string GetFileName(string name) {
        return $"{name}.txt".RemoveInvalidChars();
    }
}