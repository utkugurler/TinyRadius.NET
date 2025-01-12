using System.Security.Cryptography;
using TinyRadius.NET.Attribute;
using TinyRadius.NET.Utils;

namespace TinyRadius.NET.Packets;

 /// <summary>
/// This class represents an Access-Request Radius packet.
/// </summary>
public class AccessRequest : RadiusPacket
{
    /// <summary>
    /// Passphrase Authentication Protocol
    /// </summary>
    public const string AUTH_PAP = "pap";

    /// <summary>
    /// Challenged Handshake Authentication Protocol
    /// </summary>
    public const string AUTH_CHAP = "chap";

    /// <summary>
    /// Microsoft Challenged Handshake Authentication Protocol V2
    /// </summary>
    public const string AUTH_MS_CHAP_V2 = "mschapv2";

    /// <summary>
    /// Extended Authentication Protocol
    /// </summary>
    public const string AUTH_EAP = "eap";

    public static readonly HashSet<string> AUTH_PROTOCOLS = new HashSet<string> { AUTH_PAP, AUTH_CHAP, AUTH_MS_CHAP_V2, AUTH_EAP };

    /// <summary>
    /// Temporary storage for the unencrypted User-Password attribute.
    /// </summary>
    private string password;

    /// <summary>
    /// Authentication protocol for this access request.
    /// </summary>
    private string authProtocol = AUTH_PAP;

    /// <summary>
    /// CHAP password from a decoded CHAP Access-Request.
    /// </summary>
    private byte[] chapPassword;

    /// <summary>
    /// CHAP challenge from a decoded CHAP Access-Request.
    /// </summary>
    private byte[] chapChallenge;

    /// <summary>
    /// Random generator
    /// </summary>
    private static readonly RandomNumberGenerator random = RandomNumberGenerator.Create();

    /// <summary>
    /// Radius type code for Radius attribute User-Name
    /// </summary>
    private const int USER_NAME = 1;

    /// <summary>
    /// Radius attribute type for User-Password attribute.
    /// </summary>
    private const int USER_PASSWORD = 2;

    /// <summary>
    /// Radius attribute type for CHAP-Password attribute.
    /// </summary>
    private const int CHAP_PASSWORD = 3;

    /// <summary>
    /// Radius attribute type for CHAP-Challenge attribute.
    /// </summary>
    private const int CHAP_CHALLENGE = 60;

    /// <summary>
    /// Radius attribute type for EAP-Message attribute.
    /// </summary>
    private const int EAP_MESSAGE = 79;

    /// <summary>
    /// Vendor id for Microsoft
    /// </summary>
    private const int MICROSOFT = 311;

    /// <summary>
    /// Radius attribute type for MS-CHAP-Challenge attribute.
    /// </summary>
    private const int MS_CHAP_CHALLENGE = 11;

    /// <summary>
    /// Radius attribute type for MS-CHAP-Challenge attribute.
    /// </summary>
    private const int MS_CHAP2_RESPONSE = 25;
    
    /// <summary>
    /// Constructs an empty Access-Request packet.
    /// </summary>
    public AccessRequest() : base(ACCESS_REQUEST)
    {
    }

    /// <summary>
    /// Constructs an Access-Request packet, sets the
    /// code, identifier and adds a User-Name and a
    /// User-Password attribute (PAP).
    /// </summary>
    /// <param name="userName">User name</param>
    /// <param name="userPassword">User password</param>
    public AccessRequest(string userName, string userPassword) : base(ACCESS_REQUEST, GetNextPacketIdentifier())
    {
        SetUserName(userName);
        SetUserPassword(userPassword);
    }

    /// <summary>
    /// Sets the User-Name attribute of this Access-Request.
    /// </summary>
    /// <param name="userName">User name to set</param>
    public void SetUserName(string userName)
    {
        if (userName == null)
            throw new ArgumentNullException(nameof(userName), "User name not set");
        if (userName.Length == 0)
            throw new ArgumentException("Empty user name not allowed");

        RemoveAttributes(USER_NAME);
        AddAttribute(new StringAttribute(USER_NAME, userName));
    }

