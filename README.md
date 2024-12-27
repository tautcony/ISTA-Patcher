<div align="center">
<img alt="LOGO" src="assets/patcher-icon.png" width="256" height="256" />

# $$\mathbf{ISTA\ Patcher}^{\color{orange}overdose}$$ <br/> [![License: GPL v3](https://img.shields.io/github/license/tautcony/ISTA-Patcher?style=flat-square)](https://www.gnu.org/licenses/gpl-3.0) [![build](https://img.shields.io/github/actions/workflow/status/tautcony/ISTA-Patcher/build.yml?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/actions) [![Latest release](https://img.shields.io/github/v/release/tautcony/ISTA-Patcher?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/releases/latest) [![](https://img.shields.io/github/downloads/tautcony/ISTA-Patcher/total.svg?style=flat-square)](https://github.com/tautcony/ISTA-Patcher/releases) 

An IL patcher for ISTA-P from scratch, a product of learning about [dnlib](https://github.com/0xd4d/dnlib).

</div>

## Usage

> [!IMPORTANT]
> ISTA-Patcher will back up the original files to the `@ista-backup` folder. However, it is recommended to manually back up the `ISTA\TesterGUI` & `ISTA\PSdZ` folders before patching.

> [!NOTE]
> *nix users may need the following steps to get ISTA-Patcher working:
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
# [option] is a placeholder for the options you want to use.
ISTA-Patcher patch [option]... "\PATH\TO\ISTA"
```

The directory containing the patched files is located as follows:
- `.\ISTA\TesterGUI\bin\Release\@ista-patched`
- `.\ISTA\PSdZ\host\@ista-patched`
- `.\ISTA\PSdZ\hostx64\@ista-patched`

Overwrite the patched files in their respective parent directories, read the notes, then run the program and, dang, it's ready to use.

> [!NOTE]
> - The program offers optional parameters, which vary by version. Configure settings as needed.
> - Ensure that all related processes are terminated before starting.
> - Enclose paths containing spaces in quotes to avoid errors.
> - Verify that environment variables (eg: `ISPIDATA`) and the configuration files are correctly set.

> [!TIP]
> Explore alternative features available in the program.
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
> It must be made clear that ISTA-Patcher is an independent project and does not provide support for the installation or usage of ISTA. Any individual or organization redistributing it or its derivatives is not affiliated with this project.
