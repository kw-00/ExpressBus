namespace ExpressBus.Protocol;

/// <summary>
/// Marks a message as associated with a specific request, identified by <see cref="RequestId"/>.
/// </summary>
/// <remarks>
/// Implemented by all request and response message types. Notifications (which are fire-and-forget)
/// do not implement this interface.
/// </remarks>
public interface IRequestAssociated
{
    /// <summary>
    /// Unique identifier correlating a request with its associated response.
    /// </summary>
    Guid RequestId { get; }
}
