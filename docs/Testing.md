# Testing locally and in CI

## 1. CI

`.github/workflows/linux-tests.yml` runs automatically on pushes and pull requests, or manually through **Actions > Linux Tests > Run workflow**. It runs on Ubuntu and invokes `scripts/Test-Linux.ps1` using PowerShell. TRX reports are uploaded even when tests fail.

## 2. Locally

Start Docker Desktop with its Linux engine, then run from the repository root:

```powershell
.\scripts\Test-Linux.ps1
```

This one command uses the .NET 10 SDK Linux image, which includes PowerShell. It requires Docker Desktop's Linux engine and network access to the image registry and NuGet. No Python or shell-script installation is needed. The source is mounted read-only and copied into a private build directory (including uncommitted changes, excluding build outputs). Results are written to `TestResults/linux`. Test failures or invalid reports fail the command.

The same script runs directly on Linux with PowerShell 7 and the .NET 10 SDK, without Docker. It resolves project paths relative to itself; `-ResultsDirectory` optionally changes the report destination.

For the full Windows solution, use the .NET 10 SDK and the .NET Framework 4.8.1 runtime. Building the Framework target also requires its reference assemblies, either from the 4.8.1 Developer Pack or the SDK's implicit `Microsoft.NETFramework.ReferenceAssemblies` NuGet restore. Restore therefore needs access to NuGet when the targeting pack is not installed:

```powershell
dotnet build SecureCoding.sln
dotnet test SecureCoding.Test/SecureCoding.Test.csproj -f net10.0
dotnet test SecureCoding.Test/SecureCoding.Test.csproj -f net481
dotnet test Analyzers/Analyzers.Test/Analyzers.Test.csproj -f net10.0
```

The main CI workflow delegates to an externally maintained reusable workflow. Its hosted runners must provide the .NET 10 SDK and Framework 4.8.1 runtime/reference assemblies; the local Linux workflow installs the .NET 10 SDK explicitly.

## What the Linux runner covers

Both entry points use `scripts/Test-Linux.ps1`. It runs library tests in `SecureCoding.Test.SecureIO` and `SecureCoding.Test.SecureReflection`, targeting net10.0, plus the analyzer test suite. Library coverage includes path construction, explicit Linux and Windows string rules, and reflection/Authenticode. Serialization library tests are excluded by the filter. New library tests within these two namespaces are automatically included by the namespace filter.

The script rejects zero-test runs and checks the fresh `library.trx` report. It fails on unexpected skips, failures, a missing report, or missing required coverage. A class prefix in `$required` checks that at least one test from that class passed; it does not select tests or prove that every method was discovered. All discovered tests matching `$testFilter` run regardless of this list. Only the Windows path class, Windows runtime-dispatch method and Windows/Linux signature-comparison method may skip. Update this explicit allowlist when adding a genuinely Windows-only test; do not broadly exclude test namespaces.

.NET Standard is a library contract, not a runnable test target. The net481 Windows tests exercise the netstandard2.0 library asset; net10.0 tests exercise the net10.0 asset on each OS. Linux does not run net481 or emulate Windows integration behavior.

## Path contracts and review scope

- `IsPathValid(path, platform)` is pure lexical policy and both platform datasets run everywhere.
- `IsPathValid(path)` and `CreateSecurePath` use the current OS. The extension datasets cross-check these APIs on their corresponding OS.
- Successful construction datasets assert both the expected path and extension validity. `PathContractTests` adds missing-segment, single-array, double-dot-name, rooted-sibling and filename-shape regressions.
- Linux lexical validation allows `:` and `\`. The filesystem abstraction can interpret them as drive/directory separators during construction, so lexical acceptance does **not** imply that every construction overload accepts the same input.
- Existing policy rejects any `..` substring and multiple `%` characters, not just literal traversal segments. Constructors now apply this policy to filename branches too. Multi-segment construction now rejects null/empty/whitespace elements consistently.
- Analyzer fixtures are copied beside the test assembly; .NET Framework scenario references come from NuGet rather than a hard-coded Windows installation path.

This is regression evidence, not proof of 100% correctness or filesystem security. Lexical path checks do not resolve symlinks, prevent filesystem races, or establish ownership/access permissions. Existing certificate-chain tests can depend on trust stores and network/revocation availability. Review the actual passed/skipped counts in the retained TRX reports rather than treating a green build or explicit-platform string test as proof of real Linux execution.

Test counts can change as tests are added; they are not hard-coded as coverage thresholds. A local Linux run does not replace the main CI workflow's Windows-specific and .NET Framework coverage.
