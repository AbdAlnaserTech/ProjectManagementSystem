namespace ProjectManagement.Api.Errors;

public class CrudException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public static CrudException NotFound(string resource) => new(404, $"{resource} was not found.");
    public static CrudException Invalid(string message) => new(400, message);
    public static CrudException Conflict(string message) => new(409, message);
}
