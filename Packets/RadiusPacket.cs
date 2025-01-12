using System.Security.Cryptography;
using System.Text;
using TinyRadius.NET.Attribute;
using TinyRadius.NET.Dictionary;
using TinyRadius.NET.Utils;
using TinyRadius.Dictionary;

namespace TinyRadius.NET.Packets
{
    public class RadiusPacket
    {
        public const int ACCESS_REQUEST = 1;
        public const int ACCESS_ACCEPT = 2;
        public const int ACCESS_REJECT = 3;
        public const int ACCOUNTING_REQUEST = 4;
        public const int ACCOUNTING_RESPONSE = 5;
        public const int ACCOUNTING_STATUS = 6;
        public const int PASSWORD_REQUEST = 7;
        public const int PASSWORD_ACCEPT = 8;
        public const int PASSWORD_REJECT = 9;
        public const int ACCOUNTING_MESSAGE = 10;
        public const int ACCESS_CHALLENGE = 11;
        public const int STATUS_SERVER = 12;
        public const int STATUS_CLIENT = 13;
        public const int DISCONNECT_REQUEST = 40;
        public const int DISCONNECT_ACK = 41;
        public const int DISCONNECT_NAK = 42;
        public const int COA_REQUEST = 43;
        public const int COA_ACK = 44;
        public const int COA_NAK = 45;
        public const int STATUS_REQUEST = 46;
        public const int STATUS_ACCEPT = 47;
        public const int STATUS_REJECT = 48;
        public const int RESERVED = 255;
        public const int UNDEFINED = 0;

        public const int MAX_PACKET_LENGTH = 4096;
        public const int RADIUS_HEADER_LENGTH = 20;
        
        public int PacketType { get; set; } = UNDEFINED;
        public int PacketIdentifier { get; private set; } = 0;
        public List<RadiusAttribute> Attributes { get; private set; } = new List<RadiusAttribute>();
        
        public RadiusPacket(int type)
        {
            PacketType = type;
            PacketIdentifier = GetNextPacketIdentifier();
        }

        public RadiusPacket(int type, int identifier)
        {
            PacketType = type;
            PacketIdentifier = identifier;
        }

        private static int nextPacketId = 0;
        private static readonly RandomNumberGenerator random = RandomNumberGenerator.Create();
        private byte[] authenticator = null;
        private IDictionary dictionary = DefaultDictionary.GetDefaultDictionary();
        
        public RadiusPacket(int type, int identifier, List<RadiusAttribute> attributes)
        {
            PacketType = type;
            PacketIdentifier = identifier;
            Attributes = attributes;
        }

        private RadiusPacket()
        {
        }

        private string GetPacketTypeName()
        {
            return PacketType switch
            {
                ACCESS_REQUEST => "Access-Request",
                ACCESS_ACCEPT => "Access-Accept",
                ACCESS_REJECT => "Access-Reject",
                ACCOUNTING_REQUEST => "Accounting-Request",
                ACCOUNTING_RESPONSE => "Accounting-Response",
                ACCOUNTING_STATUS => "Accounting-Status",
                PASSWORD_REQUEST => "Password-Request",
                PASSWORD_ACCEPT => "Password-Accept",
                PASSWORD_REJECT => "Password-Reject",
                ACCOUNTING_MESSAGE => "Accounting-Message",
                ACCESS_CHALLENGE => "Access-Challenge",
                STATUS_SERVER => "Status-Server",
                STATUS_CLIENT => "Status-Client",
                DISCONNECT_REQUEST => "Disconnect-Request",
                DISCONNECT_ACK => "Disconnect-ACK",
                DISCONNECT_NAK => "Disconnect-NAK",
                COA_REQUEST => "CoA-Request",
                COA_ACK => "CoA-ACK",
                COA_NAK => "CoA-NAK",
                STATUS_REQUEST => "Status-Request",
                STATUS_ACCEPT => "Status-Accept",
                STATUS_REJECT => "Status-Reject",
                RESERVED => "Reserved",
                _ => $"Unknown ({PacketType})",
            };
        }

        public void SetPacketType(int type)
        {
            if (type < 1 || type > 255)
                throw new ArgumentOutOfRangeException(nameof(type), "Packet type out of bounds");
            PacketType = type;
        }

        public void SetAttributes(List<RadiusAttribute> attributes)
        {
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));

