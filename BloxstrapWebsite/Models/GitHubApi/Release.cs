using System.Text.Json.Serialization;

namespace BloxstrapWebsite.Models.GitHubApi
{
    public class Release
    {
        [JsonPropertyName("tag_name")]
        public required string TagName { get; set; }

        [JsonPropertyName("assets")]
        public required IEnumerable<ReleaseAsset> Assets { get; set; }
    }
}
