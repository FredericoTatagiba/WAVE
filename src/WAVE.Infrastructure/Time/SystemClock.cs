using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Time;

/// <summary>Relógio real do sistema.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
