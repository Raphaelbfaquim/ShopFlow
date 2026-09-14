namespace ShopFlow.BuildingBlocks.Results;

public class Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Resultado de sucesso não pode conter erro.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Resultado de falha precisa de um erro.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.CreateSuccess(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.CreateFailure(error);
}

public class Result<T> : Result
{
    public T Value { get; }

    private Result(T value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    internal static Result<T> CreateSuccess(T value) => new(value, true, Error.None);

    internal static Result<T> CreateFailure(Error error) => new(default!, false, error);
}
