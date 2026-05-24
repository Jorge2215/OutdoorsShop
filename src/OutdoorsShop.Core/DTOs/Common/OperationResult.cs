namespace OutdoorsShop.Core.DTOs.Common;

public class OperationResult
{
    public bool Succeeded { get; init; }
    public bool Forbidden { get; init; }
    public bool NotFound { get; init; }
    public string? ErrorMessage { get; init; }

    public static OperationResult Success() => new() { Succeeded = true };
    public static OperationResult ForbiddenResult(string? errorMessage = null) => new() { Forbidden = true, ErrorMessage = errorMessage };
    public static OperationResult NotFoundResult(string errorMessage) => new() { NotFound = true, ErrorMessage = errorMessage };
    public static OperationResult Invalid(string errorMessage) => new() { ErrorMessage = errorMessage };
}

public class OperationResult<T> : OperationResult
{
    public T? Value { get; init; }

    public static OperationResult<T> Success(T value) => new() { Succeeded = true, Value = value };
    public static new OperationResult<T> ForbiddenResult(string? errorMessage = null) => new() { Forbidden = true, ErrorMessage = errorMessage };
    public static new OperationResult<T> NotFoundResult(string errorMessage) => new() { NotFound = true, ErrorMessage = errorMessage };
    public static new OperationResult<T> Invalid(string errorMessage) => new() { ErrorMessage = errorMessage };
}
