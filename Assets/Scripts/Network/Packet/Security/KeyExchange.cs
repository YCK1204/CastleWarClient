using System;
using System.Security.Cryptography;
using System.Text;
using Google.FlatBuffers;
using Network;
using Core;

public partial class PacketHandler
{
    public static void SC_RSA_PUB_KEYHandler(PacketSession session, byte[] buffer)
    {
        var packet = SC_RsaPubKey.GetRootAsSC_RsaPubKey(new ByteBuffer(buffer));
        var wrappedBytes = packet.GetPubKeyBytes()?.ToArray();
        if (wrappedBytes == null) return;

        // 1. base64|hash 형식으로 수신 → 역순 언래핑
        string combined = Encoding.UTF8.GetString((byte[])(Array)wrappedBytes);
        var parts = combined.Split('|');
        if (parts.Length != 2) return;

        string hash     = parts[0];
        string keyParams = parts[1]; // "base64(Modulus).base64(Exponent)"

        // 2. 해시 검증
        if (keyParams.ToHash() != hash) return;

        // 3. RSAParameters 방식으로 서버 공개키 설정 (ImportRSAPublicKey 대신)
        SecurityManager.Instance.SetServerPublicKey(keyParams);

        // 4. AES Key + IV 생성
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        aes.GenerateIV();

        // 5. 세션 AES 초기화
        session.SetAes(aes.IV, aes.Key);

        // 6. Key, IV 각각 RSA 암호화 → base64 → hash (3차 래핑) 후 전송
        byte[] wrappedKey = WrapWithRsa(aes.Key);
        byte[] wrappedIv  = WrapWithRsa(aes.IV);

        var builder = new FlatBufferBuilder(256);
        var keyOffset = CS_AesKey.CreateKeyVectorBlock(builder, (sbyte[])(Array)wrappedKey);
        var ivOffset  = CS_AesKey.CreateIvVectorBlock(builder, (sbyte[])(Array)wrappedIv);
        var aesKeyOffset = CS_AesKey.CreateCS_AesKey(builder, ivOffset, keyOffset);

        var sendPacket = PacketManager.Instance.CreatePacket(aesKeyOffset, builder, CW_PKT_Type.CS_AES_KEY);
        if (sendPacket != null)
            NetworkManager.Instance.Send(sendPacket);
    }

    // RSA 암호화 → base64 → hash 3차 래핑 후 UTF8 바이트 반환
    private static byte[] WrapWithRsa(byte[] data)
    {
        byte[] encrypted = SecurityManager.Instance.RsaEncryptWithServerKey(data);
        string base64 = Convert.ToBase64String(encrypted);
        string hash = base64.ToHash();
        return Encoding.UTF8.GetBytes(hash + "|" + base64);
    }
}