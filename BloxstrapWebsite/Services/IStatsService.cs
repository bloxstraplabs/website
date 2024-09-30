namespace BloxstrapWebsite.Services
{
    public interface IStatsService
    {
        bool Loaded { get; }

        int StarCount { get; }

        int ReleaseSizeMB { get; }

        Version Version { get; }

        Task Update();
    }
}
