using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Builders; 

public class Fb2Builder : BuilderBase {
    private readonly XElement _book;
    private readonly XNamespace _ns = "http://www.gribuser.ru/xml/fictionbook/2.0";
    private readonly XNamespace _xlink = "http://www.w3.org/1999/xlink";

    private readonly XElement _description;
    private readonly XElement _titleInfo;
    private readonly XElement _body;
    private readonly XElement _documentInfo;
    private readonly List<Image> _images = new();

    private readonly Dictionary<string, string> _map = new() {
        {"strong", "strong"},
        {"b", "strong"},
        {"i", "emphasis"},
        {"em", "emphasis"},
        {"emphasis", "emphasis"},
        {"del", "strikethrough"},
        {"blockquote", "cite"},
        {"h1", "subtitle"},
        {"h2", "subtitle"},
        {"h3", "subtitle"},
        {"h4", "subtitle"},
        {"h5", "subtitle"},
        {"h6", "subtitle"},
        {"p", "p"},
        {"div", "p"},
    };

    private Fb2Builder() {
        _book = CreateXElement("FictionBook");
        _book.SetAttributeValue(XNamespace.Xmlns + "xlink", _xlink.NamespaceName);
        _description = CreateXElement("description");
        _titleInfo = CreateXElement("title-info");
        _documentInfo = CreateXElement("document-info");
        _body = CreateXElement("body");
    }

    private XElement CreateXElement(string name) {
        return new XElement(_ns + name);
    }

    private XElement CreateAuthor(Author author) {
        var authorElem = CreateXElement("author");

        var parts = author.Name.Split(" ");
        if (parts.Length == 2) {
            var firstName = CreateXElement("first-name");
            firstName.Value = parts[0];
            authorElem.Add(firstName);

            var lastName = CreateXElement("last-name");
            lastName.Value = parts[1];
            authorElem.Add(lastName);
        } else {
            var firstName = CreateXElement("first-name");
            firstName.Value = author.Name;
            authorElem.Add(firstName);
        }

        if (author.Url != default) {
            var homePageElem = CreateXElement("home-page");
            homePageElem.Value = author.Url.ToString();
            authorElem.Add(homePageElem);
        }

        return authorElem;
    }

    private XElement CreateTitle(string text) {
        var p = CreateXElement("p");
        p.Value = text.HtmlDecode().ReplaceNewLine();
            
        var title = CreateXElement("title");
            
        title.Add(p);
        return title;
    }

    private XElement GetBinary(Image image) {
        var binaryElem = new XElement(_ns + "binary");
        binaryElem.Value = Convert.ToBase64String(image.GetContent().Result);
        binaryElem.SetAttributeValue("id", image.Name);
        binaryElem.SetAttributeValue("content-type", "image/" + image.Extension);

        return binaryElem;
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
    public override BuilderBase AddAuthor(Author author) {
        var authorElem = CreateAuthor(author);
        _titleInfo.Add(authorElem);
        _documentInfo.Add(authorElem);
        return this;
    }

    /// <summary>
    /// Указание названия книги
    /// </summary>
    /// <param name="title">Название книги</param>
    /// <returns></returns>
    public override BuilderBase WithTitle(string title) {
        var bookTitle = CreateXElement("book-title");
        bookTitle.Value = title.ReplaceNewLine();

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
        if (cover != default) {
            var coverPage = CreateXElement("coverpage");
            
            var imageElem = CreateXElement("image");
            imageElem.SetAttributeValue(_xlink + "href", "#" + cover.Name);
            
            coverPage.Add(imageElem);
            _titleInfo.Add(coverPage);
            _images.Add(cover);
        }

        return this;
    }

    public override BuilderBase WithBookUrl(Uri url) {
        if (url != default) {
            var srcUrlElem = CreateXElement("src-url");
            srcUrlElem.Value = url.ToString().CleanInvalidXmlChars();
            _documentInfo.Add(srcUrlElem);
        }

        return this;
    }

    public override BuilderBase WithAnnotation(string annotation) {
        if (!string.IsNullOrWhiteSpace(annotation)) {
            _titleInfo.Add(CreateAnnotation(annotation));
        }

        return this;
    }

    private XElement CreateAnnotation(string annotation) {
        var a = CreateXElement("annotation");

        var doc = annotation.AsHtmlDoc();
        foreach (var node in doc.DocumentNode.ChildNodes) {
            if (!string.IsNullOrWhiteSpace(node.InnerText)) {
                ProcessSection(a, node, "p");
            }
        }
        
        return a;
    }

    /// <summary>
    /// Добавление внешних файлов
    /// </summary>
    /// <param name="directory">Путь к директории с файлами</param>
    /// <param name="searchPattern">Шаблон поиска файлов</param>
    /// <param name="type">Тип файла</param>
    /// <returns></returns>
    public override BuilderBase WithFiles(string directory, string searchPattern) {
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
                
            var doc = chapter.Content.AsHtmlDoc();
            foreach (var node in doc.DocumentNode.ChildNodes) {
                ProcessSection(section, node);
            }
                
            _body.Add(section);

            foreach (var image in chapter.Images) {
                _images.Add(image);
            }
        }

        return this;
    }

