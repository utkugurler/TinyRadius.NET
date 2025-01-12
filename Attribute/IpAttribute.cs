using TinyRadius.NET.Utils;

namespace TinyRadius.NET.Attribute;

/// <summary>
/// This class represents a Radius attribute for an IP number.
/// </summary>
public class IpAttribute : RadiusAttribute
{
    /// <summary>
    /// Constructs an empty IP attribute.
    /// </summary>
    public IpAttribute() : base()
    {
    }

    /// <summary>
    /// Constructs an IP attribute.
    /// </summary>
    /// <param name="type">Attribute type code</param>
    /// <param name="value">Value, format: xx.xx.xx.xx</param>
    public IpAttribute(int type, string value)
    {
        SetAttributeType(type);
        SetAttributeValue(value);
    }

    /// <summary>
    /// Constructs an IP attribute.
    /// </summary>
    /// <param name="type">Attribute type code</param>
    /// <param name="ipNum">Value as a 32 bit unsigned int</param>
    public IpAttribute(int type, long ipNum)
    {
        SetAttributeType(type);
        SetIpAsLong(ipNum);
    }

    /// <summary>
    /// Returns the attribute value (IP number) as a string of the format "xx.xx.xx.xx".
    /// </summary>
    /// <returns>IP number as a string</returns>
    public override string GetAttributeValue()
    {
        byte[] data = GetAttributeData();
        if (data == null || data.Length != 4)
            throw new InvalidOperationException("IP attribute: expected 4 bytes attribute data");

        return string.Join(".", data.Select(b => (b & 0xFF).ToString()));
    }

    /// <summary>
    /// Sets the attribute value (IP number). String format: "xx.xx.xx.xx".
    /// </summary>
    /// <param name="value">IP number as a string</param>
    public sealed override void SetAttributeValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 7 || value.Length > 15)
            throw new ArgumentException("Bad IP number");

        var tokens = value.Split('.');
        if (tokens.Length != 4)
            throw new ArgumentException("Bad IP number: 4 numbers required");

        byte[] data = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            int num = int.Parse(tokens[i]);
            if (num < 0 || num > 255)
                throw new ArgumentException("Bad IP number: num out of bounds");
            data[i] = (byte)num;
        }

        SetAttributeData(data);
    }

    /// <summary>
    /// Returns the IP number as a 32 bit unsigned number. The number is returned in a long because C# does not support unsigned ints.
    /// </summary>
    /// <returns>IP number</returns>
    public long GetIpAsLong()
    {
        byte[] data = GetAttributeData();
        if (data == null || data.Length != 4)
            throw new InvalidOperationException("Expected 4 bytes attribute data");

        return ((long)(data[0] & 0x0FF) << 24) | ((data[1] & 0x0FF) << 16) |
               ((data[2] & 0x0FF) << 8) | (data[3] & 0x0FF);
    }

    /// <summary>
    /// Sets the IP number represented by this IpAttribute as a 32 bit unsigned number.
    /// </summary>
    /// <param name="ip">IP number</param>
    public void SetIpAsLong(long ip)
    {
        byte[] data = new byte[4];
        data[0] = (byte)((ip >> 24) & 0x0FF);
        data[1] = (byte)((ip >> 16) & 0x0FF);
        data[2] = (byte)((ip >> 8) & 0x0FF);
        data[3] = (byte)(ip & 0x0FF);
        SetAttributeData(data);
    }

    /// <summary>
    /// Check attribute length.
    /// </summary>
    /// <param name="data">Data buffer</param>
    /// <param name="offset">The offset to read</param>
    /// <param name="length">The amount of data to read</param>
    /// <exception cref="RadiusException">If length is not 6</exception>
    public override void ReadAttribute(byte[] data, int offset, int length)
    {
        if (length != 6)
            throw new RadiusException("IP attribute: expected 4 bytes data");
        base.ReadAttribute(data, offset, length);
    }
}

