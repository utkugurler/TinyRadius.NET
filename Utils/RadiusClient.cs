using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using TinyRadius.NET.Packets;

namespace TinyRadius.NET.Utils
{
    /// <summary>
    /// Represents a RADIUS client that communicates with a specified RADIUS server.
    /// </summary>
    public class RadiusClient
    {
        private const int DEFAULT_AUTH_PORT = 1812;
        private const int DEFAULT_ACCT_PORT = 1813;
        private const int DEFAULT_RETRY_COUNT = 3;
        private const int DEFAULT_SOCKET_TIMEOUT = 3000;
        private const string DEFAULT_AUTH_PROTOCOL = AccessRequest.AUTH_PAP;

        private string hostName;
        private string sharedSecret;
        private int authPort;
        private int acctPort;
        private int retryCount;
        private int socketTimeout;
        private string authProtocol;

        public RadiusClient(string hostName, string sharedSecret)
        {
            HostName = hostName;
            SharedSecret = sharedSecret;
            authPort = DEFAULT_AUTH_PORT;
            acctPort = DEFAULT_ACCT_PORT;
            retryCount = DEFAULT_RETRY_COUNT;
            socketTimeout = DEFAULT_SOCKET_TIMEOUT;
            authProtocol = DEFAULT_AUTH_PROTOCOL;
        }

        public RadiusClient(RadiusEndpoint client) : this(client.EndpointAddress.Address.ToString(), client.SharedSecret)
        {
        }

        public string HostName
        {
            get => hostName;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || !IPAddress.TryParse(value, out _))
                {
                    throw new ArgumentException("Invalid host name or IP address.", nameof(value));
                }
                hostName = value;
            }
        }

        public string SharedSecret
        {
            get => sharedSecret;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Shared secret must not be empty.", nameof(value));
                }
                sharedSecret = value;
            }
        }

        public int AuthPort
        {
            get => authPort;
            set => authPort = ValidatePort(value);
        }

        public int AcctPort
        {
            get => acctPort;
            set => acctPort = ValidatePort(value);
        }

        public int RetryCount
        {
            get => retryCount;
            set
            {
                if (value < 1) throw new ArgumentException("Retry count must be positive.");
                retryCount = value;
            }
        }

        public int SocketTimeout
        {
            get => socketTimeout;
            set
            {
                if (value < 1) throw new ArgumentException("Socket timeout must be positive.");
                socketTimeout = value;
            }
        }

        public string AuthProtocol
        {
            get => authProtocol;
            set => authProtocol = value;
        }

        public bool Authenticate(string userName, string password) => Authenticate(userName, password, authProtocol);

        public bool Authenticate(string userName, string password, string protocol)
        {
            var request = new AccessRequest(userName, password);
            request.SetAuthProtocol(protocol);
            var response = Authenticate(request);
            return response?.PacketType == RadiusPacket.ACCESS_ACCEPT;
        }

        public RadiusPacket Authenticate(AccessRequest request)
        {
            Console.WriteLine($"send Access-Request packet: {request}");
            var response = Communicate(request, authPort);
            Console.WriteLine($"received packet: {response}");
            return response;
        }

        public RadiusPacket Account(AccountingRequest request)
        {
            Console.WriteLine($"send Accounting-Request packet: {request}");
            var response = Communicate(request, acctPort);
            Console.WriteLine($"received packet: {response}");
            return response;
        }

        public RadiusPacket Communicate(RadiusPacket request, int port)
        {
            byte[] packetIn = new byte[RadiusPacket.MAX_PACKET_LENGTH];
            byte[] packetOut = MakeDatagramPacket(request);

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = socketTimeout
            };

            var endPoint = new IPEndPoint(IPAddress.Parse(hostName), port);

            for (int i = 1; i <= retryCount; i++)
            {
                try
                {
                    socket.SendTo(packetOut, endPoint);
                    socket.Receive(packetIn);
                    return MakeRadiusPacket(packetIn, request);
                }
                catch (SocketException)
                {
                    if (i == retryCount)
                    {
                        Console.WriteLine("Communication failure (timeout), no more retries");
                        throw;
                    }
                    Console.WriteLine($"Communication failure, retry {i}");
                }
            }

            return null;
        }

        protected byte[] MakeDatagramPacket(RadiusPacket packet)
        {
            using (var memoryStream = new MemoryStream())
            {
                packet.EncodeRequestPacket(memoryStream, sharedSecret);
                return memoryStream.ToArray();
            }

        }

        protected RadiusPacket MakeRadiusPacket(byte[] packet, RadiusPacket request)
        {
            using(var inStream = new MemoryStream(packet))
                return RadiusPacket.DecodeResponsePacket(inStream, sharedSecret, request);
        }

        private static int ValidatePort(int port)
        {
            if (port < 1 || port > 65535)
                throw new ArgumentException("Invalid port number.");
            return port;
        }
    }
}