    public override BuilderBase WithSeria(Seria seria) {
        if (seria != default) {
            var sequenceElem = CreateXElement("sequence");
            sequenceElem.SetAttributeValue("name", seria.Name.CleanInvalidXmlChars());
            sequenceElem.SetAttributeValue("number", seria.Number.CleanInvalidXmlChars());
            _titleInfo.Add(sequenceElem);
        }

        return this;
    }

    public override BuilderBase WithLang(string lang) {
        if (!string.IsNullOrWhiteSpace(lang)) {
            var langElem = CreateXElement("lang");
            langElem.Value = lang;
            _titleInfo.Add(langElem);
        }

        return this;
    }

    private static bool IsTextNode(HtmlNode node) {
        return node.Name is "#text" or "br" or "span";
    }

    private void ProcessSection(XElement parent, HtmlNode node, string textNode = "") {
        if (node.Name == "a") {
            if (node.Attributes["href"] == null) {
                return;
            }

            var href = node.Attributes["href"].Value;
            if (string.IsNullOrWhiteSpace(textNode)) {
                parent.Add(href);
            } else {
                var tag = CreateXElement(textNode);
                tag.Value = href.CleanInvalidXmlChars();
                parent.Add(tag);
            }

            return;
        }
        
        if (node.ChildNodes.Count > 0) {
            var section = CreateXElement(_map.GetValueOrDefault(node.Name, "p"));

            foreach (var child in node.ChildNodes) {
                ProcessSection(IsTextNode(node) ? parent : section, child);
            }

            if (!section.IsEmpty) {
                parent.Add(section);
            }

            return;
        }
        
        if (node.Name == "img") {
            if (node.Attributes["src"] == null) {
                return;
            }
            
            var imageElem = CreateXElement("image");
            imageElem.SetAttributeValue(_xlink + "href", "#" + node.Attributes["src"].Value);
            parent.Add(imageElem);

            return;
        }

        var nodeText = node.InnerText.HtmlDecode().CleanInvalidXmlChars();
        if (node.InnerText.StartsWith(" ")) {
            nodeText = " " + nodeText;
        }
        
        if (node.InnerText.EndsWith(" ")) {
            nodeText += " ";
        }
        
        if (IsTextNode(node)) {
            if (string.IsNullOrWhiteSpace(textNode)) {
                parent.Add(new XText(nodeText));
            } else {
                var tag = CreateXElement(textNode);
                tag.Value = nodeText;
                parent.Add(tag);
            }
            
            return;
        }

        if (_map.TryGetValue(node.Name, out var fb2Tag)) {
            var tag = CreateXElement(fb2Tag);
            tag.Value = nodeText;
            parent.Add(tag);
        } else {
            parent.Add(nodeText);
            Console.WriteLine(node.Name);
        }
    }

    private XElement GetDateElement(DateTime date) {
        var today = date.ToString("yyyy-MM-dd");
        
        var dateElem = CreateXElement("date");
        dateElem.SetAttributeValue("value", today);
        dateElem.Value = today;
        
        return dateElem;
    }

    protected override async Task BuildInternal(string name) {
        _documentInfo.Add(GetDateElement(DateTime.Today));
        
        _description.Add(_titleInfo);
        _description.Add(_documentInfo);

        await using var file = File.Create(name);
        var xws = new XmlWriterSettings {
            Async = true,
            Encoding = Encoding.UTF8,
            Indent = true,
            NewLineHandling = NewLineHandling.Replace
        };

        await using var writer = XmlWriter.Create(file, xws);
        var cancellationToken = new CancellationToken();
        
        await writer.WriteStartElementAsync(string.Empty, "FictionBook", _ns.NamespaceName);
        await writer.WriteAttributeStringAsync("xmlns", "xlink", null, _xlink.NamespaceName);

        await _description.WriteToAsync(writer, cancellationToken);
        await _body.WriteToAsync(writer, cancellationToken);
        foreach (var image in _images) {
            await GetBinary(image).WriteToAsync(writer, cancellationToken);
        }

        await writer.WriteEndElementAsync();
        await writer.FlushAsync();
    }

    protected override string GetFileName(string name) {
        return $"{name}.fb2".RemoveInvalidChars();
    }
}