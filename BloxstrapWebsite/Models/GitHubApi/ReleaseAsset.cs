using System.Text.Json.Serialization;

namespace BloxstrapWebsite.Models.GitHubApi
{
    public class ReleaseAsset
    {
        [JsonPropertyName("size")]
        public int Size { get; set; }
    }
}
