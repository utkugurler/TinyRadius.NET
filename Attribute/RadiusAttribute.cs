using TinyRadius.NET.Dictionary;
using TinyRadius.NET.Utils;
using TinyRadius.Dictionary;

namespace TinyRadius.NET.Attribute
{
    /// <summary>
    /// This class represents a generic Radius attribute. Subclasses implement
    /// methods to access the fields of special attributes.
    /// </summary>
    public class RadiusAttribute
    {
        private IDictionary dictionary = DefaultDictionary.GetDefaultDictionary();
        private int attributeType = -1;
        private int vendorId = -1;
        private byte[] attributeData = null;

        /// <summary>
        /// Constructs an empty Radius attribute.
        /// </summary>
        public RadiusAttribute() { }

        /// <summary>
        /// Constructs a Radius attribute with the specified type and data.
        /// </summary>
        /// <param name="type">Attribute type</param>
        /// <param name="data">Attribute data</param>
        public RadiusAttribute(int type, byte[] data)
        {
            SetAttributeType(type);
            SetAttributeData(data);
        }

        /// <summary>
        /// Returns the data for this attribute.
        /// </summary>
        /// <returns>Attribute data</returns>
        public byte[] GetAttributeData()
        {
            return attributeData;
        }

        /// <summary>
        /// Sets the data for this attribute.
        /// </summary>
        /// <param name="attributeData">Attribute data</param>
        public void SetAttributeData(byte[] attributeData)
        {
            if (attributeData == null)
                throw new ArgumentNullException(nameof(attributeData), "attribute data is null");
            this.attributeData = attributeData;
        }

        /// <summary>
        /// Returns the type of this Radius attribute.
        /// </summary>
        /// <returns>Type code, 0-255</returns>
        public int GetAttributeType()
        {
            return attributeType;
        }

        /// <summary>
        /// Sets the type of this Radius attribute.
        /// </summary>
        /// <param name="attributeType">Type code, 0-255</param>
        public void SetAttributeType(int attributeType)
        {
            if (attributeType < 0 || attributeType > 255)
                throw new ArgumentException("attribute type invalid: " + attributeType);
            this.attributeType = attributeType;
        }

        /// <summary>
        /// Sets the value of the attribute using a string.
        /// </summary>
        /// <param name="value">Value as a string</param>
        public virtual void SetAttributeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new NotSupportedException("cannot set the value of attribute " + attributeType + " as a string");
            SetAttributeData(System.Text.Encoding.Default.GetBytes(value));
        }

        /// <summary>
        /// Gets the value of this attribute as a string.
        /// </summary>
        /// <returns>Value</returns>
        public virtual string GetAttributeValue()
        {
            return RadiusUtil.GetHexString(GetAttributeData());
        }

        /// <summary>
        /// Gets the Vendor-Id of the Vendor-Specific attribute this attribute belongs to. Returns -1 if this attribute is not a sub attribute of a Vendor-Specific attribute.
        /// </summary>
        /// <returns>Vendor ID</returns>
        public int GetVendorId()
        {
            return vendorId;
        }

        /// <summary>
        /// Sets the Vendor-Id of the Vendor-Specific attribute this attribute belongs to. The default value of -1 means this attribute is not a sub attribute of a Vendor-Specific attribute.
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        public void SetVendorId(int vendorId)
        {
            this.vendorId = vendorId;
        }

        /// <summary>
        /// Returns the dictionary this Radius attribute uses.
        /// </summary>
        /// <returns>Dictionary instance</returns>
        public IDictionary GetDictionary()
        {
            return dictionary;
        }

        /// <summary>
        /// Sets a custom dictionary to use. If no dictionary is set, the default dictionary is used.
        /// </summary>
        /// <param name="dictionary">Dictionary class to use</param>
        public virtual void SetDictionary(IDictionary dictionary)
        {
            this.dictionary = dictionary;
        }

