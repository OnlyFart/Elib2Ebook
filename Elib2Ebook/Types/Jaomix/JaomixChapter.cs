using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Jaomix; 

public class JaomixChapter {
    [JsonPropertyName("link")]
    public string Link { get; set; }

    [JsonPropertyName("title")]
    public JaomixChapterTitle Title { get; set; }

    [JsonPropertyName("content")]
    public JaomixChapterContent Content { get; set; }

    public int id { get; set; }
    public DateTime date { get; set; }
    public DateTime date_gmt { get; set; }
    public JaomixChapterGuid guid { get; set; }
    public DateTime modified { get; set; }
    public DateTime modified_gmt { get; set; }
    public string slug { get; set; }
    public string status { get; set; }
    public string type { get; set; }
    public JaomixChapterExcerpt excerpt { get; set; }
    public int author { get; set; }
    public int featured_media { get; set; }
    public string comment_status { get; set; }
    public string ping_status { get; set; }
    public bool sticky { get; set; }
    public string template { get; set; }
    public string format { get; set; }
    public List<object> meta { get; set; }
    public List<int> categories { get; set; }
    public List<object> tags { get; set; }
    public List<object> acf { get; set; }
    public JaomixChapterLinks _links { get; set; }
}

public class JaomixChapterContent
{
    [JsonPropertyName("rendered")]
    public string Rendered { get; set; }

    [JsonPropertyName("protected")]
    public bool @protected { get; set; }
}

public class JaomixChapterTitle
{
    [JsonPropertyName("rendered")]
    public string Rendered { get; set; }
}

public class JaomixChapterAbout
{
    public string href { get; set; }
}

public class JaomixChapterAuthor
{
    public bool embeddable { get; set; }
    public string href { get; set; }
}

public class JaomixChapterCollection
{
    public string href { get; set; }
}

public class JaomixChapterCury
{
    public string name { get; set; }
    public string href { get; set; }
    public bool templated { get; set; }
}

public class JaomixChapterExcerpt
{
    public string rendered { get; set; }
    public bool @protected { get; set; }
}

public class JaomixChapterGuid
{
    public string rendered { get; set; }
}

public class JaomixChapterLinks
{
    public List<JaomixChapterSelf> self { get; set; }
    public List<JaomixChapterCollection> collection { get; set; }
    public List<JaomixChapterAbout> about { get; set; }
    public List<JaomixChapterAuthor> author { get; set; }
    public List<JaomixChapterReply> replies { get; set; }

    [JsonPropertyName("version-history")]
    public List<JaomixChapterVersionHistory> versionhistory { get; set; }

    [JsonPropertyName("wp:attachment")]
    public List<JaomixChapterWpAttachment> wpattachment { get; set; }

    [JsonPropertyName("wp:term")]
    public List<JaomixChapterWpTerm> wpterm { get; set; }
    public List<JaomixChapterCury> curies { get; set; }
}

public class JaomixChapterReply
{
    public bool embeddable { get; set; }
    public string href { get; set; }
}

public class JaomixChapterSelf
{
    public string href { get; set; }
}

public class JaomixChapterVersionHistory
{
    public int count { get; set; }
    public string href { get; set; }
}

public class JaomixChapterWpAttachment
{
    public string href { get; set; }
}

public class JaomixChapterWpTerm
{
    public string taxonomy { get; set; }
    public bool embeddable { get; set; }
    public string href { get; set; }
}

