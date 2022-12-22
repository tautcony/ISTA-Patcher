# ISTA Patcher [![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg?style=flat-square)](https://www.gnu.org/licenses/gpl-3.0) [![build](https://img.shields.io/github/actions/workflow/status/tautcony/ISTA-Patcher/ci.yml?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/actions) [![](https://img.shields.io/github/downloads/tautcony/ISTA-Patcher/total.svg?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/releases) 

An IL patcher for ISTA-P

## Usage

Before using it, make sure the file structure looks like bellow:

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

> ⚠️ Please backup folder `ISTA\TesterGUI` & `ISTA\PSdZ` before patch.

Execute the following command in terminal.

```batch
ISTA-Patcher.exe patch "C:\EC-APPS\ISTA"
```

You can find the patched files in the following directory:
- `C:\EC-APPS\ISTA\TesterGUI\bin\Release\patched`
- `C:\EC-APPS\ISTA\PSdZ\host\patched`
- `C:\EC-APPS\ISTA\PSdZ\host\patched`

Overwrite the patched file to its parent directory, then start the program, and dang, it's ready to use.

## Other options

Start `ISTA-Patcher` without any arguments and it will show all options.

## License

Distributed under the GPLv3+ License. See LICENSE for more information.

## Disclaimer

This repository was created for educational purposes only. Use it at your own risk.
