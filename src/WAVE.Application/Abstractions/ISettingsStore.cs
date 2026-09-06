using WAVE.Application.Configuration;

namespace WAVE.Application.Abstractions;

/// <summary>Reads and writes the per-device settings.</summary>
public interface ISettingsStore
{
    /// <summary>The settings in force right now.</summary>
    WaveSettings Current { get; }

    Task SaveAsync(WaveSettings settings, CancellationToken cancellationToken = default);

    /// <summary>Raised after a successful save, so path consumers pick the change up.</summary>
    event EventHandler? Changed;
}
