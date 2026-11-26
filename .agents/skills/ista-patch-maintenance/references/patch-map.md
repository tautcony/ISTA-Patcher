# Patch Map & Recipes

Companion reference for `ista-patch-maintenance`. Keep this file factual;
update when major patch categories or data layouts change.

## Install / Binary Paths

Runtime patching roots (under ISTA install):

| Relative path | Role |
|---------------|------|
| `TesterGUI/bin/Release/` | Primary managed assemblies (default file list fallback) |
| `Ecu/enc_cne_1.prg` | Encrypted integrity list of expected files |
| `PSdZ/host/`, `PSdZ/hostx64/` | PSDZ host (`PsdzServiceImpl.dll` etc.) — may need `!`-prefixed include paths |

Patcher output directories next to each original:

| Dir | Role |
|-----|------|
| `@ista-backup` | First-time backup of originals |
| `@ista-patched` | Written patched modules pending verification |

## Decompile Tree Conventions

`*-project` folders are per-assembly C# projects (ILSpy/dnSpy export style):

```text
V_x.y.z-project/
  RheingoldCoreFramework/
    BMW/Rheingold/CoreFramework/...
  ISTAGUI/
    BMW/Rheingold/ISTAGUI/...
  CommonServices/
  RheingoldISPINext/
  ...
```

Notes:

- The same type may appear under many projects (linked/copied decompile noise).
  Prefer the assembly that **ships** the method at runtime.
- Namespace folders may differ slightly across versions
  (`CoreFramework/LicenseManagement` vs `BMW/Rheingold/CoreFramework/...`).
- Search by type name first, then open the file under the primary assembly project.

## Signature Recipe

Implementation: `DnlibUtils.DescriptionOf(MethodDef)`.

```text
(paramFullName1,paramFullName2,...,paramFullNameN)returnFullName
```

| C# | Signature fragment |
|----|--------------------|
| `void Foo()` | `()System.Void` |
| `bool Foo(string s)` | `(System.String)System.Boolean` |
| `LicenseStatus Foo(LicenseInfo a, bool b)` | `(…LicenseInfo,System.Boolean)…LicenseStatus` |
| `Dictionary<A,B> Foo(bool x)` | `(System.Boolean)System.Collections.Generic.Dictionary`2<A,B>` |
| ctor | name `.ctor`, return `System.Void` |
| property getter `IsX` | often method `get_IsX` with `()System.Boolean` |

When dnlib and C# disagree (modopt, mangling), trust dnlib on the genuine DLL.

## Operation Cheat Sheet

| Goal | Helper |
|------|--------|
| Always true | `DnlibUtils.ReturnTrueMethod` |
| Always false | `DnlibUtils.ReturnFalseMethod` |
| Return 0 / often enum Valid | `DnlibUtils.ReturnZeroMethod` |
| Return 1 | `DnlibUtils.ReturnOneMethod` |
| Void no-op | `DnlibUtils.EmptyingMethod` |
| Return string / uint / newobj | `ReturnStringMethod` / `ReturnUInt32Method` / `ReturnObjectMethod` |
| Async | `PatchAsyncFunction` |
| Property get | `PatchGetter` |

Config-only custom patches (`CustomPatchDefinition.OperationType`):
`ReturnTrue`, `ReturnFalse`, `ReturnZero`, `ReturnOne`, `Empty`/`Emptying`.

## Version Migration Examples (in-repo)

### CheckExpirationDate (UI license expiry)

| Version range | Type | Method | Library |
|---------------|------|--------|---------|
| `< 4.58` | `BMW.Rheingold.ISTAGUI.ViewModels.MainWindowViewModel` | `CheckExpirationDate` | `ISTAGUI.exe` |
| `>= 4.58` | `BMW.Rheingold.ISTAGUI.Services.MainWindowViewModelService` | `CheckExpirationDate` | `ISTAGUI.exe` |

Evidence:

- `ista-test-data/4.53.13-project/.../MainWindowViewModel.cs` defines private `CheckExpirationDate`.
- `ref/4.58.12-project/.../MainWindowViewModelService.cs` defines the method; ViewModel delegates to service.

Patches: `PatchMainWindowViewModel` / `PatchMainWindowViewModelService` in `PatchUtils.cs`.

### PSDZ ConfigurationService

| Range | Notes |
|-------|-------|
| `< 4.56` | `PatchPsdzConfigurationService` rewrites `SetPsdzProperties` IL |
| `>= 4.56` | Additional validation paths: `CommonServiceWrapper_IsAvailable`, dealer ctor XML, `HasProtectionVehicleService` |

### User environment / login

| Range | Library / type notes |
|-------|----------------------|
| `4.44–4.52` | PresentationFramework `UserEnvironmentProvider` |
| `>= 4.52` | `BMW.ISPI.TRIC.ISTA.LOGIN.dll` provider |
| Login options | PresentationFramework split around 4.48 / 4.52 |

See `FromVersion`/`UntilVersion` on methods in `PatchUtils.Optional.cs`.

## Essential Targets (always loaded)

High-value methods historically in `PatchUtils.cs` with `[EssentialPatch]`:

| Patch | Typical target |
|-------|----------------|
| `PatchIntegrityManager` | `…SecurityAndLicense.IntegrityManager..ctor` emptied |
| `PatchVerifyAssemblyHelper` | `VerifyStrongName` → true (+ related getter) |
| `PatchIstaIcsServiceClient` | ICS client checks |
| `PatchIstaProcessStarter` | process start guards |
| `PatchPackageValidityService` | package validity |

Re-audit these first on every new version.

## Validation Targets (Standalone mode)

License stack examples:

| Patch | Type / method (conceptual) |
|-------|----------------------------|
| `PatchLicenseStatusChecker` | `LicenseStatusChecker.IsLicenseValid` |
| `PatchLicenseWizardHelper` | `LicenseWizardHelper.DoLicenseCheck` |
| `PatchLicenseManager` | `VerifyLicense`, `CheckRITALicense`, compile-time license helpers |
| `PatchLicenseHelper` | `IsVehicleLockedDown` |
| `PatchCommonServiceWrapper_VerifyLicense` | ISPINext wrapper |
| `PatchSecureAccessHelper` | `IsCodeAccessPermitted` |
| `PatchActivationCertificateHelper` | whitelist helpers |
| `PatchServiceProgramCompilerLicense` | external tool license |

## Surgical Patch Anchors (fragile)

These need re-validation when body diffs appear:

| Patch | Anchor idea |
|-------|-------------|
| `PatchPsdzConfigurationService` | Calls to `get_BaseService`, `get/setPSdZProperties`, `PutProperty` |
| `PatchInitializeEnetDevice` | 2nd `apiInitExt`; string param before call; `get_IPAddress` |
| `PatchOperationFinishedListViewModel` | `IsProgrammingEnabled` call → force true |
| `PatchDealerConstructor` | `ldstr` XML with `distributionPartnerNumber="00000"` |
| `PatchIstaInstallationRequirements` | `Dictionary<,>` ctor for empty requirements map |
| Clamp / SLP / DS2 patches | Multiple sequence points in large methods — re-read whole method |

## Diff Commands (copy/paste)

```bash
REPO=ISTA-Patcher
DATA=../ista-test-data
OLD=4.53.13-project
NEW=4.58.12-project   # or path under ref/

