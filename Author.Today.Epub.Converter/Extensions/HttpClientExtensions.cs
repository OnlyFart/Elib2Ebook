using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Author.Today.Epub.Converter.Extensions {
    public static class HttpClientExtensions {
        private const int MAX_TRY_COUNT = 3;
        
        public static async Task<HttpResponseMessage> GetStringWithTriesAsync(this HttpClient client, Uri url) {
            for (var i = 0; i < MAX_TRY_COUNT; i++) {
                try { 
                    var response = await client.GetAsync(url);

                    if (response.StatusCode != HttpStatusCode.OK) {
                        continue;
                    }

                    return response;
                } catch (Exception) {
                    await Task.Delay(i * 3000);
                }
            }

            return default;
        }
    }
}
