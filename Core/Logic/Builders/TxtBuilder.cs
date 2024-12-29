using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Builders; 

public class TxtBuilder(Options options, ILogger logger) : BuilderBase(options, logger) {
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