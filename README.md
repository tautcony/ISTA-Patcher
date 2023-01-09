<div align="center">
<img alt="LOGO" src="images/patcher-icon.png" width="256" height="256" />

# ISTA Patcher <br/> [![License: GPL v3](https://img.shields.io/github/license/tautcony/ISTA-Patcher?style=flat-square)](https://www.gnu.org/licenses/gpl-3.0) [![build](https://img.shields.io/github/actions/workflow/status/tautcony/ISTA-Patcher/build.yml?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/actions) [![](https://img.shields.io/github/downloads/tautcony/ISTA-Patcher/total.svg?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/releases)

An IL patcher for ISTA-P

</div>

## Usage

Before using it, make sure the file structure looks like bellow:

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

> ⚠️ Please backup folder `ISTA\TesterGUI` & `ISTA\PSdZ` before patch.

Execute the following command in terminal.

```batch
ISTA-Patcher.exe patch "C:\EC-APPS\ISTA"
```

You can find the patched files in the following directory:
- `C:\EC-APPS\ISTA\TesterGUI\bin\Release\patched`
- `C:\EC-APPS\ISTA\PSdZ\host\patched`
- `C:\EC-APPS\ISTA\PSdZ\hostx64\patched`

Overwrite the patched file to its parent directory, then start the program, and dang, it's ready to use.

## Other options

Start `ISTA-Patcher` without any arguments and it will show all options.

## License

Distributed under the GPLv3+ License. See LICENSE for more information.

## Disclaimer

Credit of icon: [comboo](https://twitter.com/comboo28).

This repository was created for educational purposes only. Use it at your own risk.
