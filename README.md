<div align="center">
<img alt="LOGO" src="assets/patcher-icon.png" width="256" height="256" />

# ISTA Patcher <br/> [![License: GPL v3](https://img.shields.io/github/license/tautcony/ISTA-Patcher?style=flat-square)](https://www.gnu.org/licenses/gpl-3.0) [![build](https://img.shields.io/github/actions/workflow/status/tautcony/ISTA-Patcher/build.yml?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/actions) [![](https://img.shields.io/github/downloads/tautcony/ISTA-Patcher/total.svg?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/releases)

An IL patcher for ISTA-P, a product of learning about [dnlib](https://github.com/0xd4d/dnlib)

</div>

## Usage

> [!IMPORTANT]
> ISTA-Patcher will backup the original files to the `@ista-backup` folder, but it is still recommended to backup the `ISTA\TesterGUI` & `ISTA\PSdZ` folders before patching.

> [!NOTE]
> *nix users may need the following steps to get the ISTA-Patcher work.
> ```shell
> unzip ISTA-Patcher-*-Release.zip
> cd ISTA-Patcher-*-Release
> 
> # linux
> chmod +x ISTA-Patcher
> 
> # macos
> xattr -d com.apple.quarantine ISTA-Patcher
> chmod +x ISTA-Patcher
> codesign --force --deep -s - ISTA-Patcher
> ```

Execute the following command in a terminal:

```shell
ISTA-Patcher patch "\PATH\TO\ISTA"
```

The directory containing the patched files is located as follows:
- `.\ISTA\TesterGUI\bin\Release\@ista-patched`
- `.\ISTA\PSdZ\host\@ista-patched`
- `.\ISTA\PSdZ\hostx64\@ista-patched`

Overwrite patched files to its parent directory, read the notes, then run the program and, dang, it's ready to use.

> [!NOTE]
> - Please ensure that all related processes been killed before starting the program.
> - Import generated registry file(`license.reg`) under `Release` directory to resolve any exceptions that may arise while loading license.
> - Please ensure that both `ILeanActive` and `OSSModeActive` in the configuration file are set to `false`, otherwise `DealerData` will not load the default configuration correctly.
> - Please ensure that the `Logging.Directory` in the configuration file is a relative path that does not start with `%ISPIDATA%`, otherwise exceptions will occur during the log cleaning process.

> [!TIP]
> There are some more alternative options available, which can be discovered through exploration.
>
> To view all available options, please execute ISTA-Patcher without any arguments.

## License

Distributed under the GPLv3+ License. See LICENSE for more information.

In any case where this software is used, please be sure to include an attribution statement giving credit to [ISTA-Patcher](https://github.com/tautcony/ISTA-Patcher).

This project has never distributed ISTA or related programs to third parties, any person or organization that makes a profit from distribution is not affiliated with this project, and any risks or legal liabilities associated with that are not affiliated with this project.

## Disclaimer

Icon credit: [comboo](https://twitter.com/comboo28).

> [!CAUTION]
> This repository has been created for educational purposes only. Use it at your own risk.
