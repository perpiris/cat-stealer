namespace CatStealer.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    
    public T Data { get; private set; }
    
    public string Error { get; private set; }

    private Result(bool isSuccess, T data, string error)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
    }

    public static Result<T> Success(T data) => 
        new Result<T>(true, data, null);

    public static Result<T> Failure(string error) => 
        new Result<T>(false, default, error);
}


public static class Result
{
    public static Result<T> Success<T>(T data) => Result<T>.Success(data);
    
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}