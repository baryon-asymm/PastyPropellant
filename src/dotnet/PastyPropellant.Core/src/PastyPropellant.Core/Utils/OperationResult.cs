namespace PastyPropellant.Core.Utils;

public record OperationResult(
    Exception? Exception = null
)
{
    public bool IsSuccess => Exception is null;
}

public record OperationResult<T>(
    T? Value,
    Exception? Exception = null
) : OperationResult(Exception)
    where T : class
{
    public OperationResult(Exception? exception) : this(null, exception)
    {
    }

    public bool HasValue => Value is not null;
}
