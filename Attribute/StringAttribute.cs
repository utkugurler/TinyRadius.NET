using System.Text;

namespace TinyRadius.NET.Attribute;

public class StringAttribute : RadiusAttribute
{
    public StringAttribute() : base() { }

    public StringAttribute(int type, string value)
    {
        SetAttributeType(type);
        SetAttributeValue(value);
    }

    public override string GetAttributeValue()
    {
        return Encoding.UTF8.GetString(GetAttributeData());
    }

    public void SetAttributeValue(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "string value not set");
        SetAttributeData(Encoding.UTF8.GetBytes(value));
    }
}
