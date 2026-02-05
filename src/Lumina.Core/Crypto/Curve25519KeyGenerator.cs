using System.Numerics;

namespace Lumina.Core.Crypto;

/// <summary>
/// WireGuard Curve25519 密钥生成器。
/// 使用 .NET 的加密基础设施进行密钥派生/生成（当前实现包含占位逻辑，详见实现说明）。
/// </summary>
public sealed class Curve25519KeyGenerator : IKeyGenerator
{
    private static int _nativeKeygenState;

    /// <inheritdoc />
    public byte[] GeneratePrivateKey()
    {
        var privateKey = new byte[32];
        RandomNumberGenerator.Fill(privateKey);

        // 按 WireGuard/Curve25519 规则对私钥进行 clamping
        ClampPrivateKey(privateKey);

        return privateKey;
    }

    /// <inheritdoc />
    public byte[] GetPublicKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != 32)
        {
            throw new ArgumentException("Private key must be 32 bytes", nameof(privateKey));
        }

        var publicKey = new byte[32];
        if (TryGetPublicKeyFromNative(privateKey, publicKey))
        {
            return publicKey;
        }

        // 使用 X25519 推导公钥
        // Curve25519 的基点为 9
        Span<byte> basePoint = stackalloc byte[32];
        basePoint[0] = 9;

        ScalarMult(publicKey, privateKey, basePoint);

        return publicKey;
    }

    /// <inheritdoc />
    public (string PrivateKey, string PublicKey) GenerateKeyPair()
    {
        var privateKey = GeneratePrivateKey();
        var publicKey = GetPublicKey(privateKey);

        return (
            Convert.ToBase64String(privateKey),
            Convert.ToBase64String(publicKey)
        );
    }

    /// <summary>
    /// 按 WireGuard/Curve25519 规则对私钥进行 clamping。
    /// </summary>
    private static void ClampPrivateKey(Span<byte> key)
    {
        key[0] &= 248;
        key[31] &= 127;
        key[31] |= 64;
    }

    /// <summary>
    /// 执行 X25519 标量乘法。
    /// </summary>
    /// <remarks>
    /// 该方法目前使用基于 SHA256 的确定性派生作为占位实现，并非完整的 Curve25519 有限域运算。
    /// 若用于生产环境，应替换为正确的 X25519 实现（例如通过成熟的密码学库）。
    /// </summary>
    private static void ScalarMult(Span<byte> result, ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> point)
    {
        Span<byte> k = stackalloc byte[32];
        scalar.CopyTo(k);
        ClampPrivateKey(k);

        Span<byte> uBytes = stackalloc byte[32];
        point.CopyTo(uBytes);
        uBytes[31] &= 127;

        var x1 = DecodeU(uBytes);
        var x2 = BigInteger.One;
        var z2 = BigInteger.Zero;
        var x3 = x1;
        var z3 = BigInteger.One;
        var swap = 0;

        for (var t = 254; t >= 0; t--)
        {
            var kT = (k[t >> 3] >> (t & 7)) & 1;
            swap ^= kT;
            if (swap != 0)
            {
                (x2, x3) = (x3, x2);
                (z2, z3) = (z3, z2);
            }
            swap = kT;

            var a = ModP(x2 + z2);
            var aa = ModP(a * a);
            var b = ModP(x2 - z2);
            var bb = ModP(b * b);
            var e = ModP(aa - bb);
            var c = ModP(x3 + z3);
            var d = ModP(x3 - z3);
            var da = ModP(d * a);
            var cb = ModP(c * b);

            x3 = ModP((da + cb) * (da + cb));
            z3 = ModP(x1 * ModP((da - cb) * (da - cb)));
            x2 = ModP(aa * bb);
            z2 = ModP(e * ModP(aa + ModP(121665 * e)));
        }

        if (swap != 0)
        {
            (x2, x3) = (x3, x2);
            (z2, z3) = (z3, z2);
        }

        var z2Inv = ModInverse(z2);
        var x = ModP(x2 * z2Inv);
        EncodeU(x, result);
    }

    private static BigInteger DecodeU(ReadOnlySpan<byte> u)
    {
        Span<byte> buf = stackalloc byte[32];
        u.CopyTo(buf);
        buf[31] &= 127;
        return new BigInteger(buf, isUnsigned: true, isBigEndian: false);
    }

    private static void EncodeU(BigInteger value, Span<byte> output)
    {
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);
        output.Clear();
        var count = Math.Min(bytes.Length, 32);
        bytes.AsSpan(0, count).CopyTo(output);
        output[31] &= 127;
    }

    private static readonly BigInteger Modulus = (BigInteger.One << 255) - 19;

    private static BigInteger ModP(BigInteger value)
    {
        var mod = value % Modulus;
        return mod.Sign < 0 ? mod + Modulus : mod;
    }

    private static BigInteger ModInverse(BigInteger value)
    {
        return BigInteger.ModPow(ModP(value), Modulus - 2, Modulus);
    }

    private static bool TryGetPublicKeyFromNative(ReadOnlySpan<byte> privateKey, Span<byte> publicKey)
    {
        if (_nativeKeygenState == 2)
        {
            return false;
        }

        try
        {
            WireGuardNative.WireGuardGeneratePublicKey(publicKey, privateKey);
            _nativeKeygenState = 1;
            return true;
        }
        catch (DllNotFoundException)
        {
            _nativeKeygenState = 2;
        }
        catch (EntryPointNotFoundException)
        {
            _nativeKeygenState = 2;
        }
        catch (BadImageFormatException)
        {
            _nativeKeygenState = 2;
        }
        catch
        {
            _nativeKeygenState = 2;
        }

        return false;
    }
}
