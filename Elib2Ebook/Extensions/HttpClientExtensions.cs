using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Elib2Ebook.Extensions; 

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
            } catch (Exception ex) {
                Console.WriteLine(ex);
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
            } catch (Exception ex) {
                Console.WriteLine(ex);
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
            } catch (Exception ex) {
                Console.WriteLine(ex);
                await Task.Delay(GetTimeout(errorTimeout));
            }
        }

        return default;
    }
        
    public static async Task<HtmlDocument> GetHtmlDocWithTriesAsync(this HttpClient client, Uri url) {
        var response = await client.GetWithTriesAsync(url);
        var content = await response.Content.ReadAsStringAsync();
            
        return content.AsHtmlDoc();
    }
    
    public static async Task<HtmlDocument> PostHtmlDocWithTriesAsync(this HttpClient client, Uri url, HttpContent content) {
        var response = await client.PostWithTriesAsync(url, content);
        var data = await response.Content.ReadAsStringAsync();
            
        return data.AsHtmlDoc();
    }
}