            foreach (var attribute in attributes)
            {
                if (attribute == null)
                    throw new ArgumentException("Attribute is null", nameof(attributes));
            }
        }

        public void AddAttribute(RadiusAttribute attribute)
        {
            if (attribute == null)
                throw new ArgumentNullException(nameof(attribute));
            attribute.SetDictionary(dictionary);
            if (attribute.GetVendorId() == -1)
                Attributes.Add(attribute);
            else
            {
                var vsa = new VendorSpecificAttribute(attribute.GetVendorId());
                vsa.AddSubAttribute(attribute);
                Attributes.Add(vsa);
            }
        }

        public void AddAttribute(string typeName, string value)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Type name is empty", nameof(typeName));
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value is empty", nameof(value));

            var type = dictionary.GetAttributeTypeByName(typeName)
                       ?? throw new ArgumentException($"Unknown attribute type '{typeName}'", nameof(typeName));

            var attribute = RadiusAttribute.CreateRadiusAttribute(dictionary, type.GetVendorId(), type.GetTypeCode());
            attribute.SetAttributeValue(value);
            AddAttribute(attribute);
        }

        private void RemoveAttribute(RadiusAttribute attribute)
        {
            if (attribute.GetVendorId() == -1)
            {
                if (!Attributes.Remove(attribute))
                    throw new ArgumentException("No such attribute", nameof(attribute));
            }
            else
            {
                var vsas = GetVendorAttributes(attribute.GetVendorId());
                foreach (VendorSpecificAttribute vsa in vsas)
                {
                    var sas = vsa.GetSubAttributes();
                    if (sas.Contains(attribute))
                    {
                        vsa.RemoveSubAttribute(attribute);
                        if (sas.Count == 1)
                            RemoveAttribute(vsa);
                    }
                }
            }
        }

        protected void RemoveAttributes(int type)
        {
            if (type < 1 || type > 255)
                throw new ArgumentOutOfRangeException(nameof(type), "Attribute type out of bounds");

            Attributes.RemoveAll(a => a.GetAttributeType() == type);
        }

        public void RemoveLastAttribute(int type)
        {
            var attrs = GetAttributes(type);
            if (attrs == null || attrs.Count == 0)
                return;

            var lastAttribute = attrs.Last();
            RemoveAttribute(lastAttribute);
        }

        public void RemoveAttributes(int vendorId, int typeCode)
        {
            if (vendorId == -1)
            {
                RemoveAttributes(typeCode);
                return;
            }

            var vsas = GetVendorAttributes(vendorId);
            foreach (var vsa in vsas)
            {
                var sas = vsa.GetSubAttributes();
                sas.RemoveAll(attr => attr.GetAttributeType() == typeCode && attr.GetVendorId() == vendorId);
                if (sas.Count == 0)
                    RemoveAttribute(vsa);
            }
        }

        public List<RadiusAttribute> GetAttributes(int attributeType)
        {
            if (attributeType < 1 || attributeType > 255)
                throw new ArgumentOutOfRangeException(nameof(attributeType), "Attribute type out of bounds");

            return Attributes.Where(a => a.GetAttributeType() == attributeType).ToList();
        }

        private List<RadiusAttribute> GetAttributes(int vendorId, int attributeType)
        {
            if (vendorId == -1)
                return GetAttributes(attributeType);

            return GetVendorAttributes(vendorId)
                .SelectMany(vsa => vsa.GetSubAttributes())
                .Where(attr => attr.GetAttributeType() == attributeType && attr.GetVendorId() == vendorId)
                .ToList();
        }

        protected RadiusAttribute GetAttribute(int type)
        {
            var attrs = GetAttributes(type);
            if (attrs.Count > 1)
                throw new InvalidOperationException($"Multiple attributes of requested type {type}");
            return attrs.SingleOrDefault();
        }

        protected RadiusAttribute GetAttribute(int vendorId, int type)
        {
            if (vendorId == -1)
                return GetAttribute(type);

            var attrs = GetAttributes(vendorId, type);
            if (attrs.Count > 1)
                throw new InvalidOperationException($"Multiple attributes of requested type {type}");
            return attrs.SingleOrDefault();
        }

        private RadiusAttribute GetAttribute(string type)
        {
            if (string.IsNullOrEmpty(type))
                throw new ArgumentException("Type name is empty", nameof(type));

            var t = dictionary.GetAttributeTypeByName(type)
                    ?? throw new ArgumentException($"Unknown attribute type name '{type}'", nameof(type));

            return GetAttribute(t.GetVendorId(), t.GetTypeCode());
        }

        public string GetAttributeValue(string type)
        {
            return GetAttribute(type)?.GetAttributeValue();
        }

        private List<VendorSpecificAttribute> GetVendorAttributes(int vendorId)
        {
            return Attributes
                .OfType<VendorSpecificAttribute>()
                .Where(vsa => vsa.GetChildVendorId() == vendorId)
                .ToList();
        }

        protected static int GetNextPacketIdentifier()
        {
            nextPacketId++;
            if (nextPacketId > 255)
                nextPacketId = 0;
            return nextPacketId;
        }

        private static RadiusPacket CreateRadiusPacket(int type)
        {
            RadiusPacket rp = type switch
            {
                ACCESS_REQUEST => new AccessRequest(),
                ACCOUNTING_REQUEST => new AccountingRequest(),
                ACCESS_ACCEPT or ACCESS_REJECT or ACCOUNTING_RESPONSE => new RadiusPacket(),
                _ => new RadiusPacket(),
            };

            rp.SetPacketType(type);
            return rp;
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            s.Append(GetPacketTypeName());
            s.Append(", ID ");
            s.Append(PacketIdentifier);
            foreach (var attr in Attributes)
            {
                s.Append("\n");
                s.Append(attr.ToString());
            }
            return s.ToString();
        }

        public byte[] GetAuthenticator()
        {
            return authenticator;
        }

        public void SetAuthenticator(byte[] authenticator)
        {
            this.authenticator = authenticator;
        }

        public IDictionary GetDictionary()
        {
            return dictionary;
        }

        public void SetDictionary(IDictionary dictionary)
        {
            this.dictionary = dictionary;
            foreach (var attr in Attributes)
            {
                attr.SetDictionary(dictionary);
            }
        }

        public static RadiusPacket DecodeRequestPacket(Stream inStream, string sharedSecret)
        {
            return DecodePacket(DefaultDictionary.GetDefaultDictionary(), inStream, sharedSecret, null);
        }

        public static RadiusPacket DecodeRequestPacket(Stream inStream, string sharedSecret, int forceType)
        {
            return DecodePacket(DefaultDictionary.GetDefaultDictionary(), inStream, sharedSecret, null, forceType);
        }

        public static RadiusPacket DecodeResponsePacket(Stream inStream, string sharedSecret, RadiusPacket request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "request may not be null");

            return DecodePacket(request.GetDictionary(), inStream, sharedSecret, request);
        }

        public void EncodeRequestPacket(Stream outStream, string sharedSecret)
        {
            EncodePacket(outStream, sharedSecret, null);
        }

        public void EncodeResponsePacket(Stream outputStream, string sharedSecret, RadiusPacket request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "request cannot be null");
            }

            EncodePacket(outputStream, sharedSecret, request);
        }

        private void EncodePacket(Stream outStream, string sharedSecret, RadiusPacket request)
        {
            if (string.IsNullOrEmpty(sharedSecret))
                throw new InvalidOperationException("No shared secret has been set");

            if (request == null)
            {
                authenticator = CreateRequestAuthenticator(sharedSecret);
                EncodeRequestAttributes(sharedSecret);
            }
            
            
            byte[] attributes = GetAttributeBytes();
            int packetLength = RADIUS_HEADER_LENGTH + attributes.Length; // 20 is RADIUS_HEADER_LENGTH
            if (packetLength > MAX_PACKET_LENGTH)
                throw new Exception("packet too long");

            if (request != null)
            {
                authenticator = CreateResponseAuthenticator(sharedSecret, packetLength, attributes, authenticator);
            }
            else
            {
                authenticator = UpdateRequestAuthenticator(sharedSecret, packetLength, attributes);
            }
            
            using var writer = new BinaryWriter(outStream, Encoding.UTF8, true);
            writer.Write((byte)PacketType);
            writer.Write((byte)PacketIdentifier);
            writer.Write((byte)(packetLength >> 8));
            writer.Write((byte)(packetLength & 0xff));
            writer.Write(authenticator);
            writer.Write(attributes);
        }

        protected virtual void EncodeRequestAttributes(string sharedSecret) { }

        private byte[] CreateRequestAuthenticator(string sharedSecret)
        {
            var secretBytes = RadiusUtil.GetUtf8Bytes(sharedSecret);
            var randomBytes = new byte[16];
            random.GetBytes(randomBytes);

            using var md5 = MD5.Create();
            md5.Initialize();
            md5.TransformBlock(secretBytes, 0, secretBytes.Length, null, 0);
            md5.TransformBlock(randomBytes, 0, randomBytes.Length, null, 0);
            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return md5.Hash;
        }

        protected virtual byte[] UpdateRequestAuthenticator(string sharedSecret, int packetLength, byte[] attributes)
        {
            return authenticator;
        }

        protected byte[] CreateResponseAuthenticator(string sharedSecret, int packetLength, byte[] attributes, byte[] requestAuthenticator)
        {
            using MD5 md5 = MD5.Create();
            md5.Initialize();
            md5.TransformBlock(new byte[] { (byte)PacketType }, 0, 1, null, 0);
            md5.TransformBlock(new byte[] { (byte)PacketIdentifier }, 0, 1, null, 0);
            md5.TransformBlock(new byte[] { (byte)(packetLength >> 8), (byte)(packetLength & 0xFF) }, 0, 2, null, 0);
            md5.TransformBlock(requestAuthenticator, 0, requestAuthenticator.Length, null, 0);
            md5.TransformBlock(attributes, 0, attributes.Length, null, 0);
            byte[] sharedSecretBytes = Encoding.UTF8.GetBytes(sharedSecret);
            md5.TransformFinalBlock(sharedSecretBytes, 0, sharedSecretBytes.Length);
            return md5.Hash;
        }


        private static RadiusPacket DecodePacket(IDictionary dictionary, Stream inStream, string sharedSecret, RadiusPacket request, int forceType = UNDEFINED)
        {
            if (string.IsNullOrEmpty(sharedSecret))
                throw new InvalidOperationException("No shared secret has been set");

            if (request != null && request.GetAuthenticator() == null)
                throw new InvalidOperationException("Request authenticator not set");

            using var reader = new BinaryReader(inStream);
            var type = reader.ReadByte();
            var identifier = reader.ReadByte();
            var length = (reader.ReadByte() << 8) | reader.ReadByte();

            if (request != null && request.PacketIdentifier != identifier)
                throw new RadiusException($"Bad packet: invalid packet identifier (request: {request.PacketIdentifier}, response: {identifier})");
            if (length < RADIUS_HEADER_LENGTH)
                throw new RadiusException($"Bad packet: packet too short ({length} bytes)");
            if (length > MAX_PACKET_LENGTH)
                throw new RadiusException($"Bad packet: packet too long ({length} bytes)");

            var authenticator = reader.ReadBytes(16);
            var attributeData = reader.ReadBytes(length - RADIUS_HEADER_LENGTH);

            int pos = 0;
            int attributeCount = 0;
            while (pos < attributeData.Length)
            {
                if (pos + 1 >= attributeData.Length)
                    throw new RadiusException("Bad packet: attribute length mismatch");
                int attributeLength = attributeData[pos + 1];
                if (attributeLength < 2)
                    throw new RadiusException("Bad packet: invalid attribute length");
                pos += attributeLength;
                attributeCount++;
            }
            if (pos != attributeData.Length)
                throw new RadiusException("Bad packet: attribute length mismatch");

            var rp = CreateRadiusPacket(forceType == UNDEFINED ? type : forceType);
            rp.SetDictionary(dictionary);
            rp.SetPacketType(type);
            rp.SetPacketIdentifier(identifier);
            rp.authenticator = authenticator;

            pos = 0;
            while (pos < attributeData.Length)
            {
                int attributeType = attributeData[pos];
                int attributeLength = attributeData[pos + 1];
                var a = RadiusAttribute.CreateRadiusAttribute(dictionary, -1, attributeType);
                a.ReadAttribute(attributeData, pos, attributeLength);
                rp.AddAttribute(a);
                pos += attributeLength;
            }

            if (request == null)
            {
                rp.DecodeRequestAttributes(sharedSecret);
                rp.CheckRequestAuthenticator(sharedSecret, length, attributeData);
            }
            else
            {
                rp.CheckResponseAuthenticator(sharedSecret, length, attributeData, request.GetAuthenticator());
            }

            return rp;
        }

        protected virtual void CheckRequestAuthenticator(string sharedSecret, int packetLength, byte[] attributes) { }

        protected virtual void DecodeRequestAttributes(string sharedSecret) { }

        protected virtual void CheckResponseAuthenticator(string sharedSecret, int packetLength, byte[] attributes, byte[] requestAuthenticator)
        {
            byte[] authenticator = CreateResponseAuthenticator(sharedSecret, packetLength, attributes, requestAuthenticator);
            byte[] receivedAuth = GetAuthenticator();
            if (!authenticator.SequenceEqual(receivedAuth))
                throw new RadiusException("Response authenticator invalid");
        }

        private void SetPacketIdentifier(int identifier)
        {
            if (identifier < 0 || identifier > 255)
                throw new ArgumentOutOfRangeException(nameof(identifier), "Packet identifier out of bounds");
            PacketIdentifier = identifier;
        }
        
        private byte[] GetAttributeBytes()
        {
            using var bos = new MemoryStream();
            foreach (var a in Attributes.OrderBy(attr => attr.GetAttributeType()))
            {
                var buffer = a.WriteAttribute();
                if (bos.Length + buffer.Length > MAX_PACKET_LENGTH)
                    throw new InvalidOperationException("Packet too long");

                bos.Write(buffer, 0, buffer.Length);
            }
            bos.Flush();
            return bos.ToArray();
        }
    }
}

