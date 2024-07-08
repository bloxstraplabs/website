using BloxstrapWebsite.Models.GitHubApi;

namespace BloxstrapWebsite
{
    public static class Stats
    {
        public static bool Loaded { get; private set; } = false;

        public static int StarCount { get; private set; }

        public static int ReleaseSizeMB { get; private set; }

        public static string Version { get; private set; }

        public static async Task Update()
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "bloxstraplabs/website");

            var repoData = await httpClient.GetFromJsonAsync<RepoData>("https://api.github.com/repos/pizzaboxer/bloxstrap");
            var releaseData = await httpClient.GetFromJsonAsync<Release>("https://api.github.com/repos/pizzaboxer/bloxstrap/releases/latest");
            
            StarCount = repoData.StargazersCount;
            Version = releaseData.TagName.Substring(1);
            ReleaseSizeMB = releaseData.Assets.ToArray()[0].Size / (1024 * 1024);

            Loaded = true;
        }
    }
}
