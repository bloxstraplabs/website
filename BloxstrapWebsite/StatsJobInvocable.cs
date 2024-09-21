using Coravel.Invocable;

namespace BloxstrapWebsite
{
    public class StatsJobInvocable : IInvocable
    {
        private IStatsService _statsService;

        public StatsJobInvocable(IStatsService statsService) 
        { 
            _statsService = statsService;
        }

        public Task Invoke() => _statsService.Update();
    }
}
