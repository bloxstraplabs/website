using System.Text.Json.Serialization;

namespace BloxstrapWebsite.Models.GitHubApi
{
    public class RepoData
    {
        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }
    }
}
