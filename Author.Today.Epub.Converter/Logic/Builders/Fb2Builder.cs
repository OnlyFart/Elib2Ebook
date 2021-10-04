using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Author.Today.Epub.Converter.Extensions;
using Author.Today.Epub.Converter.Types.Book;
using EpubSharp.Format;
using HtmlAgilityPack;

namespace Author.Today.Epub.Converter.Logic.Builders {
    public class Fb2Builder : BuilderBase {
        private readonly XElement _book;
        private readonly XNamespace _ns = "http://www.gribuser.ru/xml/fictionbook/2.0";
        private readonly XNamespace _xlink = "http://www.w3.org/1999/xlink";

        private readonly XElement _description;
        private readonly XElement _titleInfo;
        private readonly XElement _body;
        private readonly List<XElement> _images = new();
        
        private Fb2Builder() {
            _book = CreateXElement("FictionBook");
            _book.SetAttributeValue(XNamespace.Xmlns + "xlink", _xlink.NamespaceName);
            _description = CreateXElement("description");
            _titleInfo = CreateXElement("title-info");
            _body = CreateXElement("body");
        }

        private XElement CreateXElement(string name) {
            return new XElement(_ns + name);
        }

        private XElement CreateAuthor(string author) {
            var authorElem = CreateXElement("author");
            var nicknameElem = CreateXElement("nickname");
            nicknameElem.Value = author;
            
            authorElem.Add(nicknameElem);
            return authorElem;
        }

        private XElement CreateTitle(string text) {
            var p = CreateXElement("p");
            p.Value = text;
            
            var title = CreateXElement("title");
            
            title.Add(p);
            return title;
        }

        private XElement GetBinary(Image image) {
            var binaryElem = new XElement(_ns + "binary");
            binaryElem.Value = Convert.ToBase64String(image.Content);
            binaryElem.SetAttributeValue("id", image.Path);
            binaryElem.SetAttributeValue("content-type", "image/" + image.Extension);

            return binaryElem;
        }

        private static HtmlDocument CreateDoc(string content) {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            return doc;
        }

        /// <summary>
        /// Создание нового объекта Builder'a
        /// </summary>
        /// <returns></returns>
        public static BuilderBase Create() {
            return new Fb2Builder();
        }

        /// <summary>
        /// Добавление автора книги
        /// </summary>
        /// <param name="author">Автор</param>
        /// <returns></returns>
        public override BuilderBase AddAuthor(string author) {
            _titleInfo.Add(CreateAuthor(author));
            return this;
        }

        /// <summary>
        /// Указание названия книги
        /// </summary>
        /// <param name="title">Название книги</param>
        /// <returns></returns>
        public override BuilderBase WithTitle(string title) {
            var bookTitle = CreateXElement("book-title");
            bookTitle.Value = title;

            _titleInfo.Add(bookTitle);
            _body.Add(CreateTitle(title));
            return this;
        }

        /// <summary>
        /// Добавление обложки книги
        /// </summary>
        /// <param name="cover">Обложка</param>
        /// <returns></returns>
        public override BuilderBase WithCover(Image cover) {
            var imageElem = CreateXElement("image");
            imageElem.SetAttributeValue(_xlink + "href", "#" + cover.Path);

            _titleInfo.Add(imageElem);
            _images.Add(GetBinary(cover));
            return this;
        }

        /// <summary>
        /// Добавление внешних файлов
        /// </summary>
        /// <param name="directory">Путь к директории с файлами</param>
        /// <param name="searchPattern">Шаблон поиска файлов</param>
        /// <param name="type">Тип файла</param>
        /// <returns></returns>
        public override BuilderBase WithFiles(string directory, string searchPattern, EpubContentType type) {
            return this;
        }

        /// <summary>
        /// Добавление списка частей книги
        /// </summary>
        /// <param name="chapters">Список частей</param>
        /// <returns></returns>
        public override BuilderBase WithChapters(IEnumerable<Chapter> chapters) {
            foreach (var chapter in chapters.Where(c => c.IsValid)) {
                var section = CreateXElement("section");
                section.Add(CreateTitle(chapter.Title));
                
                var doc = CreateDoc(chapter.Content);
                foreach (var node in doc.DocumentNode.ChildNodes) {
                    if (node.Name == "p") {
                        var p = CreateXElement("p");
                        foreach (var child in node.ChildNodes) {
                            switch (child.Name) {
                                case "#text":
                                case "br":
                                case "span":
                                    p.Add(new XText(child.InnerText));
                                    break;
                                case "em":
                                case "strong": {
                                    var elem = CreateXElement(child.Name);
                                    elem.Value = child.InnerText;
                                    p.Add(elem);
                                    break;
                                }
                                case "img": {
                                    var imageElem = CreateXElement("image");
                                    imageElem.SetAttributeValue(_xlink + "href", "#" + child.Attributes["src"].Value);
                                    p.Add(imageElem);
                                    break;
                                }
                                default:
                                    p.Add(new XText(child.InnerText));
                                    Console.WriteLine(child.Name);
                                    break;
                            }
                        }

                        section.Add(p);
                    }
                }
                
                _body.Add(section);

                foreach (var image in chapter.Images) {
                    _images.Add(GetBinary(image));
                }
            }

            return this;
        }

        protected override void BuildInternal(string name) {
            _description.Add(_titleInfo);
            _book.Add(_description);
            _book.Add(_body);

            foreach (var image in _images) {
                _book.Add(image);
            }
            
            _book.Save(name);
        }

        protected override string GetFileName(string name) {
            return $"{name}.fb2".RemoveInvalidChars();
        }
    }
}