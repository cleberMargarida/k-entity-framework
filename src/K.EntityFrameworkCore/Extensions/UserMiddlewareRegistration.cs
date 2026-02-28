namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// Describes a user-registered middleware type for pipeline integration.
/// </summary>
internal record UserMiddlewareRegistration(Type MiddlewareType);
