﻿namespace BloxstrapWebsite.Models
{
    public class IndexViewModel
    {
        public int StarCount { get; set; }

        public double ReleaseSizeMB { get; set; }

        public Version Version { get; set; } = null!;
    }
}
