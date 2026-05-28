// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

using ISTAlter.Utils;

namespace ISTestA;

/// <summary>
/// Tests for HashFileInfo array bounds vulnerability.
/// </summary>
public class HashFileInfoTests
{
    /// <summary>
    /// Test that HashFileInfo throws when given an empty array.
    /// This demonstrates the vulnerability described in FULL_CODEBASE_REVIEW.md issue #2.
    /// </summary>
    [Test]
    public void Constructor_EmptyArray_ThrowsException()
    {
        // Arrange
        var emptyArray = Array.Empty<string>();

        // Act & Assert
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
        {
            // Use reflection to call the protected internal constructor
            var constructor = typeof(HashFileInfo).GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(IReadOnlyList<string>) },
                null);
            constructor?.Invoke(new object[] { emptyArray });
        }, "Empty array should throw exception");

        Assert.That(ex!.InnerException, Is.InstanceOf<ArgumentException>(),
            "Inner exception should be ArgumentException");
        Assert.That(ex.InnerException!.Message, Does.Contain("Expected at least 2 elements"),
            "Exception message should indicate the validation failure");
    }

    /// <summary>
    /// Test that HashFileInfo throws when given a single-element array.
    /// </summary>
    [Test]
    public void Constructor_SingleElement_ThrowsException()
    {
        // Arrange
        var singleElement = new[] { "single_value_without_delimiter" };

        // Act & Assert
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
        {
            var constructor = typeof(HashFileInfo).GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(IReadOnlyList<string>) },
                null);
            constructor?.Invoke(new object[] { singleElement });
        }, "Single-element array should throw exception");

        Assert.That(ex!.InnerException, Is.InstanceOf<ArgumentException>(),
            "Inner exception should be ArgumentException");
        Assert.That(ex.InnerException!.Message, Does.Contain("Expected at least 2 elements"),
            "Exception message should indicate the validation failure");
    }

    /// <summary>
    /// Test that HashFileInfo works correctly with valid two-element array.
    /// </summary>
    [Test]
    public void Constructor_ValidTwoElements_CreatesInstance()
    {
        // Arrange
        var validData = new[] { "path/to/file.dll", "YWJjZGVmZ2hpamtsbW5vcA==" }; // base64 hash

        // Act
        var constructor = typeof(HashFileInfo).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(IReadOnlyList<string>) },
            null);
        var instance = constructor?.Invoke(new object[] { validData }) as HashFileInfo;

        // Assert
        Assert.That(instance, Is.Not.Null, "Valid data should create instance");
        Assert.That(instance!.FilePath, Is.EqualTo("path/to/file.dll"), "FilePath should be set correctly");
        Assert.That(instance.FileName, Is.EqualTo("file.dll"), "FileName should be extracted correctly");
    }

    /// <summary>
    /// Test that HashFileInfo handles BOM character correctly.
    /// </summary>
    [Test]
    public void Constructor_WithBOM_RemovesBOM()
    {
        // Arrange
        var dataWithBOM = new[] { "﻿path/to/file.dll", "YWJjZGVmZ2hpamtsbW5vcA==" };

        // Act
        var constructor = typeof(HashFileInfo).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(IReadOnlyList<string>) },
            null);
        var instance = constructor?.Invoke(new object[] { dataWithBOM }) as HashFileInfo;

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance!.FilePath, Is.EqualTo("path/to/file.dll"),
            "BOM character should be trimmed from file path");
    }

    /// <summary>
    /// Test that HashFileInfo handles backslashes correctly.
    /// </summary>
    [Test]
    public void Constructor_WithBackslashes_ConvertsToForwardSlashes()
    {
        // Arrange
        var dataWithBackslashes = new[] { "path\\to\\file.dll", "YWJjZGVmZ2hpamtsbW5vcA==" };

        // Act
        var constructor = typeof(HashFileInfo).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(IReadOnlyList<string>) },
            null);
        var instance = constructor?.Invoke(new object[] { dataWithBackslashes }) as HashFileInfo;

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance!.FilePath, Is.EqualTo("path/to/file.dll"),
            "Backslashes should be converted to forward slashes");
    }

    /// <summary>
    /// Test the scenario from IntegrityUtils where split returns fewer elements than expected.
    /// </summary>
    [Test]
    public void Constructor_MalformedSplitResult_ThrowsException()
    {
        // Arrange: Simulate what happens when "row.Split(";;")" returns only one element
        var malformedData = "single_value_without_delimiter".Split(";;");

        // Act & Assert
        Assert.That(malformedData.Length, Is.EqualTo(1),
            "Split should return single element for malformed data");

        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
        {
            var constructor = typeof(HashFileInfo).GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(IReadOnlyList<string>) },
                null);
            constructor?.Invoke(new object[] { malformedData });
        }, "Malformed split result should throw exception");

        Assert.That(ex!.InnerException, Is.InstanceOf<ArgumentException>(),
            "Inner exception should be ArgumentException");
        Assert.That(ex.InnerException!.Message, Does.Contain("Expected at least 2 elements"),
            "Exception message should indicate the validation failure");
    }

    // ────────────── Hash property ──────────────

    /// <summary>
    /// Hash stored as base64 should be decoded to hex on first access.
    /// </summary>
    [Test]
    public void Hash_ValidBase64_DecodesOnFirstAccess()
    {
        // "abcdefghijklmnop" (16 bytes) as base64
        var bytes = new byte[] { 0xAB, 0xCD, 0xEF };
        var b64 = Convert.ToBase64String(bytes);
        var instance = new HashFileInfo(["file.dll", b64]);

        var hash = instance.Hash;

        Assert.That(hash, Is.EqualTo("ABCDEF").IgnoreCase);
    }

    /// <summary>
    /// Second access of Hash should return cached value without re-decoding.
    /// </summary>
    [Test]
    public void Hash_ValidBase64_SecondAccessReturnsCachedValue()
    {
        var bytes = new byte[] { 0x01, 0x02, 0x03 };
        var b64 = Convert.ToBase64String(bytes);
        var instance = new HashFileInfo(["file.dll", b64]);

        var first = instance.Hash;
        var second = instance.Hash;

        Assert.That(second, Is.EqualTo(first));
    }

    /// <summary>
    /// Invalid base64 should cause the Hash property to return empty string.
    /// </summary>
    [Test]
    public void Hash_InvalidBase64_ReturnsEmptyString()
    {
        var instance = new HashFileInfo(["file.dll", "not-valid-base64!!!"]);

        var hash = instance.Hash;

        Assert.That(hash, Is.Empty);
    }

    // ────────────── CalculateHash ──────────────

    [Test]
    public async Task CalculateHash_ExistingFile_ReturnsHexHash()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFile, [1, 2, 3, 4, 5]);

            var hash = await HashFileInfo.CalculateHash(tempFile);

            Assert.That(hash, Is.Not.Empty);
            Assert.That(hash, Has.Length.EqualTo(64)); // SHA-256 = 32 bytes = 64 hex chars
            Assert.That(hash, Does.Match("^[0-9A-Fa-f]+$"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task CalculateHash_NonExistentFile_ReturnsEmptyString()
    {
        // Use a temp directory that exists but with a non-existent file name
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "_nonexistent.dll");

        var hash = await HashFileInfo.CalculateHash(path);

        Assert.That(hash, Is.Empty);
    }
}
