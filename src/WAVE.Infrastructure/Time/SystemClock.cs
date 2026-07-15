using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Time;

/// <summary>Real system clock.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
