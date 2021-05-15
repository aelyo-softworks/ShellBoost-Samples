using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public class ServerInfo
    {
        public ServerInfo()
        {
            LocalUtcNow = DateTime.UtcNow;
        }

        [DisplayName("File System")]
        public string FileSystem { get; set; }

        public Guid Id { get; set; }
        public string Bitness { get; set; }

        [DisplayName("Root Id")]
        public Guid RootId { get; set; }

        public string Configuration { get; set; }

        [DisplayName("Stable Version")]
        public string FileVersion { get; set; }

        [DisplayName("Build Version")]
        public string InformationalVersion { get; set; }

        [Browsable(false)]
        public DateTime UtcNow { get; set; }

        [JsonIgnore]
        [Browsable(false)]
        public DateTime LocalUtcNow { get; set; }

        // should be near zero! or server and client are not in sync
        [DisplayName("Time Difference")]
        public TimeSpan TimeDifference => LocalUtcNow - UtcNow;
    }
}
