namespace WorkforceConnect.Application.Common;

/// <summary>Base type for errors the API turns into a specific HTTP status.</summary>
public abstract class AppException(string message) : Exception(message);

/// <summary>404. Also used when a resource exists but is outside the caller's data scope.</summary>
public sealed class NotFoundException(string message = "The requested resource was not found.") : AppException(message);

/// <summary>409. Duplicate names/codes, or deleting something that still has active children.</summary>
public sealed class ConflictException(string message) : AppException(message);

/// <summary>400. A request that is well-formed but breaks a business rule.</summary>
public sealed class AppValidationException(string message) : AppException(message);

/// <summary>403. The caller is authenticated but not allowed to perform the action.</summary>
public sealed class ForbiddenException(string message = "You do not have permission to perform this action.") : AppException(message);

/// <summary>401. Wrong email or password. Deliberately does not say which was wrong.</summary>
public sealed class InvalidCredentialsException() : AppException("Invalid email or password.");

/// <summary>401. The token refers to a user that no longer exists or is inactive.</summary>
public sealed class UnauthenticatedException() : AppException("Authentication is required.");
