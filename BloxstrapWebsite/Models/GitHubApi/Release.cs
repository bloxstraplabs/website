using System.Text.Json.Serialization;

namespace BloxstrapWebsite.Models.GitHubApi
{
    public class Release
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("assets")]
        public IEnumerable<ReleaseAsset> Assets { get; set; }
    }
}
