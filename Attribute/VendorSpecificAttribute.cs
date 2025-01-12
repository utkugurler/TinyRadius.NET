using TinyRadius.NET.Dictionary;
using TinyRadius.NET.Utils;

namespace TinyRadius.NET.Attribute
{
    public class VendorSpecificAttribute : RadiusAttribute
    {
        /// <summary>
        /// Radius attribute type code for Vendor-Specific
        /// </summary>
        public static readonly int VENDOR_SPECIFIC = 26;
        
        /// <summary>
        /// Sub attributes. Only set if isRawData == false.
        /// </summary>
        private List<RadiusAttribute> subAttributes = new List<RadiusAttribute>();

        /// <summary>
        /// Vendor ID of sub-attributes.
        /// </summary>
        private int childVendorId;

        /// <summary>
        /// Constructs an empty Vendor-Specific attribute that can be read from a Radius packet.
        /// </summary>
        public VendorSpecificAttribute() : base()
        {
        }

        /// <summary>
        /// Constructs a new Vendor-Specific attribute to be sent.
        /// </summary>
        /// <param name="vendorId">Vendor ID of the sub-attributes</param>
        public VendorSpecificAttribute(int vendorId)
        {
            SetAttributeType(VENDOR_SPECIFIC);
            SetChildVendorId(vendorId);
        }

        /// <summary>
        /// Sets the vendor ID of the child attributes.
        /// </summary>
        /// <param name="childVendorId">Vendor ID of the child attributes</param>
        public void SetChildVendorId(int childVendorId)
        {
            this.childVendorId = childVendorId;
        }

        /// <summary>
        /// Returns the vendor ID of the sub-attributes.
        /// </summary>
        /// <returns>Vendor ID of sub attributes</returns>
        public int GetChildVendorId()
        {
            return childVendorId;
        }

        /// <summary>
        /// Also copies the new dictionary to sub-attributes.
        /// </summary>
        /// <param name="dictionary">Dictionary to set</param>
        public override void SetDictionary(IDictionary dictionary)
        {
            base.SetDictionary(dictionary);
            foreach (RadiusAttribute attr in subAttributes)
            {
                attr.SetDictionary(dictionary);
            }
        }

        /// <summary>
        /// Adds a sub-attribute to this attribute.
        /// </summary>
        /// <param name="attribute">Sub-attribute to add</param>
        public void AddSubAttribute(RadiusAttribute attribute)
        {
            if (attribute.GetVendorId() != GetChildVendorId())
                throw new ArgumentException("Sub attribute has incorrect vendor ID");

            subAttributes.Add(attribute);
        }

        /// <summary>
        /// Adds a sub-attribute with the specified name to this attribute.
        /// </summary>
        /// <param name="name">Name of the sub-attribute</param>
        /// <param name="value">Value of the sub-attribute</param>
        public void AddSubAttribute(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Type name is empty");
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value is empty");

            AttributeType type = GetDictionary().GetAttributeTypeByName(name);
            if (type == null)
                throw new ArgumentException($"Unknown attribute type '{name}'");
            if (type.GetVendorId() == -1)
                throw new ArgumentException($"Attribute type '{name}' is not a Vendor-Specific sub-attribute");
            if (type.GetVendorId() != GetChildVendorId())
                throw new ArgumentException($"Attribute type '{name}' does not belong to vendor ID {GetChildVendorId()}");

            RadiusAttribute attribute = CreateRadiusAttribute(GetDictionary(), GetChildVendorId(), type.GetTypeCode());
            attribute.SetAttributeValue(value);
            AddSubAttribute(attribute);
        }

        /// <summary>
        /// Removes the specified sub-attribute from this attribute.
        /// </summary>
        /// <param name="attribute">RadiusAttribute to remove</param>
        public void RemoveSubAttribute(RadiusAttribute attribute)
        {
            if (!subAttributes.Remove(attribute))
                throw new ArgumentException("No such attribute");
        }

        /// <summary>
        /// Returns the list of sub-attributes.
        /// </summary>
        /// <returns>List of RadiusAttribute objects</returns>
        public List<RadiusAttribute> GetSubAttributes()
        {
            return subAttributes;
        }

        /// <summary>
        /// Returns all sub-attributes of this attribute which have the given type.
        /// </summary>
        /// <param name="attributeType">Type of sub-attributes to get</param>
        /// <returns>List of RadiusAttribute objects, does not return null</returns>
        public List<RadiusAttribute> GetSubAttributes(int attributeType)
        {
            if (attributeType < 1 || attributeType > 255)
                throw new ArgumentException("Sub-attribute type out of bounds");

            return subAttributes.Where(a => a.GetAttributeType() == attributeType).ToList();
        }

        /// <summary>
        /// Returns a sub-attribute of the given type which may only occur once in this attribute.
        /// </summary>
        /// <param name="type">Sub-attribute type</param>
        /// <returns>RadiusAttribute object or null if there is no such sub-attribute</returns>
        public RadiusAttribute GetSubAttribute(int type)
        {
            List<RadiusAttribute> attrs = GetSubAttributes(type);
            if (attrs.Count > 1)
                throw new InvalidOperationException($"Multiple sub-attributes of requested type {type}");
            else if (attrs.Count == 0)
                return null;
            else
                return attrs[0];
        }

