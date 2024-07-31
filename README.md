<div align="center">
<img alt="LOGO" src="assets/patcher-icon.png" width="256" height="256" />

# $$\mathbf{ISTA\ Patcher}^{\color{orange}overdose}$$ <br/> [![License: GPL v3](https://img.shields.io/github/license/tautcony/ISTA-Patcher?style=flat-square)](https://www.gnu.org/licenses/gpl-3.0) [![build](https://img.shields.io/github/actions/workflow/status/tautcony/ISTA-Patcher/build.yml?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/actions) [![](https://img.shields.io/github/downloads/tautcony/ISTA-Patcher/total.svg?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/releases)

An IL patcher for ISTA-P from scratch, a product of learning about [dnlib](https://github.com/0xd4d/dnlib).

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
> - Import the generated registry file(`license.reg`) under `Release` directory to resolve some exceptions that may arise while loading the license.
> - From version `4.48.x`, Please ensure that `BMW.Rheingold.Auth.Enabled` in the configuration file is set to `false`, or try the `--patch-user-auth` option.
> - Please ensure that the `Logging.Directory` in the configuration file is a relative path that does not start with `%ISPIDATA%`, otherwise exceptions will occur during the log cleaning process.

> [!TIP]
> There are several other alternative features that can be discovered through exploration.
>
> For all available options and to learn more, please execute ISTA-Patcher without any arguments and follow the instructions.

## License

Distributed under the GPLv3+ License. See LICENSE for more information.

When redistributing any content that benefiting from ISTA-Patcher, it is imperative to include an attribution statement that credits [ISTA-Patcher](https://github.com/tautcony/ISTA-Patcher).

## Disclaimer

Icon credit: [comboo](https://twitter.com/comboo28).

> [!CAUTION]
> This repository has been created for educational purposes only. Use it at your own risk.
> 
> It must be made clear that ISTA-Patcher is an independent project, any other individual or organization redistributing it or its derivatives is not affiliated with this project and may not be able to provide any support for installing or using ISTA.
