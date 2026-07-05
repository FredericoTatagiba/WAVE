namespace WAVE.Domain.Common;

/// <summary>
/// Resultado de uma operação que pode falhar, evitando o uso de exceções para
/// controle de fluxo (padrão "railway"). Mantém baixo acoplamento e clareza.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);

    public static Result Failure(string error) => new(false, error);
}

/// <summary>Resultado que carrega um valor em caso de sucesso.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, string error)
        : base(isSuccess, error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Não há valor em um resultado de falha.");

    public static Result<T> Success(T value) => new(true, value, string.Empty);

    public static new Result<T> Failure(string error) => new(false, default, error);
}
