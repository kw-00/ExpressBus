using System.Net;

namespace ExpressBus.Transfer;

/// <summary>
/// Network address for a transfer endpoint.
/// </summary>
public sealed class Address
{
	/// <summary>
	/// Hostname or IP address.
	/// </summary>
	public string Host { get; }

	/// <summary>
	/// TCP port number.
	/// </summary>
	public int Port { get; }

	/// <summary>
	/// Creates an <see cref="Address"/> for the given host and port.
	/// </summary>
	/// <param name="host">Hostname or IP address. Must not be null or empty.</param>
	/// <param name="port">Port number.</param>
	public Address(string host, int port)
	{
		if (string.IsNullOrWhiteSpace(host))
			throw new ArgumentException("Host must not be null or empty.", nameof(host));

		if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
			throw new ArgumentOutOfRangeException(nameof(port), $"Port must be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}.");

		Host = host;
		Port = port;
	}

	public override bool Equals(object? obj) =>
		obj is Address other && other.Host == Host && other.Port == Port;

	public override int GetHashCode() =>
		HashCode.Combine(Host, Port);

	public override string ToString() =>
		$"{Host}:{Port}";
}
