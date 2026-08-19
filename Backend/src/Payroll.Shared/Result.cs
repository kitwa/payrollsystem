namespace Payroll.Shared;

/// <summary>Result wrapper — all Application handlers return this instead of throwing.</summary>
public class Result<T>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = [];

    private Result() { }

    public static Result<T> Ok(T value) => new() { IsSuccess = true, Value = value };

    public static Result<T> Fail(string error) => new() { IsSuccess = false, Errors = [error] };

    public static Result<T> Fail(IEnumerable<string> errors) => new() { IsSuccess = false, Errors = errors.ToList() };
}

/// <summary>Non-generic result for commands that return no data.</summary>
public class Result
{
    public bool IsSuccess { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = [];

    private Result() { }

    public static Result Ok() => new() { IsSuccess = true };

    public static Result Fail(string error) => new() { IsSuccess = false, Errors = [error] };

    public static Result Fail(IEnumerable<string> errors) => new() { IsSuccess = false, Errors = errors.ToList() };
}
