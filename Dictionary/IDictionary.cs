namespace TinyRadius.NET.Dictionary
{
    public interface IDictionary
    {
        AttributeType GetAttributeTypeByCode(int typeCode);
        AttributeType GetAttributeTypeByCode(int vendorCode, int typeCode);
        AttributeType GetAttributeTypeByName(string typeName);
        int GetVendorId(string vendorName);
        string GetVendorName(int vendorId);
    }
}

