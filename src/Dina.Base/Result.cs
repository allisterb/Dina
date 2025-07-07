namespace Dina;

public enum ResultType
{
    Success,
    Failure
}
    
public struct Result<T>
{
    public Result(ResultType type, T? value = default, string? message = null, Exception ? exception = null)
    {
        this.Type = type;
        this._Value = value;
        this.Message = message;
        this.Exception = exception;
    }

    public bool IsSuccess => this.Type == ResultType.Success;

    public T Value => IsSuccess ? _Value!: throw new InvalidOperationException("The operation did not succced.");
       
    public bool Succeeded(out Result<T> r)
    {
        r = this;
        return IsSuccess;
    }

    public static Result<T> Success(T value) => new Result<T>(ResultType.Success, value);

    public static Result<T> Failure(string? message, Exception? exception = null) => new Result<T>(ResultType.Failure, message:message, exception: exception);

    public static async Task<Result<T>> ExecuteAsync(Task<T> task, string? errorMessage = null)
    {
        try
        {  
            return Success(await task);
        }
        catch (Exception ex)
        {
            return Failure(errorMessage, ex);   
        }
    }
    public ResultType Type;
    public T? _Value;
    public string? Message;
    public Exception? Exception;
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new Result<T>(ResultType.Success, value);

    public static Result<T> Failure<T>(string? message, Exception? exception = null) => new Result<T>(ResultType.Failure, message: message, exception: exception);

    public static Result<T> Failure<T>(string message, params object[] args) => new Result<T>(ResultType.Failure, message:string.Format(message, args));

    public static Result<T> Failure<T>(string message, Exception exception, params object[] args) => new Result<T>(ResultType.Failure, exception:exception, message: string.Format(message, args));

    public static Result<T> FailureError<T>(string message, Exception? exception = null)
    {
        if (exception is not null)
        {
            Runtime.Error(exception, message);
            return Failure<T>(message, exception);
        }
        else
        {
            Runtime.Error(message);
            return Failure<T>(message);
        }
    }

    public static Result<T> FailureError<T>(string message, params object[] args)
    {
       
        Runtime.Error(message, args);
        return Failure<T>(message, args);
        
    }

    public static Result<T> FailureError<T>(string message, Exception exception, params object[] args)
    {
        Runtime.Error(exception, message, args);
        return Failure<T>(message, exception, args);
    }

    public static async Task<Result<T>> ExecuteAsync<T>(Task<T> task, string? errorMessage = null) => await Result<T>.ExecuteAsync(task, errorMessage);

    public static bool Succedeed<T>(Result<T> result, out Result<T> r)
    {
        r = result;
        return r.IsSuccess;
    }
}

