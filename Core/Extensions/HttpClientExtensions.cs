using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Core.Extensions; 

public static class HttpClientExtensions {
    private const int MAX_TRY_COUNT = 5;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

    private static TimeSpan GetTimeout(TimeSpan errorTimeout) {
        return errorTimeout == default ? DefaultTimeout : errorTimeout;
    }

    public static async Task<HttpResponseMessage> GetWithTriesAsync(this HttpClient client, Uri url, TimeSpan errorTimeout = default) {
        Exception lastEx = null;
        
        for (var i = 0; i < MAX_TRY_COUNT; i++) {
            try {
                var response = await client.GetAsync(url);

                if (response.StatusCode != HttpStatusCode.OK) {
                    if (i == MAX_TRY_COUNT - 1) {
                        return response;
                    }

                    response.Dispose();
                    await Task.Delay(GetTimeout(errorTimeout));
                    continue;
                }

                return response;
            } catch (Exception ex) {
                lastEx = ex;
                await Task.Delay(GetTimeout(errorTimeout));
            }
        }

        if (lastEx != null) {
            throw lastEx;
        }

        return default;
    }

    public static async Task<HttpResponseMessage> SendWithTriesAsync(this HttpClient client, Func<HttpRequestMessage> message, TimeSpan errorTimeout = default) {
        Exception lastEx = null;
        
        for (var i = 0; i < MAX_TRY_COUNT; i++) {
            try {
                var response = await client.SendAsync(message());

                if (response.StatusCode != HttpStatusCode.OK) {
                    if (i == MAX_TRY_COUNT - 1) {
                        return response;
                    }

                    response.Dispose();
                    await Task.Delay(GetTimeout(errorTimeout));
                    continue;
                }

                return response;
            } catch (Exception ex) {
                lastEx = ex;
                await Task.Delay(GetTimeout(errorTimeout));
            }
        }

        if (lastEx != null) {
            throw lastEx;
        }

        return default;
    }
        
    public static async Task<HttpResponseMessage> PostWithTriesAsync(this HttpClient client, Uri url, HttpContent content, TimeSpan errorTimeout = default) {
        Exception lastEx = null;
        
        for (var i = 0; i < MAX_TRY_COUNT; i++) {
            try { 
                var response = await client.PostAsync(url, content);

                if (response.StatusCode != HttpStatusCode.OK) {
                    await Task.Delay(GetTimeout(errorTimeout));
                    if (i == MAX_TRY_COUNT - 1) {
                        return response;
                    }
                        
                    continue;
                }
                    
                return response;
            } catch (Exception ex) {
                lastEx = ex;
                await Task.Delay(GetTimeout(errorTimeout));
            }
        }

        if (lastEx != null) {
            throw lastEx;
        }

        return default;
    }
        
    public static async Task<HtmlDocument> GetHtmlDocWithTriesAsync(this HttpClient client, Uri url, Encoding encoding = null) {
        using var response = await client.GetWithTriesAsync(url); 
        return await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc(encoding));
    }
    
    public static async Task<T> GetFromJsonWithTriesAsync<T>(this HttpClient client, Uri url, TimeSpan errorTimeout = default) {
        using var response = await client.GetWithTriesAsync(url, errorTimeout);
        if (response == default) {
            throw new Exception($"Не удалось получить ответ от {url}");
        }

        if (response.StatusCode != HttpStatusCode.OK) {
            var errorBody = await SafeReadAsStringAsync(response.Content);
            throw new Exception($"Ошибка запроса {url}. Код {(int)response.StatusCode} ({response.StatusCode}).{FormatSnippet(errorBody)}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var payload = Encoding.UTF8.GetString(memoryStream.ToArray());
        if (string.IsNullOrWhiteSpace(payload)) {
            throw new Exception($"Ответ {url} пустой");
        }

        if (LooksLikeHtml(payload)) {
            throw new Exception($"Сайт вернул HTML вместо JSON по адресу {url}. Возможно сработала защита (DDoS-Guard). Попробуйте увеличить параметры --delay/--timeout или настроить --flare.");
        }

        try {
            return JsonSerializer.Deserialize<T>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new Exception($"Сайт вернул пустой JSON по адресу {url}");
        } catch (JsonException ex) {
            throw new Exception($"Не удалось разобрать JSON ответа {url}. {ex.Message}. {FormatSnippet(payload)}", ex);
        }
    }

    public static async Task<HtmlDocument> PostHtmlDocWithTriesAsync(this HttpClient client, Uri url, HttpContent content, Encoding encoding = null) {
        using var response = await client.PostWithTriesAsync(url, content);
        return await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc(encoding));
    }

    private static async Task<string> SafeReadAsStringAsync(HttpContent content) {
        try {
            return await content.ReadAsStringAsync();
        } catch {
            return string.Empty;
        }
    }

    private static bool LooksLikeHtml(string value) {
        return value.TrimStart().StartsWith("<", StringComparison.Ordinal);
    }

    private static string FormatSnippet(string value, int maxLength = 300) {
        if (string.IsNullOrWhiteSpace(value)) {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (LooksLikeHtml(trimmed)) {
            return string.Empty;
        }
        if (trimmed.Length > maxLength) {
            trimmed = trimmed[..maxLength] + "…";
        }

        return $" Ответ сервера: {trimmed}";
    }
}
