namespace BloxstrapWebsite.Models.Configuration
{
    public class Credentials
    {
        private string? _influxDBToken;

        public string StatsKey { get; set; } = "";
        
        public string? InfluxDBToken 
        { 
            get => !String.IsNullOrEmpty(_influxDBToken) ? _influxDBToken : Environment.GetEnvironmentVariable("BLOXSTRAP_WEBSITE_TOKEN_INFLUXDB"); 
            set => _influxDBToken = value;
        }
    }
}
