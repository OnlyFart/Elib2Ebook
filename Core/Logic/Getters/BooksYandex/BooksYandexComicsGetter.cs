using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.BookYandex;

namespace Core.Logic.Getters.BooksYandex;

public class BooksYandexComicsGetter(BookGetterConfig config) : BooksYandexGetterBase(config) {
    protected override string[] Paths => ["comicbooks"];

    protected override async Task<IEnumerable<Chapter>> FillChapters(Book book, BooksYandexResponse response) {
        if (Config.Options.NoChapters || string.IsNullOrWhiteSpace(response.Comicbook?.UUID)) {
            return [];
        }
        
        var metadata = await Config.Client.GetFromJsonAsync<BooksYandexComicMetadata>($"https://api.bookmate.ru/api/v5/comicbooks/{response.Comicbook.UUID}/metadata.json");
        
        var sb = new StringBuilder();
        
        foreach (var page in metadata.Pages) {
            sb.Append($"<img src=\"{page.Content.Uri.Image}\" />");
        }

        var doc = sb.AsHtmlDoc();
        var chapter = new Chapter {
            Images = await GetImages(doc, SystemUrl),
            Content = doc.DocumentNode.InnerHtml,
            Title = response.Comicbook.Title
        };

        return [chapter];
    }
}