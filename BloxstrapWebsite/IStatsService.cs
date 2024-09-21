namespace BloxstrapWebsite
{
    public interface IStatsService
    {
        bool Loaded { get; }

        int StarCount { get; }

        int ReleaseSizeMB { get; }

        string Version { get; }

        Task Update();
    }
}
