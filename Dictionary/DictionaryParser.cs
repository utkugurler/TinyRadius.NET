using System.Text;
using System.Text.RegularExpressions;
using TinyRadius.NET.Attribute;

namespace TinyRadius.NET.Dictionary
{
    public class DictionaryParser
    {
        /// <summary>
        /// Returns a new dictionary filled with the contents from the given input stream.
        /// </summary>
        /// <param name="source">input stream</param>
        /// <returns>dictionary object</returns>
        /// <exception cref="IOException"></exception>
        public static IDictionary ParseDictionary(Stream source)
        {
            IWritableDictionary d = new MemoryDictionary();
            ParseDictionary(source, d);
            return d;
        }

        /// <summary>
        /// Parses the dictionary from the specified InputStream.
        /// </summary>
        /// <param name="source">input stream</param>
        /// <param name="dictionary">dictionary data is written to</param>
        /// <exception cref="IOException">syntax errors</exception>
        /// <exception cref="ArgumentException">syntax errors</exception>
        public static void ParseDictionary(Stream source, IWritableDictionary dictionary)
        {
            using (StreamReader inStream = new StreamReader(source, Encoding.UTF8))
            {
                string line;
                int lineNum = -1;
                while ((line = inStream.ReadLine()) != null)
                {
                    // ignore comments
                    lineNum++;
                    line = line.Trim();
                    if (line.StartsWith("#") || line.Length == 0)
                        continue;

                    // tokenize line by whitespace
                    var tok = new List<string>(Regex.Split(line, @"\s+"));
                    if (tok.Count == 0)
                        continue;

                    string lineType = tok[0].Trim();
                    if (lineType.Equals("ATTRIBUTE", StringComparison.OrdinalIgnoreCase))
                        ParseAttributeLine(dictionary, tok, lineNum);
                    else if (lineType.Equals("VALUE", StringComparison.OrdinalIgnoreCase))
                        ParseValueLine(dictionary, tok, lineNum);
                    else if (lineType.Equals("$INCLUDE", StringComparison.OrdinalIgnoreCase))
                        IncludeDictionaryFile(dictionary, tok, lineNum);
                    else if (lineType.Equals("VENDORATTR", StringComparison.OrdinalIgnoreCase))
                        ParseVendorAttributeLine(dictionary, tok, lineNum);
                    else if (lineType.Equals("VENDOR", StringComparison.OrdinalIgnoreCase))
                        ParseVendorLine(dictionary, tok, lineNum);
                    else
                        throw new IOException("unknown line type: " + lineType + " line: " + lineNum);
                }
            }
        }

        private static void ParseAttributeLine(IWritableDictionary dictionary, List<string> tok, int lineNum)
        {
            if (tok.Count != 4)
                throw new IOException("syntax error on line " + lineNum);

            // read name, code, type
            string name = tok[1].Trim();
            int code = int.Parse(tok[2]);
            string typeStr = tok[3].Trim();

            // translate type to class
            Type type = (code == VendorSpecificAttribute.VENDOR_SPECIFIC) ? typeof(VendorSpecificAttribute) : GetAttributeTypeClass(typeStr);

            // create and cache object
            dictionary.AddAttributeType(new AttributeType(code, name, type));
        }

        private static void ParseValueLine(IWritableDictionary dictionary, List<string> tok, int lineNum)
        {
            if (tok.Count != 4)
                throw new IOException("syntax error on line " + lineNum);

            string typeName = tok[1].Trim();
            string enumName = tok[2].Trim();
            string valStr = tok[3].Trim();

            AttributeType at = dictionary.GetAttributeTypeByName(typeName);
            if (at == null)
                throw new IOException("unknown attribute type: " + typeName + ", line: " + lineNum);

            at.AddEnumerationValue(int.Parse(valStr), enumName);
        }

        private static void ParseVendorAttributeLine(IWritableDictionary dictionary, List<string> tok, int lineNum)
        {
            if (tok.Count != 5)
                throw new IOException("syntax error on line " + lineNum);

            string vendor = tok[1].Trim();
            string name = tok[2].Trim();
            int code = int.Parse(tok[3].Trim());
            string typeStr = tok[4].Trim();

            Type type = GetAttributeTypeClass(typeStr);
            AttributeType at = new AttributeType(int.Parse(vendor), code, name, type);
            dictionary.AddAttributeType(at);
        }

        private static void ParseVendorLine(IWritableDictionary dictionary, List<string> tok, int lineNum)
        {
            if (tok.Count != 3)
                throw new IOException("syntax error on line " + lineNum);

            int vendorId = int.Parse(tok[1].Trim());
            string vendorName = tok[2].Trim();

            dictionary.AddVendor(vendorId, vendorName);
        }

        private static void IncludeDictionaryFile(IWritableDictionary dictionary, List<string> tok, int lineNum)
        {
            if (tok.Count != 2)
                throw new IOException("syntax error on line " + lineNum);

            string includeFile = tok[1];

            if (!File.Exists(includeFile))
                throw new IOException("included file '" + includeFile + "' not found, line " + lineNum);

            using (FileStream fs = new FileStream(includeFile, FileMode.Open, FileAccess.Read))
            {
                ParseDictionary(fs, dictionary);
            }
        }

        private static Type GetAttributeTypeClass(string typeStr)
        {
            switch (typeStr.ToLower())
            {
                case "string":
                    return typeof(StringAttribute);
                case "octets":
                    return typeof(RadiusAttribute);
                case "integer":
                case "date":
                    return typeof(IntegerAttribute);
                case "ipaddr":
                    return typeof(IpAttribute);
                case "ipv6addr":
                    return typeof(Ipv6Attribute);
                case "ipv6prefix":
                    return typeof(Ipv6PrefixAttribute);
                default:
                    throw new ArgumentException("Unknown attribute type: " + typeStr);
            }
        }
    }
}