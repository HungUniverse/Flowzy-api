namespace Flowzy.Service.Exceptions;

public class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class BadRequestException(string message) : ApiException(400, message);
public sealed class UnauthorizedException(string message) : ApiException(401, message);
public sealed class ForbiddenException(string message) : ApiException(403, message);
public sealed class NotFoundException(string message) : ApiException(404, message);
public sealed class ConflictException(string message) : ApiException(409, message);
