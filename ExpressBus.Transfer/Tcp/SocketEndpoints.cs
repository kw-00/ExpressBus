using System.Net;
using System.Net.Sockets;

namespace ExpressBus.Transfer.Tcp;

/// <summary>
/// Resolves a hostname string to an IPv4 <see cref="IPAddress"/>.
/// </summary>
internal static class SocketEndpoints
{
	/// <summary>
	/// Resolves a hostname to an IP address.
	/// Returns the first IPv4 address, falling back to any available address if none exist.
	/// </summary>
	internal static IPAddress Resolve(string host)
	{
		if (IPAddress.TryParse(host, out var ip))
			return ip;

		var addresses = System.Net.Dns.GetHostAddresses(host);
		return addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
			?? addresses.First();
	}
}
