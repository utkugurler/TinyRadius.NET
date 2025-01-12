using System.Net;
using TinyRadius.NET.Utils;

namespace TinyRadius.NET.Attribute;


/// <summary>
/// This class represents a Radius attribute for an IPv6 number.
/// </summary>
public class Ipv6Attribute : RadiusAttribute
{
    /// <summary>
    /// Constructs an empty IPv6 attribute.
    /// </summary>
    public Ipv6Attribute() : base()
    {
    }

    /// <summary>
    /// Constructs an IPv6 attribute.
    /// </summary>
    /// <param name="type">Attribute type code</param>
    /// <param name="value">Value, format: IPv6 address</param>
    public Ipv6Attribute(int type, string value)
    {
        SetAttributeType(type);
        SetAttributeValue(value);
    }

    /// <summary>
    /// Returns the attribute value (IPv6 number) as a string of the format IPv6 address.
    /// </summary>
    /// <returns>IPv6 address as a string</returns>
    public override string GetAttributeValue()
    {
        byte[] data = GetAttributeData();
        if (data == null || data.Length != 16)
            throw new InvalidOperationException("IP attribute: expected 16 bytes attribute data");
        try
        {
            IPAddress addr = new IPAddress(data);
            return addr.ToString();
        }
        catch (Exception e)
        {
            throw new ArgumentException("Bad IPv6 address", e);
        }
    }

    /// <summary>
    /// Sets the attribute value (IPv6 number). String format: IPv6 address.
    /// </summary>
    /// <param name="value">IPv6 address as a string</param>
    public override void SetAttributeValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 3)
            throw new ArgumentException("Bad IPv6 address: " + value);
        try
        {
            IPAddress addr = IPAddress.Parse(value);
            byte[] data = addr.GetAddressBytes();
            SetAttributeData(data);
        }
        catch (Exception e)
        {
            throw new ArgumentException("Bad IPv6 address: " + value, e);
        }
    }

    /// <summary>
    /// Check attribute length.
    /// </summary>
    /// <param name="data">Data buffer</param>
    /// <param name="offset">The offset to read</param>
    /// <param name="length">The amount of data to read</param>
    /// <exception cref="RadiusException">If length is not 18</exception>
    public override void ReadAttribute(byte[] data, int offset, int length)
    {
        if (length != 18)
            throw new RadiusException("IP attribute: expected 16 bytes data");
        base.ReadAttribute(data, offset, length);
    }
}
