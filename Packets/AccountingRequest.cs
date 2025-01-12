using System.Security.Cryptography;
using TinyRadius.NET.Attribute;
using TinyRadius.NET.Utils;

namespace TinyRadius.NET.Packets;

public class AccountingRequest : RadiusPacket
{
    public const int ACCT_STATUS_TYPE_START = 1;
    public const int ACCT_STATUS_TYPE_STOP = 2;
    public const int ACCT_STATUS_TYPE_INTERIM_UPDATE = 3;
    public const int ACCT_STATUS_TYPE_ACCOUNTING_ON = 7;
    public const int ACCT_STATUS_TYPE_ACCOUNTING_OFF = 8;
    
    private const int USER_NAME = 1;
    private const int ACCT_STATUS_TYPE = 40;
    
    public AccountingRequest(string userName, int acctStatusType) : base(ACCOUNTING_REQUEST, GetNextPacketIdentifier())
    {
        SetUserName(userName);
        SetAcctStatusType(acctStatusType);
    }

    public AccountingRequest() : base(ACCOUNTING_REQUEST) { }

    public void SetUserName(string userName)
    {
        if (userName == null)
            throw new ArgumentNullException(nameof(userName), "User name not set");
        if (userName.Length == 0)
            throw new ArgumentException("Empty user name not allowed", nameof(userName));

        RemoveAttributes(USER_NAME);
        AddAttribute(new StringAttribute(USER_NAME, userName));
    }

    public string GetUserName()
    {
        var attrs = GetAttributes(USER_NAME);
        if (attrs.Count != 1)
            throw new InvalidOperationException("Exactly one User-Name attribute required");

        var ra = attrs[0];
        return ((StringAttribute)ra).GetAttributeValue();
    }

    public void SetAcctStatusType(int acctStatusType)
    {
        if (acctStatusType < 1 || acctStatusType > 15)
            throw new ArgumentOutOfRangeException(nameof(acctStatusType), "Bad Acct-Status-Type");
        RemoveAttributes(ACCT_STATUS_TYPE);
        AddAttribute(new IntegerAttribute(ACCT_STATUS_TYPE, acctStatusType));
    }

    public int GetAcctStatusType()
    {
        var ra = GetAttribute(ACCT_STATUS_TYPE);
        if (ra == null)
            return -1;
        return ((IntegerAttribute)ra).GetAttributeValueInt();
    }

    protected override byte[] UpdateRequestAuthenticator(string sharedSecret, int packetLength, byte[] attributes)
    {
        var authenticator = new byte[16];
        Array.Clear(authenticator, 0, authenticator.Length);

        using var md5 = MD5.Create();
        md5.Initialize();
        md5.TransformBlock(new byte[] { (byte)PacketType }, 0, 1, null, 0);
        md5.TransformBlock(new byte[] { (byte)PacketIdentifier }, 0, 1, null, 0);
        md5.TransformBlock(BitConverter.GetBytes((short)packetLength), 0, 2, null, 0);
        md5.TransformBlock(authenticator, 0, authenticator.Length, null, 0);
        md5.TransformBlock(attributes, 0, attributes.Length, null, 0);
        md5.TransformBlock(RadiusUtil.GetUtf8Bytes(sharedSecret), 0, sharedSecret.Length, null, 0);
        md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return md5.Hash;
    }

    protected override void CheckRequestAuthenticator(string sharedSecret, int packetLength, byte[] attributes)
    {
        var expectedAuthenticator = UpdateRequestAuthenticator(sharedSecret, packetLength, attributes);
        var receivedAuth = GetAuthenticator();
        for (int i = 0; i < 16; i++)
        {
            if (expectedAuthenticator[i] != receivedAuth[i])
                throw new RadiusException("Request authenticator invalid");
        }
    }
}
