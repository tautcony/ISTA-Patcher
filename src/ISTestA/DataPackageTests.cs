// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

using ISTAlter.Utils;

namespace ISTestA;

public class DataPackageTests
{
    private readonly List<(string System, string Name, string FileName, string Version)> testData = [
        ("AIR", "AIR Client", "XXX_ISPI_AIR_AirClient_23.1.840.0.msi", "23.1.840.0"),
        ("DCOM Adapter DCOM-ISPA4", "DCOM Adapter DCOM-ISPA4", "XXX_ISPI_TRAC_DCOM_Adapter_DCOM-ISPA4_9.0.2.msi", "9.0.2"),
        ("DCOM Adapter DCOM-ISPA5", "DCOM Adapter DCOM-ISPA5", "XXX_ISPI_TRAC_DCOM_Adapter_DCOM-ISPA5_9.1.0.msi", "9.1.0"),
        ("DCOM Core", "DCOM Core", "XXX_ISPI_TRAC_DCOM_Core_9.0.2.msi", "9.0.2"),
        ("HDD Update", "HDD-Update", "XXX_ISPI_HDD-Update_2.10.8532.15016.msi", "2.10.8532.15016"),
        ("IMIB Next", "IMIB MA", "XXX_ISPI_ISVM_IMIB.MA_10.37.1110.msi", "10.37.1110"),
        ("ISPI Admin Client", "ISPI Admin Client", "XXX_ISPI_iLean_ISPI_Admin_Client_24.1.1110.352.msi", "24.1.1110.352"),
        ("ISTA Launcher", "XXX_ISPI_ICOM-FW", "XXX_ISPI_ICOM-FW_03-22-11.msi", "03-22-11"),
        ("ISTA Launcher", "XXX_ISPI_ICOM-Next-FW", "XXX_ISPI_ICOM-Next-FW_04-22-11.msi", "04-22-11"),
        ("ISTA Launcher", "XXX_ISPI_ISTA-LAUNCHER", "XXX_ISPI_ISTA-LAUNCHER_1.38.0.1313.exe", "1.38.0.1313"),
        ("ISTA Launcher", "XXX_ISPI_ISTA-META", "XXX_ISPI_ISTA-META_4.50.15.xml", "4.50.15"),
        ("ISTA NF", "XXX_ISPI_ISTA-DATA_en-GB", "XXX_ISPI_ISTA-DATA_en-GB_4.50.12.istapackage", "4.50.12"),
        ("ISTA NF", "XXX_ISPI_ISTA-DATA_GLOBAL", "XXX_ISPI_ISTA-DATA_GLOBAL_4.50.12.istapackage", "4.50.12"),
        ("ISTA NF", "XXX_ISPI_ISTA-APP", "XXX_ISPI_ISTA-APP_4.50.12.29018.msi", "4.50.12.29018"),
        ("ISTA NF", "XXX_ISPI_ISTA-DATA_DELTA_en-GB", "XXX_ISPI_ISTA-DATA_DELTA_en-GB_4.50.20.istapackage", "4.50.20"),
        ("ISTA NF", "XXX_ISPI_ISTA-DATA_DELTA", "XXX_ISPI_ISTA-DATA_DELTA_4.50.20.istapackage", "4.50.20"),
        ("ISTA NF", "XXX_ISPI_ISTA-APP", "XXX_ISPI_ISTA-APP_4.50.20.29026.msi", "4.50.20.29026"),
        ("ISTA P", "ISTA-P", "XXX_ISPI_ISTA-P_SYS_3.72.0.300.exe", "3.72.0.300"),
        ("ISTA P", "ISTA-P_DAT", "XXX_ISPI_ISTA-P_DAT_3.72.0.300.istap", "3.72.0.300"),
        ("ISTA SDP", "XXX_ISPI_ISTA_DELTA-SDP_4.50.10", "XXX_ISPI_ISTA_DELTA-SDP_4.50.10.istapackage", "4.50.10"),
        ("ISTA SDP", "XXX_ISPI_ISTA_FULL-SDP_4.50.10.istapackage.zip.001", "XXX_ISPI_ISTA_FULL-SDP_4.50.10.istapackage.zip.001", "4.50.10"),
        ("ISTA SDP", "XXX_ISPI_ISTA_FULL-SDP_4.50.10.istapackage.zip.002", "XXX_ISPI_ISTA_FULL-SDP_4.50.10.istapackage.zip.002", "4.50.10"),
        ("ISTA SDP", "XXX_ISPI_ISTA_FULL-SDP_4.50.10.istapackage.zip.003", "XXX_ISPI_ISTA_FULL-SDP_4.50.10.istapackage.zip.003", "4.50.10"),
        ("ISTA SDP", "XXX_ISPI_ISTA_FULL-SDP_4.50.10.istapackage.zip.004", "XXX_ISPI_ISTA_FULL-SDP_4.50.10.istapackage.zip.004", "4.50.10"),
        ("ISTA SDP", "XXX_ISPI_ISTA_FULL-SDP_4.50.10.istapackage.zip.005", "XXX_ISPI_ISTA_FULL-SDP_4.50.10.istapackage.zip.005", "4.50.10"),
        ("ISTA SDP", "XXX_ISPI_ISTA-META_SDP_4.50.11", "XXX_ISPI_ISTA-META_SDP_4.50.11.xml", "4.50.11"),
        ("ISTA SDP BLP", "XXX_ISPI_ISTA-BLP_4.50.11", "XXX_ISPI_ISTA-BLP_4.50.11.istapackage", "4.50.11"),
    ];

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void PackageInfoTest()
    {
        foreach (var data in testData)
        {
            var ret = DataPackageUtils.DeterminePackageDetails(data.FileName);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ret.System, Is.EqualTo(data.System));
                Assert.That(ret.Name, Is.EqualTo(data.Name));
                Assert.That(ret.Version, Is.EqualTo(data.Version));
            }
        }
    }
}
