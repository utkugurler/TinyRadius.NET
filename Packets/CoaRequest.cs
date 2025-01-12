using System.Security.Cryptography;
using System.Text;
using TinyRadius.NET.Utils;

namespace TinyRadius.NET.Packets;

/// <summary>
/// CoA packet. Thanks to Michael Krastev.
/// </summary>
public class CoaRequest : RadiusPacket
{
    public CoaRequest() : this(COA_REQUEST)
    {
    }

    public CoaRequest(int type) : base(type, GetNextPacketIdentifier())
    {
    }

    /// <summary>
    /// Updates the request authenticator.
    /// </summary>
    /// <param name="sharedSecret">Shared secret</param>
    /// <param name="packetLength">Packet length</param>
    /// <param name="attributes">Attributes</param>
    /// <returns>Updated authenticator</returns>
    protected override byte[] UpdateRequestAuthenticator(string sharedSecret, int packetLength, byte[] attributes)
    {
        byte[] authenticator = new byte[16];
        
        using (var md5 = MD5.Create())
        {
            byte[] buffer = new byte[4 + 16 + attributes.Length + sharedSecret.Length];
            int offset = 0;

            // Code (1 byte)
            buffer[offset++] = (byte)PacketType;
            // ID (1 byte)
            buffer[offset++] = (byte)PacketIdentifier;
            // Length (2 bytes) - in network byte order (big-endian)
            buffer[offset++] = (byte)(packetLength >> 8);
            buffer[offset++] = (byte)(packetLength & 0xff);
            
            // Zero authenticator (16 bytes)
            offset += 16;
            
            // Attributes
            Buffer.BlockCopy(attributes, 0, buffer, offset, attributes.Length);
            offset += attributes.Length;
            
            // Shared secret
            byte[] secretBytes = Encoding.UTF8.GetBytes(sharedSecret);
            Buffer.BlockCopy(secretBytes, 0, buffer, offset, secretBytes.Length);

            return authenticator = md5.ComputeHash(buffer);
        }
    }

}
