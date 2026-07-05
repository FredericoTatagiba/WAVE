namespace WAVE.Application.Abstractions;

/// <summary>Fonte de tempo abstraída para permitir testes determinísticos.</summary>
public interface IClock
{
    DateTimeOffset Now { get; }
}
