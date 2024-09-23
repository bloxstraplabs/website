using BloxstrapWebsite.Models.GitHubApi;

namespace BloxstrapWebsite.Services
{
    public class StatsService : IStatsService
    {
        public bool Loaded { get; private set; } = false;

        public int StarCount { get; private set; }

        public int ReleaseSizeMB { get; private set; }

        public string Version { get; private set; } = "";

        public async Task Update()
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "bloxstraplabs/website");

            var repoData = await httpClient.GetFromJsonAsync<RepoData>("https://api.github.com/repos/pizzaboxer/bloxstrap");
            var releaseData = await httpClient.GetFromJsonAsync<Release>("https://api.github.com/repos/pizzaboxer/bloxstrap/releases/latest");

            StarCount = repoData!.StargazersCount;
            Version = releaseData!.TagName.Substring(1);
            ReleaseSizeMB = releaseData.Assets.ToArray()[0].Size / (1024 * 1024);

            Loaded = true;
        }
    }
}
