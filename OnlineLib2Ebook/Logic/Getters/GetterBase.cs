using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;

namespace OnlineLib2Ebook.Logic.Getters {
    public abstract class GetterBase : IDisposable {
        protected readonly BookGetterConfig _config;

        protected GetterBase(BookGetterConfig config) {
            _config = config;
        }

        protected abstract Uri SystemUrl { get; }

        protected virtual string GetId(Uri url) {
            return url.Segments.Last();
        }

        public virtual bool IsSameUrl(Uri url) {
            return string.Equals(SystemUrl.Host.Replace("www.", ""), url.Host.Replace("www.", ""), StringComparison.InvariantCultureIgnoreCase);
        }
        
        /// <summary>
        /// Получение изображения
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected async Task<Image> GetImage(Uri uri) {
            Console.WriteLine($"Загружаем картинку {uri.ToString().CoverQuotes()}");
            try {
                using var response = await _config.Client.GetStringWithTriesAsync(uri);
                return response is { StatusCode: HttpStatusCode.OK } ? 
                    new Image(uri.GetFileName(), await response.Content.ReadAsByteArrayAsync()) : 
                    null;
            } catch (Exception) {
                return null;
            }
        }
        
        /// <summary>
        /// Загрузка изображений отдельной части
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="baseUri"></param>
        protected async Task<IEnumerable<Image>> GetImages(HtmlDocument doc, Uri baseUri) {
            var imgUrls = doc.DocumentNode
                .Descendants()
                .Where(t => t.Name == "img");

            var images = new List<Image>();
            foreach (var img in imgUrls) {
                var path = img.Attributes["src"]?.Value;
                if (string.IsNullOrWhiteSpace(path)) {
                    continue;
                }

                if (!Uri.TryCreate(baseUri, path, out var uri)) {
                    continue;
                }
                
                var image = await GetImage(uri);
                if (image == null) {
                    continue;
                }
                
                // Костыль. Исправление урла картинки, что она отображась в книге
                img.Attributes["src"].Value = uri.GetFileName();
                images.Add(image);
            }

            return images;
        }

        public abstract Task<Book> Get(Uri url);

        public void Dispose() {
            _config.Client?.Dispose();
        }
    }
}