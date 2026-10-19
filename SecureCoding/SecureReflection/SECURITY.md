# SecureAssembly security profile

## Scope and trust boundary

This is an Authenticode verifier for the entry assembly, not a sandbox, malware detector, dependency verifier, or certification of Windows policy equivalence. Only load code from publishers you trust to execute with the application's permissions. Protect the certificate allowlist, application directory, dependencies, and native libraries from untrusted writes.

Both platforms load a private, bounded byte snapshot (maximum 256 MiB): read once, validate, then pass the same bytes to Assembly.Load. The source pathname is never reopened after validation. Standalone ValidateAssemblySignature uses the same validation pipeline but discards the snapshot: it remains a point-in-time check and does not secure a later caller-managed path load.

Linux verifies the snapshot's CMS signature and PE digest and compares its signing certificate against the allowlist in memory.

Windows retains native WinVerifyTrust rather than substituting the narrower managed Linux acceptance policy. It writes the snapshot to a randomly named, atomically create-new temporary file, denies write/delete sharing, and keeps its handle open throughout verification. WINTRUST_FILE_INFO supplies that handle; the filename is not the security identity. The allowlist is compared against the certificate returned from the same successful WinVerifyTrust provider state, not from a second pathname-based certificate extraction. Provider state is closed on success and failure, and the temporary file is deleted on handle closure. Native WinTrust imports are restricted to System32. Neither the source path nor the temporary path is used for assembly loading.

This closes the source-path validation-to-load race and the separate signer-extraction-to-validation race under the normal Windows filesystem/OS trust boundary. A newly created snapshot avoids pre-existing writable mappings to the validation file; merely locking the original and then loading its name would not provide this guarantee. Create-new collisions, sharing failures and temporary-storage failures fail closed, without falling back to path loading. This is not protection against an administrator/kernel adversary, a malicious filesystem, or code capable of modifying this process's memory or handles. It also does not authenticate the choice between two assemblies legitimately signed by an allowed publisher.

Windows requires writable temporary storage and incurs one additional bounded disk copy per validation. Use an appropriately protected local temporary directory; snapshots are not confidential (read sharing is allowed), and storage exhaustion can deny service. Delete-on-close is not secure erasure. Windows trust flags are unchanged, but HashOnlyFlag alone is insufficient: regression testing demonstrated that it accepts a corrupted CMS signature if the PE digest is unchanged. The helper now additionally calls CryptMsgControl(CMSG_CTRL_VERIFY_SIGNATURE_EX) against the message and signer certificate in the same provider state, including when chain verification is disabled. Thus chain-disabled mode requires a valid signature, matching PE digest and allowed signer, without requiring certificate-chain trust. This change does not introduce stronger revocation requirements or establish equivalence with Linux's chain policy. Trust evaluation can access certificate network endpoints.

## Linux acceptance policy

- Maximum image size: 256 MiB. Input is processed as a PE file, never executed during verification.
- PE32 and PE32+ on little-endian systems; 1–96 sections; directory probes stay inside the declared optional header and section headers inside SizeOfHeaders.
- Nonempty sections must be contiguous in file order and nonoverlapping. This deliberately rejects some unusual but potentially valid PE layouts.
- The certificate table must be aligned to eight bytes, after section data, and end at EOF. Exactly one WIN_CERTIFICATE entry and one primary CMS signer are supported. Only zero alignment padding (at most seven bytes) is allowed after the encoded CMS object.
- SHA-256, SHA-384, or SHA-512 for the PE digest and primary signer digest. RSA keys must be at least 2048 bits or ECDSA keys at least 256 bits. This is a leaf-signature policy, not a complete independent audit of every certificate-chain algorithm.
- An explicit code-signing EKU is required. The exact signer certificate DER must match an allowed certificate; the allowlist is not matched by a SHA-1 fingerprint.
- Nested signatures are not selected as alternate trust paths. Multiple table entries or primary signers fail closed. Legacy countersignatures do not extend signer validity.

`verifyCertificateChain: false` is explicit certificate pinning: signature, PE digest, leaf algorithm/EKU policy and allowlist are still checked, but chain trust, certificate dates, timestamps and revocation are NOT checked. An expired/self-signed pinned certificate may be accepted. This is not full PKI validation.

`verifyCertificateChain: true` additionally requires an OS-trusted chain, code-signing EKU, validity at the verification time, and online revocation (excluding the root) with no ignored chain errors. Revocation unknown/offline fails closed. Retrieval timeout is 15 seconds per chain policy; this is not a global cancellation/deadline guarantee. Chain building may make network requests to certificate-provided endpoints; use network controls when processing hostile inputs.

On the net10.0 asset, a single Microsoft Authenticode RFC 3161 timestamp attribute is decoded with the .NET Rfc3161TimestampToken API. Full token consumption, strong digest/key, nonfuture time, signature binding, TSA signature and TSA chain trust are required before applying its time to the signer chain. The netstandard2.0 asset cannot use this API and rejects RFC3161 chain-validation requests. It does not implement a substitute timestamp parser. Legacy timestamp-only files are evaluated at the current time.

