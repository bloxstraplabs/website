using BloxstrapWebsite.Enums;

namespace BloxstrapWebsite.Models
{
    public class StatPoint
    {
        public required string Name { get; set; }

        public List<string>? Values { get; set; }

        public required bool ProductionOnly { get; set; }

        public string Bucket { get; set; } = "bloxstrap";

        /// <summary>
        /// Provided in seconds
        /// </summary>
        public required int RatelimitInterval { get; set; }

        public int RatelimitCount { get; set; } = 1;

        public RatelimitType RatelimitType { get; set; } = RatelimitType.Key;
    }
}