    /// <summary>
    /// Sets the plain-text user password.
    /// </summary>
    /// <param name="userPassword">User password to set</param>
    public void SetUserPassword(string userPassword)
    {
        if (string.IsNullOrEmpty(userPassword))
            throw new ArgumentException("Password is empty");
        this.password = userPassword;
    }
    

    /// <summary>
    /// Retrieves the user name from the User-Name attribute.
    /// </summary>
    /// <returns>User name</returns>
    public string GetUserName()
    {
        var attrs = GetAttributes(USER_NAME);
        if (attrs.Count != 1)
            throw new InvalidOperationException("Exactly one User-Name attribute required");

        var ra = (StringAttribute)attrs[0];
        return ra.GetAttributeValue();
    }

    /// <summary>
    /// Selects the protocol to use for encrypting the passphrase when encoding this Radius packet.
    /// </summary>
    /// <param name="authProtocol">AUTH_PAP or AUTH_CHAP</param>
    public void SetAuthProtocol(string authProtocol)
    {
        if (authProtocol != null && AUTH_PROTOCOLS.Contains(authProtocol))
            this.authProtocol = authProtocol;
        else
            throw new ArgumentException($"Protocol must be in {string.Join(", ", AUTH_PROTOCOLS)}");
    }

    /// <summary>
    /// Verifies that the passed plain-text password matches the password
    /// (hash) sent with this Access-Request packet. Works with both PAP
    /// and CHAP.
    /// </summary>
    /// <param name="plaintext">Plain-text password</param>
    /// <returns>True if the password is valid, false otherwise</returns>
    public bool VerifyPassword(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentException("Password is empty");
        if (authProtocol.Equals(AUTH_CHAP))
            return VerifyChapPassword(plaintext);
        if (authProtocol.Equals(AUTH_MS_CHAP_V2))
            throw new RadiusException($"{AUTH_MS_CHAP_V2} verification not supported yet");
        if (authProtocol.Equals(AUTH_EAP))
            throw new RadiusException($"{AUTH_EAP} verification not supported yet");
        return password.Equals(plaintext);
    }

    /// <summary>
    /// Decrypts the User-Password attribute.
    /// </summary>
    /// <param name="sharedSecret">Shared secret</param>
    protected override void DecodeRequestAttributes(string sharedSecret)
    {
        // detect auth protocol
        var userPassword = GetAttribute(USER_PASSWORD);
        var chapPassword = GetAttribute(CHAP_PASSWORD);
        var chapChallenge = GetAttribute(CHAP_CHALLENGE);
        var msChapChallenge = GetAttribute(MICROSOFT, MS_CHAP_CHALLENGE);
        var msChap2Response = GetAttribute(MICROSOFT, MS_CHAP2_RESPONSE);
        var eapMessage = GetAttributes(EAP_MESSAGE);
        if (userPassword != null)
        {
            SetAuthProtocol(AUTH_PAP);
            this.password = DecodePapPassword(userPassword.GetAttributeData(), RadiusUtil.GetUtf8Bytes(sharedSecret));
            // copy truncated data
            userPassword.SetAttributeData(RadiusUtil.GetUtf8Bytes(this.password));
        }
        else if (chapPassword != null && chapChallenge != null)
        {
            SetAuthProtocol(AUTH_CHAP);
            this.chapPassword = chapPassword.GetAttributeData();
            this.chapChallenge = chapChallenge.GetAttributeData();
        }
        else if (chapPassword != null && GetAuthenticator().Length == 16)
        {
            // thanks to Guillaume Tartayre
            SetAuthProtocol(AUTH_CHAP);
            this.chapPassword = chapPassword.GetAttributeData();
            this.chapChallenge = GetAuthenticator();
        }
        else if (msChapChallenge != null && msChap2Response != null)
        {
            SetAuthProtocol(AUTH_MS_CHAP_V2);
            this.chapPassword = msChap2Response.GetAttributeData();
            this.chapChallenge = msChapChallenge.GetAttributeData();
        }
        else if (eapMessage.Count > 0)
        {
            SetAuthProtocol(AUTH_EAP);
        }
        else
        {
            throw new RadiusException("Access-Request: User-Password or CHAP-Password/CHAP-Challenge missing");
        }
    }