Linux trust anchors, revocation availability, and historical chain policy differ from Windows. Publicly signed fixtures can fail on a clean/offline machine. Do not suppress failures to make those fixtures pass.

## Loading compatibility

On the net10.0 asset, certificate path and byte-array overloads use `X509CertificateLoader` and accept single X.509 certificates (DER or PEM), not PKCS12/PFX or PKCS7 containers. Load a container explicitly and pass an `X509Certificate2` instance when needed. The netstandard2.0 asset retains its legacy certificate constructors for full Framework compatibility.

Both public loader families now use Assembly.Load(byte[]) on Windows as well as Linux. **This is a behavioral breaking change on Windows**, despite unchanged public signatures and target frameworks. Location is empty; load-context identity, repeat-load behavior and dependency resolution differ from Assembly.LoadFrom/LoadFile. Mixed-mode C++/CLI assemblies and multi-module/path-dependent deployments are not supported by this snapshot-loading contract. Dependencies are not resolved from the untrusted input directory automatically by this helper and are not transitively validated. Consumers needing custom dependency loading must implement and secure that policy separately; do not reintroduce a source-path load as a fallback.

The library continues to target netstandard2.0 and net10.0; .NET Framework 4.8.1 consumes the netstandard2.0 asset. Snapshot loading, FileStream/SafeFileHandle, the WinTrust provider APIs, and the interop declarations are compatible with these targets. The original per-file 256 MiB Linux limit now applies on Windows too. Per-call limits do not bound total concurrent memory or temporary-disk use. Native timestamp/trust decisions may depend on the host; verification of a temporary file is not a promise of original-path policy equivalence.

## Research references

Reviewed during implementation; these sources are references, not endorsements of this library:

