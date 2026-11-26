// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAlter.Utils;

using global::ISTAlter.Utils;

public class RegistryUtilsTests
{
    private string _tempPath = null!;

    [SetUp]
    public void Setup()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "ISTATest_Registry_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, recursive: true);
        }
    }

    [Test]
    public void GenerateMockRegFile_WithoutCoreDll_DefaultsToNativeHiveAndWritesLicense()
    {
        RegistryUtils.GenerateMockRegFile(_tempPath, force: false);

        var content = File.ReadAllText(Path.Combine(_tempPath, "license.reg"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(content, Does.StartWith("Windows Registry Editor Version 5.00"));
            Assert.That(content, Does.Contain(@"[HKEY_LOCAL_MACHINE\SOFTWARE\BMWGroup\ISPI\Rheingold]"));
            Assert.That(content, Does.Not.Contain("WOW6432Node"));
            Assert.That(content, Does.Contain("ForceDealerData"));
            Assert.That(content, Does.Contain("<LicenseType>offline</LicenseType>"));
            Assert.That(content, Does.Not.Contain(Environment.NewLine + "<"));
        }
    }

    [Test]
    public void GenerateMockRegFile_ExistingFileWithoutForce_DoesNotOverwrite()
    {
        var file = Path.Combine(_tempPath, "license.reg");
        File.WriteAllText(file, "existing");

        RegistryUtils.GenerateMockRegFile(_tempPath, force: false);

        Assert.That(File.ReadAllText(file), Is.EqualTo("existing"));
    }

    [Test]
    public void GenerateMockRegFile_ExistingFileWithForce_Overwrites()
    {
        var file = Path.Combine(_tempPath, "license.reg");
        File.WriteAllText(file, "existing");

        RegistryUtils.GenerateMockRegFile(_tempPath, force: true);

        Assert.That(File.ReadAllText(file), Does.Contain("Windows Registry Editor Version 5.00"));
    }

    [Test]
    public void GenerateMockRegFile_WhenTargetCannotBeWritten_DoesNotThrow()
    {
        var blockingDir = Path.Combine(_tempPath, "license.reg");
        Directory.CreateDirectory(blockingDir);

        Assert.DoesNotThrow(() => RegistryUtils.GenerateMockRegFile(_tempPath, force: true));
    }
}