    /// <summary>
    /// Sets and encrypts the User-Password attribute.
    /// </summary>
    /// <param name="sharedSecret">Shared secret</param>
    protected override void EncodeRequestAttributes(string sharedSecret)
    {
        if (string.IsNullOrEmpty(password))
            return;

        if (authProtocol.Equals(AUTH_PAP))
        {
            byte[] pass = EncodePapPassword(RadiusUtil.GetUtf8Bytes(this.password), RadiusUtil.GetUtf8Bytes(sharedSecret));
            RemoveAttributes(USER_PASSWORD);
            AddAttribute(new RadiusAttribute(USER_PASSWORD, pass));
        }
        else if (authProtocol.Equals(AUTH_CHAP))
        {
            byte[] challenge = CreateChapChallenge();
            byte[] pass = EncodeChapPassword(password, challenge);
            RemoveAttributes(CHAP_PASSWORD);
            RemoveAttributes(CHAP_CHALLENGE);
            AddAttribute(new RadiusAttribute(CHAP_PASSWORD, pass));
            AddAttribute(new RadiusAttribute(CHAP_CHALLENGE, challenge));
        }
        else if (authProtocol.Equals(AUTH_MS_CHAP_V2))
        {
            throw new InvalidOperationException($"Encoding not supported for {AUTH_MS_CHAP_V2}");
        }
        else if (authProtocol.Equals(AUTH_EAP))
        {
            throw new InvalidOperationException($"Encoding not supported for {AUTH_EAP}");
        }
    }

    /// <summary>
    /// This method encodes the plaintext user password according to RFC 2865.
    /// </summary>
    /// <param name="userPass">The password to encrypt</param>
    /// <param name="sharedSecret">Shared secret</param>
    /// <returns>The byte array containing the encrypted password</returns>
    private byte[] EncodePapPassword(byte[] userPass, byte[] sharedSecret)
    {
        // the password must be a multiple of 16 bytes and less than or equal
        // to 128 bytes. If it isn't a multiple of 16 bytes fill it out with zeroes
        // to make it a multiple of 16 bytes. If it is greater than 128 bytes
        // truncate it at 128.
        byte[] userPassBytes;
        if (userPass.Length > 128)
        {
            userPassBytes = new byte[128];
            Array.Copy(userPass, 0, userPassBytes, 0, 128);
        }
        else
        {
            userPassBytes = userPass;
        }

        // declare the byte array to hold the final product
        byte[] encryptedPass;
        if (userPassBytes.Length < 128)
        {
            if (userPassBytes.Length % 16 == 0)
            {
                // tt is already a multiple of 16 bytes
                encryptedPass = new byte[userPassBytes.Length];
            }
            else
            {
                // make it a multiple of 16 bytes
                encryptedPass = new byte[((userPassBytes.Length / 16) * 16) + 16];
            }
        }
        else
        {
            // the encrypted password must be between 16 and 128 bytes
            encryptedPass = new byte[128];
        }

        // copy the userPass into the encrypted pass and then fill it out with zeroes by default.
        Array.Copy(userPassBytes, 0, encryptedPass, 0, userPassBytes.Length);

        // digest shared secret and authenticator
        using var md5 = MD5.Create();

        // According to section-5.2 in RFC 2865, when the password is longer than 16
        // characters: c(i) = pi xor (MD5(S + c(i-1)))
        for (int i = 0; i < encryptedPass.Length; i += 16)
        {
            md5.Initialize();
            md5.TransformBlock(sharedSecret, 0, sharedSecret.Length, sharedSecret, 0);
            if (i == 0)
            {
                md5.TransformFinalBlock(GetAuthenticator(), 0, GetAuthenticator().Length);
            }
            else
            {
                md5.TransformFinalBlock(encryptedPass, i - 16, 16);
            }

            byte[] bn = md5.Hash;

            // perform the XOR as specified by RFC 2865.
            for (int j = 0; j < 16; j++)
                encryptedPass[i + j] = (byte)(bn[j] ^ encryptedPass[i + j]);
        }
        return encryptedPass;
    }

