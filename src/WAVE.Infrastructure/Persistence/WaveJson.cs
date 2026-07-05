using System.Text.Json;
using System.Text.Json.Serialization;

namespace WAVE.Infrastructure.Persistence;

/// <summary>Opções de serialização JSON compartilhadas pela persistência.</summary>
internal static class WaveJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
