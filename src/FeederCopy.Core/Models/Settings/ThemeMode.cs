using System.Text.Json.Serialization;

namespace FeederCopy.Core.Models.Settings
{
    [JsonConverter(typeof(JsonStringEnumConverter<ThemeMode>))]
    public enum ThemeMode
    {
        Light,
        Dark,
    }
}
