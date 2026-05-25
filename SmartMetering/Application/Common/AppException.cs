namespace SmartMetering.Application.Common;

/// <summary>Base class for expected, user-facing errors mapped to HTTP status codes by the API.</summary>
public class AppException : Exception
{
    public AppException(string message, int statusCode = StatusCodes.BadRequest)
        : base(message) => StatusCode = statusCode;

    public int StatusCode { get; }

    public static class StatusCodes
    {
        public const int BadRequest = 400;
        public const int Unauthorized = 401;
        public const int Forbidden = 403;
        public const int NotFound = 404;
        public const int Conflict = 409;
    }
}

public sealed class NotFoundException(string message) : AppException(message, StatusCodes.NotFound);

public sealed class ConflictException(string message) : AppException(message, StatusCodes.Conflict);

public sealed class UnauthorizedAppException(string message) : AppException(message, StatusCodes.Unauthorized);
