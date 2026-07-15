namespace WAVE.Application.Abstractions;

/// <summary>Abstracted time source to allow deterministic tests.</summary>
public interface IClock
{
    DateTimeOffset Now { get; }
}
