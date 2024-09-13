using System.IO;
using System.Threading.Tasks;
using Core.Configs;
using Core.Types.Book;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Builders;

public class OriginalBuilder : BuilderBase {
    public OriginalBuilder(Options options, ILogger logger) : base(options, logger) { }

    protected override string Extension => string.Empty;

    protected override string GetFileName(Book book) {
        return GetTitle(book);
    }

    protected override string GetTitle(Book book) {
        return book.OriginalFile.Name;
    }

    protected override bool PreCheck(Book book) {
        return book.OriginalFile != default && book.OriginalFile.Bytes.Length > 0;
    }

    protected override async Task BuildInternal(Book book, string fileName) {
        await File.WriteAllBytesAsync(fileName, book.OriginalFile.Bytes);
    }
}