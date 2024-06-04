using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;

namespace Elib2Ebook.Logic.Builders; 

public class TxtBuilder : BuilderBase {
    public TxtBuilder(Options options) : base(options) { }

    protected override string Extension => "txt";

    protected override async Task BuildInternal(Book book, string fileName) {
        if (File.Exists(fileName)) {
            File.Delete(fileName);
        }
        
        await using var file = File.CreateText(fileName);
        
        foreach (var chapter in book.Chapters.Where(c => c.IsValid)) {
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
}