# SecureAssembly security profile

## Scope and trust boundary

This is an Authenticode verifier for the entry assembly, not a sandbox, malware detector, dependency verifier, or certification of Windows policy equivalence. Only load code from publishers you trust to execute with the application's permissions. Protect the certificate allowlist, application directory, dependencies, and native libraries from untrusted writes.

Linux loads a private, bounded byte snapshot: read once, verify its CMS signature and PE digest, compare its signing certificate against the allowlist, then pass the same bytes to Assembly.Load. No path is reopened after verification. Standalone ValidateAssemblySignature is only a point-in-time check and does not secure a later caller-managed path load.

Windows retains WinVerifyTrust and Assembly.LoadFrom/LoadFile semantics. The path must reside in a directory protected from replacement and modification; the Windows validation-to-load pathname race is NOT eliminated by this change.

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

On Linux both public loader families use Assembly.Load(byte[]). Location is empty; load-context identity and dependency resolution differ from Assembly.LoadFrom/LoadFile. Dependencies are not resolved from the untrusted input directory automatically by this helper. Consumers needing custom dependency loading must implement and secure that policy separately. Windows loader behavior is unchanged.

## Research references

Reviewed during implementation; these sources are references, not endorsements of this library:

- Microsoft PE specification: https://learn.microsoft.com/en-us/windows/win32/debug/pe-format
- Microsoft Authenticode timestamps: https://learn.microsoft.com/en-us/windows/win32/seccrypto/time-stamping-authenticode-signatures
- RFC 3161: https://www.rfc-editor.org/rfc/rfc3161
- .NET timestamp binding API: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.pkcs.rfc3161timestamptoken.verifysignatureforsignerinfo
- osslsigncode (maintained C/OpenSSL PE verifier, not a C# library): https://github.com/mtrojnar/osslsigncode (README.md and pe.c)
- AuthenticodeExaminer explicitly wraps Windows native APIs and is Windows-only: https://github.com/vcsjones/AuthenticodeExaminer
- Microsoft NuGet trust verifier (C#, package signatures, NOT PE Authenticode): https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Packaging/Signing/Verification/SignatureTrustAndValidityVerificationProvider.cs

No claim is made that a maintained drop-in, cross-platform C# WinVerifyTrust equivalent was found. The custom PE acceptance profile still needs independent specialist review and broader conformance/fuzz corpora. Tests do not establish absolute security.

## Validation evidence

The following is historical evidence from before the .NET 10 / .NET Framework 4.8.1 retargeting, not validation of the current targets.

- Full solution CLI restore/build succeeded. Windows library suite: net8.0 307 passed / 34 skipped; net462 299 passed / 34 skipped; no failures.
- Actual Linux .NET 8 container: full library suite 184 passed / 32 skipped; separate reflection suite 35 passed / 1 skipped; no failures. Test totals differ because OSCondition excludes data-driven cases differently on each platform.
- Independent osslsigncode 2.5 with OpenSSL 3.0.22 accepted Signed.dll including its timestamp, rejected Tampered.dll with a digest mismatch, and rejected Unsigned.dll. This comparison covers those fixtures only, not policy equivalence.
- Public NuGet vulnerability audit including transitives reported no known vulnerabilities after PKCS/ASN.1 remediation to 10.0.13. This is a point-in-time result.
- Adversarial tests cover optional-header bounds, directory counts, overlapping sections, certificate position/padding, payload tampering, input size, snapshot replacement, random short inputs, weak signer digest/key, EKU, untrusted chain, RFC3161 binding/truncation/trailing bytes, and Linux byte loading.
- Missing assurance: controlled revoked/offline PKI and historical timestamp matrices, broad PE32+ and ECDSA fixtures, differential fuzzing, and external specialist review. No claim of complete certification or immunity to vulnerabilities is made.
- Visual Studio retained a single-target restore view during the edit and reported NETSDK1005. Full CLI solution restore/build succeeds; reload the project/solution if the IDE retains that state.
