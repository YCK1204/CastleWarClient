using System.Security.Cryptography;
using CWFramework;

public class SecurityManager : Singleton<SecurityManager>
{
    private static readonly RSA _rsa = RSA.Create(2048);
    private static RSA _serverRsa = RSA.Create();

    #region RSA
    public byte[] RsaPublicKey => _rsa.ExportRSAPublicKey();

    public void SetServerPublicKey(byte[] pubKeyBytes)
    {
        _serverRsa.ImportRSAPublicKey(pubKeyBytes, out _);
    }

    public byte[] RsaEncryptWithServerKey(byte[] value)
    {
        return _serverRsa.Encrypt(value, RSAEncryptionPadding.OaepSHA256);
    }

    public byte[] RsaEncrypt(byte[] value)
    {
        return _rsa.Encrypt(value, RSAEncryptionPadding.OaepSHA256);
    }

    public byte[] RsaDecrypt(byte[] value)
    {
        return _rsa.Decrypt(value, RSAEncryptionPadding.OaepSHA256);
    }
    #endregion

    #region AES
    public byte[]? AesEncrypt(byte[] data, PacketSession session)
    {
        if (!session.IsAesInit)
        {
            return null;
        }
        var encryptor = session.Aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(data, 0, data.Length);
    }

    public byte[]? AesDecrypt(byte[] data, PacketSession session)
    {
        if (!session.IsAesInit)
        {
            return null;
        }
        var decryptor = session.Aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }
    #endregion

    // #region JWT
    // public async Task VerifyJWT(string idToken, ClientSession clientSession)
    // {
    //     try
    //     {
    //         var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
    //
    //         var player = await Manager.Player.GeneratePlayerAsync(
    //             payload.Subject,
    //             payload.Picture ?? "",
    //             payload.Name ?? payload.Email ?? "User",
    //             payload.Email ?? ""
    //         );
    //
    //         clientSession.Player = player;
    //
    //         FlatBufferBuilder builder = new FlatBufferBuilder(256);
    //         var jwtOffset         = builder.CreateString(idToken);
    //         var userIdOffset      = builder.CreateString(player.Id);
    //         var emailOffset       = builder.CreateString(player.Email);
    //         var profileUrlOffset  = builder.CreateString(player.ProfileUrl);
    //         var displayNameOffset = builder.CreateString(player.DisplayName);
    //
    //         var loginData = PKT_S_LoginSuccess.CreatePKT_S_LoginSuccess(
    //             builder, jwtOffset, userIdOffset, emailOffset, profileUrlOffset, displayNameOffset);
    //
    //         var loginPkt = PacketManager.Instance.CreatePacketWithAES(
    //             loginData, builder, PKT_Type.PKT_S_LoginSuccess, clientSession);
    //
    //         clientSession.Send(loginPkt);
    //         // Manager.Room.GenerateRoom(player, player);
    //     }
    //     // catch (InvalidJwtException ex)
    //     // {
    //     //     Console.WriteLine($"Invalid JWT: {ex.Message}");
    //     // }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"JWT validation error: {ex.GetType().Name} - {ex.Message}");
    //     }
    // }
    // #endregion
}