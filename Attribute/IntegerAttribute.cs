using TinyRadius.NET.Dictionary;
using TinyRadius.NET.Utils;

namespace TinyRadius.NET.Attribute
{
    public class IntegerAttribute : RadiusAttribute
    {
        public IntegerAttribute() : base() { }
    
        public IntegerAttribute(int type, int value)
        {
            SetAttributeType(type);
            SetAttributeValue(value);
        }
    
        public int GetAttributeValueInt()
        {
            byte[] data = GetAttributeData();
            return ((data[0] & 0xFF) << 24) | ((data[1] & 0xFF) << 16) |
                   ((data[2] & 0xFF) << 8) | (data[3] & 0xFF);
        }
    
        public override string GetAttributeValue()
        {
            int value = GetAttributeValueInt();
            AttributeType at = GetAttributeTypeObject();
            if (at != null)
            {
                string name = at.GetEnumeration(value);
                if (name != null)
                    return name;
            }
            return ((long)value & 0xFFFFFFFFL).ToString();
        }
    
        public void SetAttributeValue(int value)
        {
            byte[] data = new byte[4];
            data[0] = (byte)((value >> 24) & 0xFF);
            data[1] = (byte)((value >> 16) & 0xFF);
            data[2] = (byte)((value >> 8) & 0xFF);
            data[3] = (byte)(value & 0xFF);
            SetAttributeData(data);
        }
    
        public override void SetAttributeValue(string value)
        {
            AttributeType at = GetAttributeTypeObject();
            if (at != null)
            {
                int? val = at.GetEnumeration(value);
                if (val.HasValue)
                {
                    SetAttributeValue(val.Value);
                    return;
                }
            }
            SetAttributeValue((int)long.Parse(value));
        }
    
        public override void ReadAttribute(byte[] data, int offset, int length)
        {
            if (length != 6)
                throw new RadiusException("integer attribute: expected 4 bytes data");
            base.ReadAttribute(data, offset, length);
        }
    }
}


