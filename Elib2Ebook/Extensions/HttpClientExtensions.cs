using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Elib2Ebook.Extensions; 

public static class HttpClientExtensions {
    private const int MAX_TRY_COUNT = 5;
        
    public static async Task<HttpResponseMessage> GetWithTriesAsync(this HttpClient client, Uri url) {
        for (var i = 0; i < MAX_TRY_COUNT; i++) {
            try { 
                var response = await client.GetAsync(url);

                if (response.StatusCode != HttpStatusCode.OK) {
                    Console.WriteLine(response.StatusCode);
                    await Task.Delay(i * 500);
                    continue;
                }

                return response;
            } catch (Exception ex) {
                Console.WriteLine(ex);
                await Task.Delay(i * 1000);
            }
        }

        return default;
    }

    public static async Task<HttpResponseMessage> SendWithTriesAsync(this HttpClient client, HttpRequestMessage message) {
        for (var i = 0; i < MAX_TRY_COUNT; i++) {
            try { 
                var response = await client.SendAsync(message);

                if (response.StatusCode != HttpStatusCode.OK) {
                    Console.WriteLine(response.StatusCode);
                    await Task.Delay(i * 500);
                    continue;
                }

                return response;
            } catch (Exception ex) {
                Console.WriteLine(ex);
                await Task.Delay(i * 1000);
            }
        }

        return default;
    }
        
    public static async Task<HttpResponseMessage> PostWithTriesAsync(this HttpClient client, Uri url, HttpContent content) {
        for (var i = 0; i < MAX_TRY_COUNT; i++) {
            try { 
                var response = await client.PostAsync(url, content);

                if (response.StatusCode != HttpStatusCode.OK) {
                    await Task.Delay(100);
                    Console.WriteLine(response.StatusCode);
                    if (i == MAX_TRY_COUNT - 1) {
                        return response;
                    }
                        
                    continue;
                }
                    
                return response;
            } catch (Exception ex) {
                Console.WriteLine(ex);
                await Task.Delay(i * 3000);
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