# Project-level file presence
diff -rq "$DATA/$OLD/RheingoldCoreFramework" "$DATA/$NEW/RheingoldCoreFramework" | head -80

# Keyword sweep for new gates
grep -RInE 'License|VerifyStrongName|IntegrityManager|CheckExpiration|IsLicenseValid' \
  "$DATA/$NEW" --include='*.cs' | head -100

# Find defining file for a type
find "$DATA/$NEW" -name 'LicenseStatusChecker.cs'

# Compare a single method area
diff -u \
  "$DATA/$OLD/ISTAGUI/BMW/Rheingold/ISTAGUI/ViewModels/MainWindowViewModel.cs" \
  "$DATA/$NEW/ISTAGUI/BMW/Rheingold/ISTAGUI/ViewModels/MainWindowViewModel.cs"
```

## Dry-Run Interpretation

CLI result string is one character per loaded patch (same order as provider list):

- `1`–`F` : applied count in hex for that patch on this file
- `-` : skipped or miss

Footer:

```text
=== ISTA Patch Done in mm:ss.fff [applied/attempted patches, N functions, S skipped] ===
```

`skipped` = patches never attempted (version/library filter), not necessarily failures.
`applied < attempted` means some attempted patches returned 0 (miss / no body change).

## Adding a New Optional Flag (full surface)

1. Attribute in `AttributeUtils.cs`.
2. Property on `ISTAOptions.OptionalPatchOptions`.
3. Branch in `DefaultPatcherProvider` constructor.
4. CLI option on patch command (DotMake kebab-case).
5. GUI usually picks up via reflection; only special-case if metadata insufficient.
6. Patch method(s) with the new attribute + `LibraryName` + version bounds.
7. Tests for any new helper logic; optional doc in CHANGELOG.

## Related Code Entry Points

| Concern | File |
|---------|------|
| Apply loop | `src/ISTAlter/Core/Patch.cs` |
| Provider selection | `src/ISTAlter/Core/Patcher/Provider/DefaultPatcherProvider.cs` |
| Fingerprint / success | `PatchUtils.Base.cs` → `ComputeBodyFingerprint` |
| Custom JSON patches | `CustomPatchUtils.cs`, `Models/CustomPatchDefinition.cs` |
| Path constants | `Utils/Constants.cs` |
