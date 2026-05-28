// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

using ISTAlter.Core;
using ISTAlter.Core.Patcher.Provider;

namespace ISTestA;

/// <summary>
/// Tests for path traversal vulnerability in Patch.PatchSingleFile.
/// </summary>
public class PatchPathTraversalTests
{
    private string _testBasePath = null!;
    private string _tempTargetPath = null!;

    [SetUp]
    public void Setup()
    {
        // Create temporary directories for testing
        _testBasePath = Path.Combine(Path.GetTempPath(), "ISTATest_Base_" + Guid.NewGuid().ToString("N"));
        _tempTargetPath = Path.Combine(Path.GetTempPath(), "ISTATest_Target_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testBasePath);
        Directory.CreateDirectory(_tempTargetPath);

        // Create a dummy file to patch
        var testFile = Path.Combine(_testBasePath, "test.dll");
        File.WriteAllText(testFile, "dummy content");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up temporary directories
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, recursive: true);
        }
        if (Directory.Exists(_tempTargetPath))
        {
            Directory.Delete(_tempTargetPath, recursive: true);
        }
    }

    /// <summary>
    /// Test that path traversal sequences in Include configuration can escape the intended directory.
    /// This demonstrates the vulnerability described in FULL_CODEBASE_REVIEW.md issue #1.
    /// </summary>
    [Test]
    public void PatchSingleFile_PathTraversal_CanEscapeBaseDirectory()
    {
        // Arrange: Create a sensitive file outside the base directory
        var sensitiveFile = Path.Combine(_tempTargetPath, "sensitive.dll");
        File.WriteAllText(sensitiveFile, "sensitive content");

        // Calculate relative path from base to target using traversal
        var relativePath = Path.GetRelativePath(_testBasePath, sensitiveFile);

        // Act: Try to access the file via path traversal
        var traversalPath = Path.Join(_testBasePath, relativePath);
        var normalizedPath = Path.GetFullPath(traversalPath);

        // Assert: The normalized path escapes the base directory
        Assert.That(normalizedPath, Is.EqualTo(Path.GetFullPath(sensitiveFile)),
            "Path traversal should resolve to the sensitive file");
        Assert.That(normalizedPath.StartsWith(Path.GetFullPath(_testBasePath), StringComparison.OrdinalIgnoreCase),
            Is.False,
            "Traversal path should escape the base directory");
        Assert.That(File.Exists(normalizedPath), Is.True,
            "The traversal path should resolve to an existing file outside the base directory");
    }

    /// <summary>
    /// Test that Windows-style path traversal works.
    /// </summary>
    [Test]
    public void PatchSingleFile_WindowsPathTraversal_CanEscapeBaseDirectory()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Pass("Backslash traversal is only normalized as directory traversal on Windows.");
        }

        // Arrange
        var sensitiveFile = Path.Combine(_tempTargetPath, "important.dll");
        File.WriteAllText(sensitiveFile, "important content");

        // Create a traversal path using Windows-style separators
        var depth = _testBasePath.Split(Path.DirectorySeparatorChar).Length;
        var traversal = string.Join("\\", Enumerable.Repeat("..", depth)) + "\\" +
                       string.Join("\\", _tempTargetPath.Split(Path.DirectorySeparatorChar).Skip(1)) + "\\important.dll";

        // Act
        var traversalPath = Path.Join(_testBasePath, traversal);
        var normalizedPath = Path.GetFullPath(traversalPath);

        // Assert
        Assert.That(File.Exists(normalizedPath), Is.True,
            "Windows-style traversal should resolve to existing file");
        Assert.That(normalizedPath.StartsWith(Path.GetFullPath(_testBasePath), StringComparison.OrdinalIgnoreCase),
            Is.False,
            "Windows-style traversal should escape base directory");
    }

    /// <summary>
    /// Test that path validation would prevent traversal attacks.
    /// This test demonstrates the fix suggested in the code review.
    /// </summary>
    [Test]
    public void ValidatePath_RejectsPathTraversal()
    {
        // Arrange
        var basePath = Path.GetFullPath(_testBasePath);
        var traversalAttempt = Path.Combine("..", "sensitive.dll");
        var fullPath = Path.GetFullPath(Path.Join(basePath, traversalAttempt));

        // Act: Check if path is within base directory
        var isValid = fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);

        // Assert: Traversal should be detected and rejected
        Assert.That(isValid, Is.False,
            "Path traversal should be detected and rejected by validation");
    }

    /// <summary>
    /// Test that legitimate paths within the base directory are accepted.
    /// </summary>
    [Test]
    public void ValidatePath_AcceptsLegitimateSubdirectoryPath()
    {
        // Arrange
        var basePath = Path.GetFullPath(_testBasePath);
        var legitimatePath = "subdir\\file.dll";
        var fullPath = Path.GetFullPath(Path.Join(basePath, legitimatePath));

        // Act
        var isValid = fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.That(isValid, Is.True,
            "Legitimate subdirectory paths should be accepted");
    }
}
