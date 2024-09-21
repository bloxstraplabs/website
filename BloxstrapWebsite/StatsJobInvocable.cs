using Coravel.Invocable;

namespace BloxstrapWebsite
{
    public class StatsJobInvocable : IInvocable
    {
        public Task Invoke() => Stats.Update();
    }
}
