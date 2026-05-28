// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using ISTAlter.Core.iLean;

public class ILeanCipherTests
{
    [Test]
    public void ILeanCipher_EncryptDecrypt_RoundTripsContent()
    {
        using var cipher = new iLeanCipher("0123456789abcdef0123456789abcdef", "12345678");

        var encrypted = cipher.Encrypt("hello ista");
        var decrypted = cipher.Decrypt(encrypted);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(encrypted, Is.Not.Empty);
            Assert.That(encrypted, Is.Not.EqualTo("hello ista"));
            Assert.That(decrypted, Is.EqualTo("hello ista"));
        }
    }

    [Test]
    public void ILeanCipher_EmptyInput_ReturnsEmptyString()
    {
        using var cipher = new iLeanCipher("0123456789abcdef0123456789abcdef", "12345678");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cipher.Encrypt(string.Empty), Is.Empty);
            Assert.That(cipher.Decrypt(string.Empty), Is.Empty);
        }
    }

    [TestCase("short", "12345678")]
    [TestCase("0123456789abcdef0123456789abcdef", "short")]
    public void ILeanCipher_InvalidIdentityValues_Throw(string machineGuid, string volumeSerialNumber)
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var _ = new iLeanCipher(machineGuid, volumeSerialNumber);
        });
    }

    [Test]
    public void ILeanPasswordCipher_EncryptDecrypt_RoundTripsContent()
    {
        using var cipher = new iLeanPasswordCipher("strong-password");

        var encrypted = cipher.Encrypt("secret payload");
        var decrypted = cipher.Decrypt(encrypted);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(encrypted, Is.Not.Empty);
            Assert.That(encrypted, Is.Not.EqualTo("secret payload"));
            Assert.That(decrypted, Is.EqualTo("secret payload"));
        }
    }

    [Test]
    public void ILeanPasswordCipher_EmptyInput_ReturnsEmptyString()
    {
        using var cipher = new iLeanPasswordCipher("strong-password");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cipher.Encrypt(string.Empty), Is.Empty);
            Assert.That(cipher.Decrypt(string.Empty), Is.Empty);
        }
    }

    [Test]
    public void ILeanPasswordCipher_InvalidCipherText_ReturnsEmptyString()
    {
        using var cipher = new iLeanPasswordCipher("strong-password");

        Assert.That(cipher.Decrypt(Convert.ToBase64String(new byte[16])), Is.Empty);
    }

    // ────────────── iLeanCipher internal branches ──────────────

    [Test]
    public void ILeanCipher_InitializeAesProvider_NullMachineGuid_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            iLeanCipher.InitializeAesProvider(null!, "12345678"));
    }

    [Test]
    public void ILeanCipher_InitializeAesProvider_NullVolumeSerial_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            iLeanCipher.InitializeAesProvider("0123456789abcdef0123456789abcdef", null!));
    }

    [Test]
    public void ILeanCipher_ReverseString_EmptyInput_ReturnsEmpty()
    {
        var method = typeof(iLeanCipher)
            .GetMethod("ReverseString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [string.Empty])!;
        Assert.That(result, Is.Empty);
    }

    // ────────────── iLeanPasswordCipher internal branches ──────────────

    [Test]
    public void ILeanPasswordCipher_ReverseString_EmptyInput_ReturnsEmpty()
    {
        var method = typeof(iLeanPasswordCipher)
            .GetMethod("ReverseString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [string.Empty])!;
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ILeanPasswordCipher_GetMd5Hash_EmptyInput_ReturnsEmpty()
    {
        var method = typeof(iLeanPasswordCipher)
            .GetMethod("GetMd5Hash", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var result = (string)method.Invoke(null, [string.Empty])!;
        Assert.That(result, Is.Empty);
    }
}
