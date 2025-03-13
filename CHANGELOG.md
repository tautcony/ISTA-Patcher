# Changelog

## 2.3.5 / 2025-03-13

- Feat - Add enable skip brand compatible check with `patch --enable-skip-brand-compatible-check` flag
- Feat - Add fix DS2 vehicle identification with `patch --enable-fix-ds2-vehicle-identification` flag
- Feat - Improve iLean command with new output & formatter flag
- Feat - Add support for `SetPsdzProperties` with new default arguments in `ConfigurationService`
- Feat - Set `PsdzWebserviceEnabled` & `ShouldUseIdentNuget` to false in standalone mode
- Fix - iLean encryption/decryption command
- Fix - Console background color
- Fix - Minor bugs and usability improvements
- Chore - Upgrade dependencies

## 2.3.4 / 2025-01-26

- Feat - Add enable AIR Client with `patch --enable-air-client` flag
- Feat - Add keypair generator with `crypto --create-key-pair` flag
- Feat - Add warning for invalid patches
- Feat - Add unit tests project
- Feat - New cli parser implement
- Feat - New config file format
- Fix - Patcher for finished operations
- Fix - Minor bugs and usability improvements
- Chore - Upgrade dependencies
- Chore - Refactor project structure
- Chore - Remove de4dot

## 2.3.3 / 2024-12-12

- Feat - Upgrade to .NET 9.0
- Feat - Add patch to enable opening of finished operations with `--enable-finished-op` flag
- Feat - Add show machine info for ilean offline client with `ilean` command
- Feat - Add patch to skip fake fsc reject with `--enable-skip-fake-fsc-reject` flag
- Feat - Add support to skip validation patch with `--mode` option
- Feat - Reorder and rename cli options
- Chore - Upgrade dependencies
- Chore - Refactor project structure

## 2.3.2 / 2024-09-02

- Feat - Reduce size of the executable
- Feat - Patch some `ConfigSettings` properties to avoid manual setup
- Feat - Add support to specify the maximum number of concurrent tasks with `--max-degree-of-parallelism` flag
- Chore - Remove unused dependencies
- Chore - Upgrade dependencies
- Chore - Refactor code

## 2.3.0 / 2024-07-21

- Feat - Add support to specific market language with `--market-language` flag
- Feat - Add support to patch `ClientConfigurationManager` with `--skip-sync-client-config` flag
- Feat - Add support to skip specific library when patching with `--skip-library` flag
- Feat - Add support to Patch `LoginOptionsProvider` with `--patch-user-auth` flag
- Feat - New patch flag , avoid some reflection issues
- Fix - Minor bugs and usability improvements
- Chore - Upgrade dependencies
- Chore - Refactor code

## 2.1.1 / 2024-02-18

- Fix - Reduce hash length in `InformationalVersion`
- Chore - Upgrade dependencies

## 2.1.0 / 2024-02-01

- Feat - New CLI command parser by System.CommandLine
- Fix - Minor bugs and usability improvements
- Chore - Upgrade dependencies
- Chore - Upgrade build script

## 2.0.0 / 2023-11-08

- Feat - Upgrade to .NET 8.0
- Feat - Speed up with TaskScheduler
- Feat - Add support to patch `UserEnvironmentProvider` with `--patch-user-auth` flag
- Feat - Add support to generate reg file with `--generate-reg-file` flag
- Feat - Build all artifacts on Linux
- Fix - Minor bugs and usability improvements
- Chore - Upgrade dependencies
- Chore - Restructuring options

## 1.2.4 / 2023-09-11

- Feat - Add more options
- Feat - Add option to decode license info stream
- Chore - Refactor code

## 1.2.3 / 2023-08-04

- Feat - Upgrade to .NET 7.0
- Feat - Add some new optional patches
- Fix - Version & time extraction
- Fix - Title duplicated append
- Chore - Refactor PatchUtils class

## 1.2.2 / 2023-07-21

- Fix - File backup logic

## 1.2.1 / 2023-07-10

- Feat - Backup original files before patching
- Feat - Add integrity check
- Fix - Minor bugs and improved usability

## 1.2.0 / 2023-05-08

- Feat - Add some new optional patches
- Feat - Make enet patch optional
- Feat - use commit hash as version before release
- Chore - Refactor code

## 1.1.1 / 2023-03-06

- Feat - Add universal binary support for Apple Silicon
- Fix - Minor bugs and improved usability
- Chore - Improved operation instructions and reorganized parameters for ease of use

## 1.1.0 / 2023-01-12

- Feat - Add patch method by replacing the private key of license

## 1.0.0 / 2022-12-23

- Initial implementation
