namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Status code for requests and responses.
/// </summary>
/// <remarks>
/// Placed as the first field in every request and response payload so the
/// deserializer can check success/failure before accessing other fields.
/// </remarks>
public enum Status : byte
{
	Success = 0,
	Error = 1,
}