- Microsoft PE specification: https://learn.microsoft.com/en-us/windows/win32/debug/pe-format
- Microsoft WinVerifyTrust: https://learn.microsoft.com/en-us/windows/win32/api/wintrust/nf-wintrust-winverifytrust
- Handle-bound WINTRUST_FILE_INFO: https://learn.microsoft.com/en-us/windows/win32/api/wintrust/ns-wintrust-wintrust_file_info
- WinTrust state lifecycle: https://learn.microsoft.com/en-us/windows/win32/api/wintrust/ns-wintrust-wintrust_data
- Verified signer lookup: https://learn.microsoft.com/en-us/windows/win32/api/wintrust/nf-wintrust-wthelpergetprovsignerfromchain
- Native CMS signature verification: https://learn.microsoft.com/en-us/windows/win32/api/wincrypt/nf-wincrypt-cryptmsgcontrol
- Provider message layout: https://learn.microsoft.com/en-us/windows/win32/api/wintrust/ns-wintrust-crypt_provider_data
- Assembly.Load(byte[]) semantics: https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.load
- Microsoft Authenticode timestamps: https://learn.microsoft.com/en-us/windows/win32/seccrypto/time-stamping-authenticode-signatures
- RFC 3161: https://www.rfc-editor.org/rfc/rfc3161
- .NET timestamp binding API: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.pkcs.rfc3161timestamptoken.verifysignatureforsignerinfo
- osslsigncode (maintained C/OpenSSL PE verifier, not a C# library): https://github.com/mtrojnar/osslsigncode (README.md and pe.c)
- AuthenticodeExaminer explicitly wraps Windows native APIs and is Windows-only: https://github.com/vcsjones/AuthenticodeExaminer
- Microsoft NuGet trust verifier (C#, package signatures, NOT PE Authenticode): https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Packaging/Signing/Verification/SignatureTrustAndValidityVerificationProvider.cs

No claim is made that a maintained drop-in, cross-platform C# WinVerifyTrust equivalent was found. The custom PE acceptance profile still needs independent specialist review and broader conformance/fuzz corpora. Tests do not establish absolute security.

## Validation evidence

### Current assurance follow-up

- The latest workspace contains the snapshot implementation and tests. A previous intermediate workspace inspection found a different path-loading version; observations and test totals from that intermediate state are not evidence about this version.
- Fresh Visual Studio execution of all discovered solution tests: **382 passed, 0 failed, 82 skipped (464 total)**. The snapshot suite accounts for **52 passing cases across net481 and net10.0**, including the existing cross-platform tests. The net481 tests consume the netstandard2.0 library asset. Test-triggered builds compiled the changed tests successfully.
- Corrupted CMS signatures with unchanged PE digests are now tested through the public ValidateAssemblySignature, LoadFrom and LoadFile entry points in both chain modes, rather than only through the internal native helper. In chain-disabled mode, tests specifically require the CMS authentication failure, not an unrelated rejection.
- Debugger inspection on a 64-bit .NET 10 test host confirmed WinVerifyTrust returned Success with chain verification disabled, the provider message was nonzero, and CryptMsgControl used the same provider certificate. Execution reached the CMS rejection branch, and the test completed successfully with the expected SecurityException.
- New tests guard the native structure sizes and pointer offsets used by provider-message/certificate extraction and CryptMsgControl. They check the running process ABI only and do not simulate unexecuted architectures. The CRYPT_PROVIDER_DATA prefix through hMsg is 120 bytes on x64 (hMsg offset 112), or 64 bytes on x86 (offset 60). An initial incorrect expected x64 size in the new test was corrected after re-deriving the field alignment; no production interop change was necessary.
- New concurrent tests perform 16 snapshot verifications per case for both accepted and rejected signers, with independent certificate/handle ownership and deletion checks. These are regression checks, not a complete stress test or native allocation-leak analysis.
- The Linux runner explicitly allows the two additional Windows-only methods to skip; cross-platform snapshot tests remain required. The runner itself and actual Linux execution were not performed in this follow-up. A separate clean packaging/documentation build, independent x86/ARM64 runs, fresh dependency audit, controlled PKI matrix and fuzz campaign also remain unexecuted.
- Production verification/loading code was preserved because the current snapshot/CMS defense passed verification. This follow-up changed executable regression coverage, its Linux-runner integration and assurance documentation, not the public API or target frameworks.

### Remaining assurance/release gates

Before claiming broader assurance, define the supported loading/dependency contract and complete actual OS/architecture testing, controlled revoked/offline/expired/timestamp PKI scenarios, malformed/multiple-signature corpora, resource-exhaustion and native-lifetime review, fuzzing, dependency audit and a clean build/package verification. Tie retained reports to an exact source revision and configuration. Obtain independent specialist review of the custom Linux verifier and Windows interop. These gates are requirements, not completed evidence.

**Not certified:** no certification scheme, evaluated product boundary or independent certification result has been established. Passing tests and use of native cryptographic APIs do not certify the loader. Only claim the specific implemented controls and verification results documented here.

### Original Windows snapshot remediation evidence

- Visual Studio Test Explorer ran all discovered solution tests after the implementation changes: **368 passed, 0 failed, 82 skipped (450 total)**. The library results were **170 passed on net481** (consuming netstandard2.0) and **180 passed on net10.0**; the analyzer suite contributed 18 passes. The skipped tests are Linux-only runtime/filesystem cases on this Windows host.
- The new snapshot suite contributed **38 passing cases across the two library runtimes**. It covers source replacement/deletion at the validation-to-load boundary, native handle/path disagreement in both directions, temporary-file write/delete/rename denial and cleanup, invalid/tampered images, signer pinning, public snapshot loading and source release, the size bound, and CMS-signature corruption independent of the PE digest. Existing loader tests exercise all certificate overload families.
- Before explicit CryptMsgControl verification, the corrupted-CMS test failed in chain-disabled mode on both runtimes because no exception was thrown. After the fix, it passes in both modes on both runtimes. This establishes the observed regression for these fixtures, not comprehensive Authenticode conformance.
- Test execution rebuilt the changed library/test assets. A separate clean/full solution packaging/documentation build, actual Linux execution, the updated PowerShell runner, independent x86/ARM64 runs, dependency vulnerability audit, and controlled revoked/offline/historical PKI matrices were **not executed in this remediation session**. The available tools expose Visual Studio test/build-on-run operations but not a general build/PowerShell execution tool. The Linux runner's explicit skip allowlist and required snapshot tests were updated by source inspection only.
- The review was scoped to SecureAssembly, its verification/interop dependencies, related tests and test configuration. The test PFX/password is a pre-existing test fixture; it is not evidence of production credential exposure. No repository-wide clean-security claim is made. API reference links above were not independently retrieved during this session.

### Historical evidence

The following is historical evidence from before the .NET 10 / .NET Framework 4.8.1 retargeting, not validation of the current targets.

- Full solution CLI restore/build succeeded. Windows library suite: net8.0 307 passed / 34 skipped; net462 299 passed / 34 skipped; no failures.
- Actual Linux .NET 8 container: full library suite 184 passed / 32 skipped; separate reflection suite 35 passed / 1 skipped; no failures. Test totals differ because OSCondition excludes data-driven cases differently on each platform.
- Independent osslsigncode 2.5 with OpenSSL 3.0.22 accepted Signed.dll including its timestamp, rejected Tampered.dll with a digest mismatch, and rejected Unsigned.dll. This comparison covers those fixtures only, not policy equivalence.
- Public NuGet vulnerability audit including transitives reported no known vulnerabilities after PKCS/ASN.1 remediation to 10.0.13. This is a point-in-time result.
- Adversarial tests cover optional-header bounds, directory counts, overlapping sections, certificate position/padding, payload tampering, input size, snapshot replacement, random short inputs, weak signer digest/key, EKU, untrusted chain, RFC3161 binding/truncation/trailing bytes, and Linux byte loading.
- Missing assurance: controlled revoked/offline PKI and historical timestamp matrices, broad PE32+ and ECDSA fixtures, differential fuzzing, and external specialist review. No claim of complete certification or immunity to vulnerabilities is made.
- Visual Studio retained a single-target restore view during the edit and reported NETSDK1005. Full CLI solution restore/build succeeds; reload the project/solution if the IDE retains that state.
