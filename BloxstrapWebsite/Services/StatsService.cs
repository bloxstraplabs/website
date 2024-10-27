using BloxstrapWebsite.Models.GitHubApi;

namespace BloxstrapWebsite.Services
{
    public class StatsService : IStatsService
    {
        public bool Loaded { get; private set; } = false;

        public int StarCount { get; private set; }

        public int ReleaseSizeMB { get; private set; }

        public Version Version { get; private set; } = null!;

        private readonly IHttpClientFactory _httpClientFactory;

        public StatsService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task Update()
        {
            var httpClient = _httpClientFactory.CreateClient("GitHub");

            var repoData = await httpClient.GetFromJsonAsync<RepoData>("https://api.github.com/repos/bloxstraplabs/bloxstrap");
            var releaseData = await httpClient.GetFromJsonAsync<Release>("https://api.github.com/repos/bloxstraplabs/bloxstrap/releases/latest");

            StarCount = repoData!.StargazersCount;
            Version = new Version(releaseData!.TagName.Substring(1));
            ReleaseSizeMB = releaseData.Assets.ToArray()[0].Size / (1024 * 1024);

            Loaded = true;
        }
    }
}