    /// <summary>
    /// Decodes the passed encrypted password and returns the clear-text form.
    /// </summary>
    /// <param name="encryptedPass">Encrypted password</param>
    /// <param name="sharedSecret">Shared secret</param>
    /// <returns>Decrypted password</returns>
    private string DecodePapPassword(byte[] encryptedPass, byte[] sharedSecret)
    {
        if (encryptedPass == null || encryptedPass.Length < 16)
        {
            // PAP passwords require at least 16 bytes
            Console.WriteLine($"Invalid Radius packet: User-Password attribute with malformed PAP password, length = {(encryptedPass == null ? 0 : encryptedPass.Length)}, but length must be greater than 15");
            throw new RadiusException("Malformed User-Password attribute");
        }

        using var md5 = MD5.Create();
        byte[] lastBlock = new byte[16];

        for (int i = 0; i < encryptedPass.Length; i += 16)
        {
            md5.Initialize();
            md5.TransformBlock(sharedSecret, 0, sharedSecret.Length, sharedSecret, 0);
            md5.TransformFinalBlock(i == 0 ? GetAuthenticator() : lastBlock, 0, 16);
            byte[] bn = md5.Hash;

            Array.Copy(encryptedPass, i, lastBlock, 0, 16);

            for (int j = 0; j < 16; j++)
                encryptedPass[i + j] = (byte)(bn[j] ^ encryptedPass[i + j]);
        }

        // remove trailing zeros
        int len = encryptedPass.Length;
        while (len > 0 && encryptedPass[len - 1] == 0)
            len--;
        byte[] passtrunc = new byte[len];
        Array.Copy(encryptedPass, 0, passtrunc, 0, len);

        // convert to string
        return RadiusUtil.GetStringFromUtf8(passtrunc);
    }

    /// <summary>
    /// Creates a random CHAP challenge using a secure random algorithm.
    /// </summary>
    /// <returns>16 byte CHAP challenge</returns>
    private byte[] CreateChapChallenge()
    {
        byte[] challenge = new byte[16];
        random.GetBytes(challenge);
        return challenge;
    }

    /// <summary>
    /// Encodes a plain-text password using the given CHAP challenge.
    /// </summary>
    /// <param name="plaintext">Plain-text password</param>
    /// <param name="chapChallenge">CHAP challenge</param>
    /// <returns>CHAP-encoded password</returns>
    private byte[] EncodeChapPassword(string plaintext, byte[] chapChallenge)
    {
        // see RFC 2865 section 2.2
        byte chapIdentifier = GenerateChapIdentifier();
        byte[] chapPassword = new byte[17];
        chapPassword[0] = chapIdentifier;

        using var md5 = MD5.Create();
        md5.Initialize();
        md5.TransformBlock(new[] { chapIdentifier }, 0, 1, new[] { chapIdentifier }, 0);
        md5.TransformFinalBlock(RadiusUtil.GetUtf8Bytes(plaintext), 0, plaintext.Length);
        byte[] chapHash = md5.Hash;

        Array.Copy(chapHash, 0, chapPassword, 1, 16);
        return chapPassword;
    }

    /// <summary>
    /// Verifies a CHAP password against the given plaintext password.
    /// </summary>
    /// <param name="plaintext">Plain-text password</param>
    /// <returns>True if the password is valid, false otherwise</returns>
    private bool VerifyChapPassword(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentException("Plaintext must not be empty");
        if (chapChallenge == null || chapChallenge.Length != 16)
            throw new RadiusException("CHAP challenge must be 16 bytes");
        if (chapPassword == null || chapPassword.Length != 17)
            throw new RadiusException("CHAP password must be 17 bytes");

        byte chapIdentifier = chapPassword[0];
        using var md5 = MD5.Create();
        md5.Initialize();
        md5.TransformBlock(new[] { chapIdentifier }, 0, 1, new[] { chapIdentifier }, 0);
        md5.TransformFinalBlock(RadiusUtil.GetUtf8Bytes(plaintext), 0, plaintext.Length);
        byte[] chapHash = md5.Hash;

        // compare
        for (int i = 0; i < 16; i++)
            if (chapHash[i] != chapPassword[i + 1])
                return false;
        return true;
    }
    
    private static byte GenerateChapIdentifier()
    {
        byte[] randomNumber = new byte[1];
        random.GetBytes(randomNumber);
        return randomNumber[0];
    }
}
