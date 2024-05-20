using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.StrokiMts;

namespace Elib2Ebook.Logic.Getters;

public class StrokiMtsGetter : GetterBase {
    public StrokiMtsGetter(BookGetterConfig config) : base(config) { }

    protected override Uri SystemUrl => new("https://stroki.mts.ru/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2).Split("-").Last();
    }

    public override Task Authorize() {
        Config.Client.DefaultRequestHeaders.Add("access-token", Config.Options.Login ?? Config.Options.Password);
        
        return Task.CompletedTask;
    }

    public override Task Init() {
        Config.Client.DefaultRequestHeaders.Add("install-guid", Guid.NewGuid().ToString());
        Config.Client.DefaultRequestHeaders.Add("app-version", "5.0");
        Config.Client.DefaultRequestHeaders.Add("language", "ru");
        Config.Client.DefaultRequestHeaders.Add("platform", "ios");
        Config.Client.DefaultRequestHeaders.Add("api-version", "5.0");
        
        return Task.CompletedTask;
    }

    public override async Task<Book> Get(Uri url) {
        var id = GetId(url);
        var fileMeta = await GetFileMeta(id);
        var fileUrl = await GetFileUrl(fileMeta);

        var file = await Config.Client.GetByteArrayAsync(fileUrl.Url);
        await File.WriteAllBytesAsync("dddd.epub", file);
        
        return default;
    }

    private async Task<StrokiMtsFile> GetFileMeta(string id) {
        var response = await Config.Client.SendAsync(GetMessage(SystemUrl.MakeRelativeUri("/api/books/files").AppendQueryParameter("bookId", id)));
        var json = await response.Content.ReadFromJsonAsync<StrokiMtsApiResponse<StrokiMtsFiles>>();
        return json.Data.Full?.FirstOrDefault() ?? json.Data.Preview;
    }
    
    private async Task<StrokiMtsFileUrl> GetFileUrl(StrokiMtsFile file) {
        var response = await Config.Client.SendAsync(GetMessage(SystemUrl.MakeRelativeUri($"api/books/files/data/link/{file.FileId}")));
        var json = await response.Content.ReadFromJsonAsync<StrokiMtsApiResponse<StrokiMtsFileUrl>>();
        return json.Data;
    }
    
    protected virtual HttpRequestMessage GetMessage(Uri uri) {
        var message = new HttpRequestMessage(HttpMethod.Get, uri);

        foreach (var header in Config.Client.DefaultRequestHeaders) {
            message.Headers.Add(header.Key, header.Value);
        }
        
        message.Headers.Add("signature", GetSignature(uri));
        
        return message;
    }

    private static string GetSignature(Uri url) {
        var inputBytes = Encoding.UTF8.GetBytes(url.ToString().Replace("https://", "http://") + "meg@$h!t");
        var hashBytes = MD5.HashData(inputBytes);

        return Convert.ToHexString(hashBytes).ToLower();
    }
}