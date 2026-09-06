using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Configuration;

/// <summary>Identifies the device by its machine name.</summary>
public sealed class MachineDeviceIdentity : IDeviceIdentity
{
    public string Name { get; } = SafeMachineName();

    private static string SafeMachineName()
    {
        try
        {
            return Environment.MachineName;
        }
        catch (InvalidOperationException)
        {
            // The machine name is unavailable on some minimal containers.
            return "desconhecido";
        }
    }
}
