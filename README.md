<div align="center">
<img alt="LOGO" src="assets/patcher-icon.png" width="256" height="256" />

# ISTA Patcher <br/> [![License: GPL v3](https://img.shields.io/github/license/tautcony/ISTA-Patcher?style=flat-square)](https://www.gnu.org/licenses/gpl-3.0) [![build](https://img.shields.io/github/actions/workflow/status/tautcony/ISTA-Patcher/build.yml?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/actions) [![](https://img.shields.io/github/downloads/tautcony/ISTA-Patcher/total.svg?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/releases)

An IL patcher for ISTA-P

</div>

## Usage

Before using it, make sure the file structure looks like the following:

```
C:\EC-APPS\ISTA
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

> ⚠️ Please backup the `ISTA\TesterGUI` & `ISTA\PSdZ` folders before patching.

Execute the following command in a terminal:

```shell
ISTA-Patcher.exe patch "C:\EC-APPS\ISTA"
```

The patched files can be found in the following directory:
- `C:\EC-APPS\ISTA\TesterGUI\bin\Release\patched`
- `C:\EC-APPS\ISTA\PSdZ\host\patched`
- `C:\EC-APPS\ISTA\PSdZ\hostx64\patched`

Import [registry file](assets/license.reg) to resolve exception while loading license.

Overwrite patched files to its parent directory, then run the program and, dang, it's ready to use.

## Other options

Run `ISTA-Patcher` without any arguments and it will show all options.

## License

Distributed under the GPLv3+ License. See LICENSE for more information.

## Disclaimer

Icon credit: [comboo](https://twitter.com/comboo28).

This repository has been created for educational purposes only. Use it at your own risk.
