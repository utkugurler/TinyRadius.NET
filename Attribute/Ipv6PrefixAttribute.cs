using System.Net;
using System.Net.Sockets;
using TinyRadius.NET.Utils;

namespace TinyRadius.NET.Attribute;

public class Ipv6PrefixAttribute : RadiusAttribute
{
    /// <summary>
    /// Constructs an empty IPv6 attribute.
    /// </summary>
    public Ipv6PrefixAttribute() : base()
    {
    }

    /// <summary>
    /// Constructs an IPv6 prefix attribute.
    /// </summary>
    /// <param name="type">Attribute type code</param>
    /// <param name="value">Value, format: "ipv6 address/prefix"</param>
    public Ipv6PrefixAttribute(int type, string value)
    {
        SetAttributeType(type);
        SetAttributeValue(value);
    }

    /// <summary>
    /// Returns the attribute value (IP number) as a string of the format "xx.xx.xx.xx".
    /// </summary>
    /// <returns>IPv6 prefix as a string</returns>
    public override string GetAttributeValue()
    {
        var data = GetAttributeData();
        if (data == null)
            throw new InvalidOperationException("ipv6 prefix attribute: expected 2-18 bytes attribute data and got null.");
        if (data.Length < 2 || data.Length > 18)
            throw new InvalidOperationException($"ipv6 prefix attribute: expected 2-18 bytes attribute data and got {data.Length}");

        try
        {
            var prefixSize = data[1] & 0xff;
            var prefix = data.Skip(2).ToArray();
            if (prefix.Length < 16)
            {
                // Pad with trailing 0's if length not 128 bits
                Array.Resize(ref prefix, 16);
            }
            var ipv6Prefix = new IPAddress(prefix);
            return ipv6Prefix.ToString() + "/" + prefixSize;
        }
        catch (Exception e)
        {
            throw new ArgumentException("bad IPv6 prefix", e);
        }
    }

    /// <summary>
    /// Sets the attribute value (IPv6 number/prefix). String format: "ipv6 address/prefix".
    /// </summary>
    /// <param name="value">IPv6 address and prefix as a string</param>
    public override void SetAttributeValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 3)
            throw new ArgumentException("bad IPv6 address : " + value);

        try
        {
            var address = IPAddress.Parse(value.Split('/')[0]);
            var prefixLength = int.Parse(value.Split('/')[1]);

            if (address.AddressFamily != AddressFamily.InterNetworkV6)
                throw new ArgumentException("bad IPv6 address : " + value);

            var data = new byte[18];
            data[0] = 0;
            data[1] = (byte)(prefixLength & 0xff);

            var ipData = address.GetAddressBytes();
            Array.Copy(ipData, 0, data, 2, ipData.Length);

            SetAttributeData(data);
        }
        catch (Exception e)
        {
            throw new ArgumentException("bad IPv6 address : " + value, e);
        }
    }

    /// <summary>
    /// Check attribute length.
    /// </summary>
    /// <param name="data">Data buffer</param>
    /// <param name="offset">The offset to read</param>
    /// <param name="length">The amount of data to read</param>
    /// <exception cref="RadiusException">If length is not between 4 and 20</exception>
    public override void ReadAttribute(byte[] data, int offset, int length)
    {
        if (length > 20 || length < 4)
            throw new RadiusException("IPv6 prefix attribute: expected 4-20 bytes data");
        base.ReadAttribute(data, offset, length);
    }
}