using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace ClassicExtended
{
    public class ClassicExtendedConfig : BasePluginConfig
    {
        public override int Version { get; set; } = 1;
        public string ChatPrefix { get; set; } = "";

        [JsonPropertyName("DatabaseHost")]
		public string DatabaseHost { get; set; } = "";

		[JsonPropertyName("DatabasePort")]
		public int DatabasePort { get; set; } = 3306;

        [JsonPropertyName("DatabaseUser")]
        public string DatabaseUser { get; set; } = "";

        [JsonPropertyName("DatabasePassword")]
        public string DatabasePassword { get; set; } = "";

        [JsonPropertyName("DatabaseName")]
        public string DatabaseName { get; set; } = "";
        [JsonPropertyName("DbEnabled")]
        public bool DbEnabled { get; set; } = true;

        [JsonPropertyName("SpectatorEnabled")]
        public bool SpecatateEnabled { get; set; } = true;
    }
}