        /// <summary>
        /// Returns this attribute encoded as a byte array.
        /// </summary>
        /// <returns>Attribute</returns>
        public virtual byte[] WriteAttribute()
        {
            if (GetAttributeType() == -1)
                throw new ArgumentException("attribute type not set");
            if (attributeData == null)
                throw new ArgumentNullException(nameof(attributeData), "attribute data not set");

            byte[] attr = new byte[2 + attributeData.Length];
            attr[0] = (byte)GetAttributeType();
            attr[1] = (byte)(2 + attributeData.Length);
            Array.Copy(attributeData, 0, attr, 2, attributeData.Length);
            return attr;
        }

        /// <summary>
        /// Reads in this attribute from the passed byte array.
        /// </summary>
        /// <param name="data">Data buffer</param>
        /// <param name="offset">The offset to read</param>
        /// <param name="length">The amount of data to read</param>
        /// <exception cref="RadiusException">When length is less than 2</exception>
        public virtual void ReadAttribute(byte[] data, int offset, int length)
        {
            if (length < 2)
                throw new RadiusException("attribute length too small: " + length);
            int attrType = data[offset] & 0x0ff;
            int attrLen = data[offset + 1] & 0x0ff;
            byte[] attrData = new byte[attrLen - 2];
            Array.Copy(data, offset + 2, attrData, 0, attrLen - 2);
            SetAttributeType(attrType);
            SetAttributeData(attrData);
        }

        /// <summary>
        /// String representation for debugging purposes.
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            string name;

            // determine attribute name
            AttributeType at = GetAttributeTypeObject();
            if (at != null)
                name = at.GetName();
            else if (GetVendorId() != -1)
                name = "Unknown-Sub-Attribute-" + GetAttributeType();
            else
                name = "Unknown-Attribute-" + GetAttributeType();

            // indent sub attributes
            if (GetVendorId() != -1)
                name = "  " + name;

            return name + ": " + GetAttributeValue();
        }

        /// <summary>
        /// Retrieves an AttributeType object for this attribute.
        /// </summary>
        /// <returns>AttributeType object for (sub-)attribute or null</returns>
        public AttributeType GetAttributeTypeObject()
        {
            if (GetVendorId() != -1)
            {
                return dictionary.GetAttributeTypeByCode(GetVendorId(), GetAttributeType());
            }
            return dictionary.GetAttributeTypeByCode(GetAttributeType());
        }

        /// <summary>
        /// Creates a RadiusAttribute object of the appropriate type.
        /// </summary>
        /// <param name="dictionary">Dictionary to use</param>
        /// <param name="vendorId">Vendor ID or -1</param>
        /// <param name="attributeType">Attribute type</param>
        /// <returns>RadiusAttribute object</returns>
        public static RadiusAttribute CreateRadiusAttribute(IDictionary dictionary, int vendorId, int attributeType)
        {
            RadiusAttribute attribute = new RadiusAttribute();

            AttributeType at = dictionary.GetAttributeTypeByCode(vendorId, attributeType);
            if (at != null && at.GetAttributeClass() != null)
            {
                try
                {
                    attribute = (RadiusAttribute)Activator.CreateInstance(at.GetAttributeClass());
                }
                catch (Exception)
                {
                    // error instantiating class - should not occur
                }
            }

            attribute.SetAttributeType(attributeType);
            attribute.SetDictionary(dictionary);
            attribute.SetVendorId(vendorId);
            return attribute;
        }

        /// <summary>
        /// Creates a Radius attribute, including vendor-specific attributes. The default dictionary is used.
        /// </summary>
        /// <param name="vendorId">Vendor ID or -1</param>
        /// <param name="attributeType">Attribute type</param>
        /// <returns>RadiusAttribute instance</returns>
        public static RadiusAttribute CreateRadiusAttribute(int vendorId, int attributeType)
        {
            IDictionary dictionary = DefaultDictionary.GetDefaultDictionary();
            return CreateRadiusAttribute(dictionary, vendorId, attributeType);
        }

        /// <summary>
        /// Creates a Radius attribute. The default dictionary is used.
        /// </summary>
        /// <param name="attributeType">Attribute type</param>
        /// <returns>RadiusAttribute instance</returns>
        public static RadiusAttribute CreateRadiusAttribute(int attributeType)
        {
            IDictionary dictionary = DefaultDictionary.GetDefaultDictionary();
            return CreateRadiusAttribute(dictionary, -1, attributeType);
        }
    }
}