        /// <summary>
        /// Returns a single sub-attribute of the given type name.
        /// </summary>
        /// <param name="type">Attribute type name</param>
        /// <returns>RadiusAttribute object or null if there is no such attribute</returns>
        public RadiusAttribute GetSubAttribute(string type)
        {
            if (string.IsNullOrEmpty(type))
                throw new ArgumentException("Type name is empty");

            AttributeType t = GetDictionary().GetAttributeTypeByName(type);
            if (t == null)
                throw new ArgumentException($"Unknown attribute type name '{type}'");
            if (t.GetVendorId() != GetChildVendorId())
                throw new ArgumentException("Vendor ID mismatch");

            return GetSubAttribute(t.GetTypeCode());
        }

        /// <summary>
        /// Returns the value of the Radius attribute of the given type or null if there is no such attribute.
        /// </summary>
        /// <param name="type">Attribute type name</param>
        /// <returns>Value of the attribute as a string or null if there is no such attribute</returns>
        public string GetSubAttributeValue(string type)
        {
            RadiusAttribute attr = GetSubAttribute(type);
            return attr?.GetAttributeValue();
        }

        /// <summary>
        /// Renders this attribute as a byte array.
        /// </summary>
        /// <returns>Attribute as a byte array</returns>
        public override byte[] WriteAttribute()
        {
            // write vendor ID
            using (var bos = new MemoryStream())
            {
                bos.WriteByte((byte)(GetChildVendorId() >> 24 & 0x0ff));
                bos.WriteByte((byte)(GetChildVendorId() >> 16 & 0x0ff));
                bos.WriteByte((byte)(GetChildVendorId() >> 8 & 0x0ff));
                bos.WriteByte((byte)(GetChildVendorId() & 0x0ff));

                // write sub-attributes
                foreach (RadiusAttribute a in subAttributes)
                {
                    bos.Write(a.WriteAttribute(), 0, a.WriteAttribute().Length);
                }

                // check data length
                byte[] attrData = bos.ToArray();
                int len = attrData.Length;
                if (len > 253)
                    throw new InvalidOperationException("Vendor-Specific attribute too long: " + bos.Length);

                // compose attribute
                byte[] attr = new byte[len + 2];
                attr[0] = (byte)VENDOR_SPECIFIC; // code
                attr[1] = (byte)(len + 2); // length
                Array.Copy(attrData, 0, attr, 2, len);
                return attr;
            }
        }

        /// <summary>
        /// Reads a Vendor-Specific attribute and decodes the internal sub-attribute structure.
        /// </summary>
        /// <param name="data">Data buffer</param>
        /// <param name="offset">The offset to read</param>
        /// <param name="length">The amount of data to read</param>
        public override void ReadAttribute(byte[] data, int offset, int length)
        {
            // check length
            if (length < 6)
                throw new RadiusException("Vendor-Specific attribute too short: " + length);

            int vsaCode = data[offset];
            int vsaLen = (data[offset + 1] & 0x000000ff) - 6;

            if (vsaCode != VENDOR_SPECIFIC)
                throw new RadiusException("Not a Vendor-Specific attribute");

            // read vendor ID and vendor data
            int vendorId = (UnsignedByteToInt(data[offset + 2]) << 24 |
                            UnsignedByteToInt(data[offset + 3]) << 16 |
                            UnsignedByteToInt(data[offset + 4]) << 8 |
                            UnsignedByteToInt(data[offset + 5]));
            SetChildVendorId(vendorId);

            // validate sub-attribute structure
            int pos = 0;
            int count = 0;
            while (pos < vsaLen)
            {
                if (pos + 1 >= vsaLen)
                    throw new RadiusException("Vendor-Specific attribute malformed");
                int vsaSubLen = data[(offset + 6) + pos + 1] & 0x0ff;
                pos += vsaSubLen;
                count++;
            }
            if (pos != vsaLen)
                throw new RadiusException("Vendor-Specific attribute malformed");

            subAttributes = new List<RadiusAttribute>(count);
            pos = 0;
            while (pos < vsaLen)
            {
                int subtype = data[(offset + 6) + pos] & 0x0ff;
                int sublength = data[(offset + 6) + pos + 1] & 0x0ff;
                RadiusAttribute a = CreateRadiusAttribute(GetDictionary(), vendorId, subtype);
                a.ReadAttribute(data, (offset + 6) + pos, sublength);
                subAttributes.Add(a);
                pos += sublength;
            }
        }

        private static int UnsignedByteToInt(byte b)
        {
            return b & 0xFF;
        }

        /// <summary>
        /// Returns a string representation for debugging.
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Vendor-Specific: ");
            int vendorId = GetChildVendorId();
            string vendorName = GetDictionary().GetVendorName(vendorId);
            if (vendorName != null)
            {
                sb.Append(vendorName);
                sb.Append(" (");
                sb.Append(vendorId);
                sb.Append(")");
            }
            else
            {
                sb.Append("vendor ID ");
                sb.Append(vendorId);
            }
            foreach (RadiusAttribute attr in GetSubAttributes())
            {
                sb.Append("\n");
                sb.Append(attr.ToString());
            }
            return sb.ToString();
        }
    }
}

