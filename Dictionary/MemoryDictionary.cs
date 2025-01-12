namespace TinyRadius.NET.Dictionary
{
    // <summary>
    /// A dictionary that keeps the values and names in hash maps
    /// in the memory. The dictionary has to be filled using the
    /// methods <code>addAttributeType</code> and <code>addVendor</code>.
    /// </summary>
    public class MemoryDictionary : IWritableDictionary
    {
        private readonly Dictionary<int, string> vendorsByCode = new Dictionary<int, string>();
        private readonly Dictionary<int, Dictionary<int, AttributeType>> attributesByCode = new Dictionary<int, Dictionary<int, AttributeType>>();
        private readonly Dictionary<string, AttributeType> attributesByName = new Dictionary<string, AttributeType>();

        /// <summary>
        /// Returns the AttributeType for the vendor -1 from the cache.
        /// </summary>
        /// <param name="typeCode">Attribute type code</param>
        /// <returns>AttributeType or null</returns>
        public AttributeType GetAttributeTypeByCode(int typeCode)
        {
            return GetAttributeTypeByCode(-1, typeCode);
        }

        /// <summary>
        /// Returns the specified AttributeType object.
        /// </summary>
        /// <param name="vendorCode">Vendor ID or -1 for "no vendor"</param>
        /// <param name="typeCode">Attribute type code</param>
        /// <returns>AttributeType or null</returns>
        public AttributeType GetAttributeTypeByCode(int vendorCode, int typeCode)
        {
            if (attributesByCode.TryGetValue(vendorCode, out var vendorAttributes))
            {
                vendorAttributes.TryGetValue(typeCode, out var attributeType);
                return attributeType;
            }
            return null;
        }

        /// <summary>
        /// Retrieves the attribute type with the given name.
        /// </summary>
        /// <param name="typeName">Name of the attribute type</param>
        /// <returns>AttributeType or null</returns>
        public AttributeType GetAttributeTypeByName(string typeName)
        {
            attributesByName.TryGetValue(typeName, out var attributeType);
            return attributeType;
        }

        /// <summary>
        /// Searches the vendor with the given name and returns its code. This method is seldomly used.
        /// </summary>
        /// <param name="vendorName">Vendor name</param>
        /// <returns>Vendor code or -1</returns>
        public int GetVendorId(string vendorName)
        {
            foreach (var kvp in vendorsByCode)
            {
                if (kvp.Value == vendorName)
                {
                    return kvp.Key;
                }
            }
            return -1;
        }

        /// <summary>
        /// Retrieves the name of the vendor with the given code from the cache.
        /// </summary>
        /// <param name="vendorId">Vendor number</param>
        /// <returns>Vendor name or null</returns>
        public string GetVendorName(int vendorId)
        {
            vendorsByCode.TryGetValue(vendorId, out var vendorName);
            return vendorName;
        }

        /// <summary>
        /// Adds the given vendor to the cache.
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <param name="vendorName">Name of the vendor</param>
        /// <exception cref="ArgumentException">Empty vendor name, invalid vendor ID</exception>
        public void AddVendor(int vendorId, string vendorName)
        {
            if (vendorId < 0)
                throw new ArgumentException("Vendor ID must be positive");
            if (GetVendorName(vendorId) != null)
                throw new ArgumentException("Duplicate vendor code");
            if (string.IsNullOrEmpty(vendorName))
                throw new ArgumentException("Vendor name empty");

            vendorsByCode[vendorId] = vendorName;
        }

        /// <summary>
        /// Adds an AttributeType object to the cache.
        /// </summary>
        /// <param name="attributeType">AttributeType object</param>
        /// <exception cref="ArgumentException">Duplicate attribute name/type code</exception>
        public void AddAttributeType(AttributeType attributeType)
        {
            if (attributeType == null)
                throw new ArgumentException("Attribute type must not be null");

            int vendorId = attributeType.GetVendorId();
            int typeCode = attributeType.GetTypeCode();
            string attributeName = attributeType.GetName();

            if (attributesByName.ContainsKey(attributeName))
                throw new ArgumentException($"Duplicate attribute name: {attributeName}");

            if (!attributesByCode.TryGetValue(vendorId, out var vendorAttributes))
            {
                vendorAttributes = new Dictionary<int, AttributeType>();
                attributesByCode[vendorId] = vendorAttributes;
            }
            if (vendorAttributes.ContainsKey(typeCode))
                throw new ArgumentException($"Duplicate type code: {typeCode}");

            attributesByName[attributeName] = attributeType;
            vendorAttributes[typeCode] = attributeType;
        }
    }
}