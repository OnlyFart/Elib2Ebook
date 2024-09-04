using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
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
        for (var i = 0; i < MAX_TRY_COUNT; i++) {
            try {
                var response = await client.GetAsync(url);

                if (response.StatusCode != HttpStatusCode.OK) {
                    await Task.Delay(GetTimeout(errorTimeout));
                    continue;
                }

                return response;
            } catch (Exception) {
                await Task.Delay(GetTimeout(errorTimeout));
            }
        }

        return default;
    }

    public static async Task<HttpResponseMessage> SendWithTriesAsync(this HttpClient client, Func<HttpRequestMessage> message, TimeSpan errorTimeout = default) {
        for (var i = 0; i < MAX_TRY_COUNT; i++) {
            try { 
                var response = await client.SendAsync(message());

                if (response.StatusCode != HttpStatusCode.OK) {
                    await Task.Delay(GetTimeout(errorTimeout));
                    continue;
                }

                return response;
            } catch (Exception) {
                await Task.Delay(GetTimeout(errorTimeout));
            }
        }

        return default;
    }
        
    public static async Task<HttpResponseMessage> PostWithTriesAsync(this HttpClient client, Uri url, HttpContent content, TimeSpan errorTimeout = default) {
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
            } catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
                await Task.Delay(GetTimeout(errorTimeout));
            } catch (Exception) {
                
                await Task.Delay(GetTimeout(errorTimeout));
            }
        }

        return default;
    }
        
    public static async Task<HtmlDocument> GetHtmlDocWithTriesAsync(this HttpClient client, Uri url, Encoding encoding = null) {
        using var response = await client.GetWithTriesAsync(url); 
        return await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc(encoding));
    }
    
    public static async Task<T> GetFromJsonWithTriesAsync<T>(this HttpClient client, Uri url, TimeSpan errorTimeout = default) {
        using var response = await client.GetWithTriesAsync(url, errorTimeout);
        return await response.Content.ReadFromJsonAsync<T>();
    }
    
    public static async Task<HtmlDocument> PostHtmlDocWithTriesAsync(this HttpClient client, Uri url, HttpContent content, Encoding encoding = null) {
        using var response = await client.PostWithTriesAsync(url, content);
        return await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc(encoding));
    }
}