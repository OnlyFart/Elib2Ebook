using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.LibSocial.Types.SocialLib;

internal class SocialLibChapter
{
    [JsonPropertyName("chapter_number")]
    public string ChapterNumber { get; set; }

    [JsonPropertyName("chapter_volume")]
    public int ChapterVolume { get; set; }

    [JsonPropertyName("chapter_name")]
    public string ChapterName { get; set; }

    [JsonPropertyName("chapter_slug")]
    public string ChapterSlug { get; set; }

    [JsonPropertyName("branch_id")]
    public int? BranchId { get; set; }

    public string Name => $"Том {ChapterVolume} Глава {ChapterNumber} {ChapterName}".Trim();
}
