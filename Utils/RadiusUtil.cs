using System.Text;

namespace TinyRadius.NET.Utils;

/// <summary>
/// This class contains miscellaneous static utility functions.
/// </summary>
public static class RadiusUtil
{
    /// <summary>
    /// Returns the passed string as a byte array containing the
    /// string in UTF-8 representation.
    /// </summary>
    /// <param name="str">C# string</param>
    /// <returns>UTF-8 byte array</returns>
    public static byte[] GetUtf8Bytes(string str)
    {
        try
        {
            return Encoding.UTF8.GetBytes(str);
        }
        catch (EncoderFallbackException)
        {
            return Encoding.Default.GetBytes(str);
        }
    }

    /// <summary>
    /// Creates a string from the passed byte array containing the
    /// string in UTF-8 representation.
    /// </summary>
    /// <param name="utf8">UTF-8 byte array</param>
    /// <returns>C# string</returns>
    public static string GetStringFromUtf8(byte[] utf8)
    {
        try
        {
            return Encoding.UTF8.GetString(utf8);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Default.GetString(utf8);
        }
    }

    /// <summary>
    /// Returns the byte array as a hex string in the format
    /// "0x1234".
    /// </summary>
    /// <param name="data">byte array</param>
    /// <returns>hex string</returns>
    public static string GetHexString(byte[] data)
    {
        StringBuilder hex = new StringBuilder("0x");
        if (data != null)
        {
            foreach (byte b in data)
            {
                string digit = b.ToString("x2");
                hex.Append(digit);
            }
        }
        return hex.ToString();
    }
}
