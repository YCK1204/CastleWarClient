using System.Security.Cryptography;
using CWFramework;

public class SecurityManager : Singleton<SecurityManager>
{
    private static readonly RSA _rsa = RSA.Create(2048);
    private static RSA _serverRsa;

    #region RSA
    public byte[] RsaPublicKey => _rsa.ExportRSAPublicKey();

    public void SetServerPublicKey(byte[] pubKeyBytes)
    {
        _serverRsa?.Dispose();
        _serverRsa = RSA.Create();
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

        using var aes = Aes.Create();
        aes.Key = session.AesKey;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        byte[] encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

        byte[] result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);
        return result;
    }

    public byte[]? AesDecrypt(byte[] data, PacketSession session)
    {
        if (!session.IsAesInit)
        {
            return null;
        }

        using var aes = Aes.Create();
        aes.Key = session.AesKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        byte[] iv = new byte[16];
        Buffer.BlockCopy(data, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 16, data.Length - 16);
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