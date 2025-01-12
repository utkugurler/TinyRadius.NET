using System.Net;

namespace TinyRadius.NET.Utils;

/// <summary>
/// This class stores information about a Radius endpoint.
/// This includes the address of the remote endpoint and the shared secret
/// used for securing the communication.
/// </summary>
public class RadiusEndpoint
{
    public IPEndPoint EndpointAddress { get; set; }
    public string SharedSecret { get; set; }
}
