using System.Text.Json;
using System.Text.Json.Serialization;

namespace WAVE.Infrastructure.Persistence;

/// <summary>JSON serialization options shared across persistence.</summary>
internal static class WaveJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
