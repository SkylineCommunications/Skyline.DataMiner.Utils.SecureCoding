# Testing locally and in CI

## 1. CI

`.github/workflows/linux-tests.yml` runs automatically on pushes and pull requests, or manually through **Actions > Linux Tests > Run workflow**. It runs on Ubuntu and invokes `scripts/Test-Linux.ps1` using PowerShell. TRX reports are uploaded even when tests fail.

## 2. Locally

Start Docker Desktop with its Linux engine, then run from the repository root:

```powershell
.\scripts\Test-Linux.ps1
```

This one command uses the .NET 8 SDK Linux image, which includes PowerShell. It requires Docker Desktop's Linux engine and network access to the image registry and NuGet. No Python or shell-script installation is needed. The source is mounted read-only and copied into a private build directory (including uncommitted changes, excluding build outputs). Results are written to `TestResults/linux`. Test failures or invalid reports fail the command.

The same script runs directly on Linux with PowerShell 7 and the .NET 8 SDK, without Docker. It resolves project paths relative to itself; `-ResultsDirectory` optionally changes the report destination.

## What the Linux runner covers

Both entry points use `scripts/Test-Linux.ps1`. It runs only tests in `SecureCoding.Test.SecureIO` and `SecureCoding.Test.SecureReflection`, targeting net8.0. This includes path construction, explicit Linux and Windows string rules, and reflection/Authenticode. Serialization and analyzer tests are excluded from both entry points. New tests within these two namespaces are automatically included by the namespace filter.

The script rejects zero-test runs and checks the fresh `library.trx` report. It fails on unexpected skips, failures, a missing report, or missing required coverage. A class prefix in `$required` checks that at least one test from that class passed; it does not select tests or prove that every method was discovered. All discovered tests matching `$testFilter` run regardless of this list. Only the Windows path class, Windows runtime-dispatch method and Windows/Linux signature-comparison method may skip. Update this explicit allowlist when adding a genuinely Windows-only test; do not broadly exclude test namespaces.

.NET Standard is a library contract, not a runnable test target. The net462 Windows tests exercise the netstandard2.0 library asset; net8.0 tests exercise the net8.0 asset on each OS. Linux does not run net462 or emulate Windows integration behavior.

## Path contracts and review scope

- `IsPathValid(path, platform)` is pure lexical policy and both platform datasets run everywhere.
- `IsPathValid(path)` and `CreateSecurePath` use the current OS. The extension datasets cross-check these APIs on their corresponding OS.
- Successful construction datasets assert both the expected path and extension validity. `PathContractTests` adds missing-segment, single-array, double-dot-name, rooted-sibling and filename-shape regressions.
- Linux lexical validation allows `:` and `\`. The filesystem abstraction can interpret them as drive/directory separators during construction, so lexical acceptance does **not** imply that every construction overload accepts the same input.
- Existing policy rejects any `..` substring and multiple `%` characters, not just literal traversal segments. Constructors now apply this policy to filename branches too. Multi-segment construction now rejects null/empty/whitespace elements consistently.
- Analyzer fixtures are copied beside the test assembly; .NET Framework scenario references come from NuGet rather than a hard-coded Windows installation path.

This is regression evidence, not proof of 100% correctness or filesystem security. Lexical path checks do not resolve symlinks, prevent filesystem races, or establish ownership/access permissions. Existing certificate-chain tests can depend on trust stores and network/revocation availability. Review the actual passed/skipped counts in the retained TRX reports rather than treating a green build or explicit-platform string test as proof of real Linux execution.

Test counts can change as tests are added; they are not hard-coded as coverage thresholds. A local Linux run does not replace the main CI workflow's Windows-specific and .NET Framework coverage.
