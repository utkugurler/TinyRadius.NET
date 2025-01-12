using TinyRadius.NET.Attribute;

namespace TinyRadius.NET.Dictionary;

public class AttributeType
{
    private int vendorId = -1;
    private int typeCode;
    private string name;
    private Type attributeClass;
    private Dictionary<int, string> enumeration = null;

    /// <summary>
    /// Create a new attribute type.
    /// </summary>
    /// <param name="code">Radius attribute type code</param>
    /// <param name="name">Attribute type name</param>
    /// <param name="type">RadiusAttribute descendant who handles attributes of this type</param>
    public AttributeType(int code, string name, Type type)
    {
        SetTypeCode(code);
        SetName(name);
        SetAttributeClass(type);
    }

    /// <summary>
    /// Constructs a Vendor-Specific sub-attribute type.
    /// </summary>
    /// <param name="vendor">vendor ID</param>
    /// <param name="code">sub-attribute type code</param>
    /// <param name="name">sub-attribute name</param>
    /// <param name="type">sub-attribute class</param>
    public AttributeType(int vendor, int code, string name, Type type)
    {
        SetTypeCode(code);
        SetName(name);
        SetAttributeClass(type);
        SetVendorId(vendor);
    }

    /// <summary>
    /// Retrieves the Radius type code for this attribute type.
    /// </summary>
    /// <returns>Radius type code</returns>
    public int GetTypeCode()
    {
        return typeCode;
    }

    /// <summary>
    /// Sets the Radius type code for this attribute type.
    /// </summary>
    /// <param name="code">type code, 1-255</param>
    public void SetTypeCode(int code)
    {
        if (code < 1 || code > 255)
            throw new ArgumentException("code out of bounds");
        typeCode = code;
    }

    /// <summary>
    /// Retrieves the name of this type.
    /// </summary>
    /// <returns>name</returns>
    public string GetName()
    {
        return name;
    }

    /// <summary>
    /// Sets the name of this type.
    /// </summary>
    /// <param name="name">type name</param>
    public void SetName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("name is empty");
        this.name = name;
    }

    /// <summary>
    /// Retrieves the RadiusAttribute descendant class which represents attributes of this type.
    /// </summary>
    /// <returns>class</returns>
    public Type GetAttributeClass()
    {
        return attributeClass;
    }

    /// <summary>
    /// Sets the RadiusAttribute descendant class which represents attributes of this type.
    /// </summary>
    /// <param name="type">type class</param>
    public void SetAttributeClass(Type type)
    {
        if (type == null)
            throw new ArgumentNullException("type is null");
        if (!typeof(RadiusAttribute).IsAssignableFrom(type))
            throw new ArgumentException("type is not a RadiusAttribute descendant");
        attributeClass = type;
    }

    /// <summary>
    /// Returns the vendor ID. No vendor specific attribute = -1
    /// </summary>
    /// <returns>vendor ID</returns>
    public int GetVendorId()
    {
        return vendorId;
    }

    /// <summary>
    /// Sets the vendor ID.
    /// </summary>
    /// <param name="vendorId">vendor ID</param>
    public void SetVendorId(int vendorId)
    {
        this.vendorId = vendorId;
    }

    /// <summary>
    /// Returns the name of the given integer value if this attribute is an enumeration, 
    /// or null if it is not or if the integer value is unknown.
    /// </summary>
    /// <returns>name</returns>
    public string GetEnumeration(int value)
    {
        if (enumeration != null)
        {
            enumeration.TryGetValue(value, out string result);
            return result;
        }
        return null;
    }

    /// <summary>
    /// Returns the number of the given string value if this attribute is an enumeration, 
    /// or null if it is not or if the string value is unknown.
    /// </summary>
    /// <param name="value">string value</param>
    /// <returns>Integer or null</returns>
    public int? GetEnumeration(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("value is empty");
        if (enumeration == null)
            return null;

        foreach (var kvp in enumeration)
        {
            if (kvp.Value.Equals(value))
                return kvp.Key;
        }
        return null;
    }

    /// <summary>
    /// Adds a name for an integer value of this attribute.
    /// </summary>
    /// <param name="num">number that shall get a name</param>
    /// <param name="name">the name for this number</param>
    public void AddEnumerationValue(int num, string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("name is empty");
        if (enumeration == null)
            enumeration = new Dictionary<int, string>();
        enumeration[num] = name;
    }

    /// <summary>
    /// String representation of AttributeType object for debugging purposes.
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        string s = $"{GetTypeCode()}/{GetName()}: {attributeClass.FullName}";
        if (GetVendorId() != -1)
            s += $" (vendor {GetVendorId()})";
        return s;
    }
}