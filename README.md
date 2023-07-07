<div align="center">
<img alt="LOGO" src="assets/patcher-icon.png" width="256" height="256" />

# ISTA Patcher <br/> [![License: GPL v3](https://img.shields.io/github/license/tautcony/ISTA-Patcher?style=flat-square)](https://www.gnu.org/licenses/gpl-3.0) [![build](https://img.shields.io/github/actions/workflow/status/tautcony/ISTA-Patcher/build.yml?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/actions) [![](https://img.shields.io/github/downloads/tautcony/ISTA-Patcher/total.svg?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/releases)

An IL patcher for ISTA-P

</div>

## Usage

Prior to usage, it is essential to verify that the program itself can start correctly and that the folder structure conforms to the following format:

```
ISTA
├── Ecu
│   ├── enc_cne_1.prg
│   ├── ...
├── PSdZ
│   ├── host
│   ├── hostx64
│   ├── ...
├── TesterGUI
│   ├── bin
│   │   └── Release
│   │       ├── AirCallServices.dll
│   │       ├── ...
```

> ⚠️ ISTA-Patcher will backup the original files to the `@ista-backup` folder, but it is still recommended to backup the `ISTA\TesterGUI` & `ISTA\PSdZ` folders before patching.

Execute the following command in a terminal:

```shell
ISTA-Patcher.exe patch "C:\Program Files (x86)\BMW\ISPI\TRIC\ISTA"
```

The directory containing the patched files is located as follows:
- `.\ISTA\TesterGUI\bin\Release\@ista-patched`
- `.\ISTA\PSdZ\host\@ista-patched`
- `.\ISTA\PSdZ\hostx64\@ista-patched`

Overwrite patched files to its parent directory, read the notes, then run the program and, dang, it's ready to use.

### Notes

- Please ensure that all related processes been killed before starting the program.
- Import [registry file](assets/license.reg) to resolve any exceptions that may arise while loading license.
- Please ensure that both `ILeanActive` and `OSSModeActive` in the configuration file are set to `false`, otherwise `DealerData` will not load the default configuration correctly.
- Please ensure that the `Logging.Directory` in the configuration file is a relative path that does not start with `%ISPIDATA%`, otherwise exceptions will occur during the log cleaning process.

## Other options

There are alternative modes available, which can be discovered through exploration.

e.g., enabling ENET programming requires adding the `--enable-enet` parameter while patching.

To view all available options, please execute ISTA-Patcher without any arguments.

## License

Distributed under the GPLv3+ License. See LICENSE for more information.

When redistributing programs patched by ISTA-Patcher, please be sure to include an attribution statement that giving credit to [ISTA-Patcher](https://github.com/tautcony/ISTA-Patcher).

## Disclaimer

Icon credit: [comboo](https://twitter.com/comboo28).

This repository has been created for educational purposes only. Use it at your own risk.
