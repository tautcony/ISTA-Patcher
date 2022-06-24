# ISTA Patcher [![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
An IL patcher for ISTA-P

Tested under
- `4.34.40.26161`
- `4.35.18.26579`

## Usage

The folder structure should look like this:

```
C:\EC-APPS\ISTA
├── Ecu
│   ├── enc_cne_1.prg
│   ├── ...
├── TesterGUI
│   ├── bin
│   │   └── Release
│   │       ├── AirCallServices.dll
│   │       ├── ...
```

⚠️ Please backup folder `ISTA\TesterGUI\bin\Release` before patch.

Execute the following command in terminal.

`ISTA-Patcher.exe patch "C:\EC-APPS\ISTA"`

For more information on usage, please use the help command.

Sample output
```
=== ISTA Patch Begin ===
AirCallServices.dll ----------- [skip]
AirCommon.dll ----------- [skip]
AirForkServices.dll ----------- [skip]
Authoring.dll -+-++---+-- [patched]
BMW.ISPI.Puk.Decentral.PukBasicData.dll ----------- [skip]
BMW.ISPI.Puk.Decentral.PukVCLib.dll ----------- [skip]
BMW.ISPI.Puk.Decentral.VehicleCaseData.dll ----------- [skip]
BMW.Rheingold.SvgViewer.dll ----------- [skip]
COAPILib.dll ----------- [skip]
COAPILib32v4.0.dll ----------- [skip]
CommonServices.dll -------+--- [patched]
CommonServiceSec.dll ----------- [skip]
DiagnosticsBusinessData.dll ----------- [skip]
FscValidationClient.dll ----------+ [patched]
FscValidationContract.dll ----------- [skip]
HidLibrary.dll ----------- [skip]
IAirCallServices.dll ----------- [skip]
IAirForkServices.dll ----------- [skip]
ICSharpCode.SharpZipLib.dll ----------- [skip]
Interop.SHDocVw.dll ----------- [skip]
ISTAGUI.exe ++-++---+-- [patched]
IstaOperationContract.dll ----------- [skip]
IstaOperationController.dll -+-++---+-- [patched]
IstaOperationImpl.dll ++-++---+-- [patched]
IstaServicesClient.dll -----+----- [patched]
IstaServicesContract.dll ----------- [skip]
IstaServicesController.dll ----------- [skip]
IstaServicesHost.exe +---------- [patched]
IstaServicesImpl.dll ++-++---+-- [patched]
PsdzServiceClient.dll ----------- [skip]
PsdzServiceContract.dll ----------- [skip]
RGSPC.exe -+-++---+-- [patched]
RheingoldCoreBootstrap.dll ----------- [skip]
RheingoldCoreContracts.dll ----------- [skip]
RheingoldCoreFramework.dll --+------+- [patched]
RheingoldDatabaseOracleConnector.dll -+-++---+-- [patched]
RheingoldDatabasePostgreSQLConnector.dll -+-++---+-- [patched]
RheingoldDatabaseSQLiteConnector.dll -+-++---+-- [patched]
RheingoldDiagnostics.dll -+-++---+-- [patched]
RheingoldFASTA.dll -+-++---+-- [patched]
RheingoldFASTAConfigParsing.dll ----------- [skip]
RheingoldIDES.dll ----------- [skip]
RheingoldInfoProvider.dll -+-++---+-- [patched]
RheingoldInfoProvider.XmlSerializers.dll ----------- [skip]
RheingoldISPINext.dll -+-++-+-+-- [patched]
RheingoldISTACoreFramework.dll -+-++---+-- [patched]
RheingoldKMM.dll ----------- [skip]
RheingoldMeasurement.dll -+-++---+-- [patched]
RheingoldMeasurementCommon.dll ----------- [skip]
RheingoldMeasurementCommunication.dll -+-++---+-- [patched]
RheingoldOperationsReportConverter.dll ----------- [skip]
RheingoldPresentationFramework.dll -+-++---+-- [patched]
RheingoldProcessCommunicationBase.dll ----------- [skip]
RheingoldProgramming.dll -+-++---+-- [patched]
RheingoldSessionController.dll -+-++---+-- [patched]
RheingoldSharpVectorSvgViewer.dll ----------- [skip]
RheingoldVehicleCommunication.dll -+-++---+-- [patched]
RheingoldxVM.dll -+-++---+-- [patched]
WsiDataProvider.dll -+-++---+-- [patched]
=== ISTA Patch Done ===
```

You can find the patched files in folder `C:\EC-APPS\ISTA\TesterGUI\bin\Release\patched`.
Overwrite the files in this folder to the parent folder and start the program, it should work fine.

