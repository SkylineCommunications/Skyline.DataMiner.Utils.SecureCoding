namespace Skyline.DataMiner.Utils.SecureCoding.SecureReflection
{
    using System;
    using System.Collections.Generic;
    using System.Formats.Asn1;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Pkcs;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Managed Authenticode verifier used on platforms where <c>WinVerifyTrust</c> is not available.
    /// </summary>
    /// <remarks>
    /// A Portable Executable (PE) image is the binary contents of an executable or DLL, not a graphical image.
    /// Implements the Authenticode PE image hashing rules (the checksum, the security data directory entry and the
    /// certificate table itself are excluded from the hash) and validates the embedded PKCS#7 <c>SignedData</c>
    /// structure using <see cref="SignedCms"/>.
    /// </remarks>
    internal static class AuthenticodeVerifier
    {
        private const string SpcIndirectDataObjId = "1.3.6.1.4.1.311.2.1.4";
        private const string CodeSigningEkuOid = "1.3.6.1.5.5.7.3.3";
        private const string TimestampEkuOid = "1.3.6.1.5.5.7.3.8";
        private const string Rfc3161Oid = "1.3.6.1.4.1.311.3.3.1";
        private const string PeImageDataOid = "1.3.6.1.4.1.311.2.1.15";
        private const int MaximumPeImageSize = 256 * 1024 * 1024;

        private const ushort Pe32Magic = 0x10B;
        private const ushort Pe32PlusMagic = 0x20B;
        private const ushort WinCertRevision2 = 0x0200;
        private const ushort WinCertTypePkcsSignedData = 0x0002;
        private const int SecurityDirectoryIndex = 4;

        /// <summary>
        /// Retrieves the certificate of the first signer of the first Authenticode signature of the specified file.
        /// </summary>
        /// <param name="filePath">The full path to the signed file.</param>
        /// <returns>The signing certificate, or <see langword="null"/> when the file is not signed.</returns>
        /// <exception cref="CryptographicException">Thrown when Authenticode content is detected but cannot be decoded.</exception>
        internal static X509Certificate2 GetSigningCertificate(string filePath)
        {
            var fileBytes = ReadPeImage(filePath);

            if (!PeImage.TryParse(fileBytes, out var image) || !image.TryGetSignedData(out var signedData))
            {
                return null;
            }

            var cms = new SignedCms();
            cms.Decode(signedData);

            if (cms.SignerInfos.Count != 1)
            {
                throw new CryptographicException("The Authenticode signature does not contain any signer.");
            }

            var signer = cms.SignerInfos[0].Certificate;
            if (signer is null)
            {
                throw new CryptographicException("The Authenticode signer certificate could not be found.");
            }

            return new X509Certificate2(signer);
        }

        /// <summary>
        /// Reads the binary contents of a Portable Executable (PE) file within the supported size limit.
        /// </summary>
        /// <param name="filePath">The full path to the executable or DLL.</param>
        /// <returns>The PE file bytes, which have not yet been parsed or signature-validated.</returns>
        internal static byte[] ReadPeImage(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length == 0 || stream.Length > MaximumPeImageSize)
                {
                    throw new SecurityException("Assembly size is outside the supported range (1 byte to 256 MiB).");
                }

                var bytes = new byte[(int)stream.Length];
                var offset = 0;
                while (offset < bytes.Length)
                {
                    var count = stream.Read(bytes, offset, bytes.Length - offset);
                    if (count == 0)
                    {
                        throw new SecurityException("The assembly changed while it was being read.");
                    }

                    offset += count;
                }

                if (stream.ReadByte() != -1)
                {
                    throw new SecurityException("The assembly changed while it was being read.");
                }

                return bytes;
            }
        }

        /// <summary>
        /// Validates the Authenticode signature and allowed signer of the supplied Portable Executable (PE) file bytes.
        /// </summary>
        /// <param name="peImageBytes">The binary contents of the executable or DLL to validate.</param>
        /// <param name="verifyCertificateChain">Whether to verify the signer's certificate chain against the machine's trusted roots.</param>
        /// <param name="allowedCertificates">The certificates permitted to sign the PE file.</param>
        internal static void ValidatePeImage(byte[] peImageBytes, bool verifyCertificateChain, X509Certificate2[] allowedCertificates)
        {
            if (peImageBytes == null)
            {
                throw new ArgumentNullException(nameof(peImageBytes));
            }

            if (allowedCertificates == null || !allowedCertificates.Any(certificate => certificate != null))
            {
                throw new ArgumentException("At least one allowed certificate is required.", nameof(allowedCertificates));
            }

            if (peImageBytes.Length == 0 || peImageBytes.Length > MaximumPeImageSize)
            {
                throw new SecurityException("Assembly size is outside the supported range.");
            }

            try
            {
                if (!TryValidateSignature(peImageBytes, verifyCertificateChain, allowedCertificates, out var result))
                {
                    throw new SecurityException($"Attempt to load an assembly with an invalid signature: {result}");
                }
            }
            catch (CryptographicException ex)
            {
                throw new SecurityException("The assembly's Authenticode signature could not be verified.", ex);
            }
            catch (AsnContentException ex)
            {
                throw new SecurityException("The assembly's Authenticode data is malformed.", ex);
            }
        }

        /// <summary>
        /// Validates the Authenticode signature of the specified file.
        /// </summary>
        /// <param name="filePath">The full path to the file to validate.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/>, the certificate chain of the signer is verified against the trusted roots of the machine;
        /// if <see langword="false"/>, only the file hash and the signature are verified.
        /// </param>
        /// <param name="result">The verification result, mirroring the <c>WinVerifyTrust</c> result codes.</param>
        /// <returns><see langword="true"/> when the signature is valid; otherwise <see langword="false"/>.</returns>
        internal static bool TryValidateSignature(string filePath, bool verifyCertificateChain, out SecureAssembly.WinVerifyTrustResult result)
        {
            return TryValidateSignature(ReadPeImage(filePath), verifyCertificateChain, null, out result);
        }

        private static bool TryValidateSignature(byte[] fileBytes, bool verifyCertificateChain, X509Certificate2[] allowedCertificates, out SecureAssembly.WinVerifyTrustResult result)
        {
            if (!PeImage.TryParse(fileBytes, out var image))
            {
                result = SecureAssembly.WinVerifyTrustResult.SubjectFormUnknown;
                return false;
            }

            if (!image.TryGetSignedData(out var signedData))
            {
                result = SecureAssembly.WinVerifyTrustResult.FileNotSigned;
                return false;
            }

            SignedCms cms;
            SpcIndirectData indirectData;
            try
            {
                cms = new SignedCms();
                cms.Decode(signedData);

                if (cms.SignerInfos.Count != 1 || cms.ContentInfo.ContentType.Value != SpcIndirectDataObjId ||
                    !SpcIndirectData.TryParse(cms.ContentInfo.Content, out indirectData))
                {
                    result = SecureAssembly.WinVerifyTrustResult.SubjectFormUnknown;
                    return false;
                }

                var signer = cms.SignerInfos[0];
                var signerCertificate = signer.Certificate;
                if (signerCertificate == null)
                {
                    result = SecureAssembly.WinVerifyTrustResult.SubjectNotTrusted;
                    return false;
                }

                // Reject unapproved signers before cryptographic verification; a match does not establish trust.
                if (allowedCertificates != null && !allowedCertificates.Any(certificate => certificate != null && certificate.RawData.SequenceEqual(signerCertificate.RawData)))
                {
                    throw new SecurityException("The certificate of the assembly does not match the provided certificate.");
                }

                if (!HasStrongSignature(signer) || !HasEku(signer.Certificate, CodeSigningEkuOid))
                {
                    result = SecureAssembly.WinVerifyTrustResult.SubjectNotTrusted;
                    return false;
                }

                // Verifies the signer's signature over the signed attributes and the message digest over the content.
                signer.CheckSignature(verifySignatureOnly: true);
            }
            catch (CryptographicException)
            {
                result = SecureAssembly.WinVerifyTrustResult.SignatureOrFileCorrupt;
                return false;
            }

            if (!TryGetHashAlgorithm(indirectData.DigestAlgorithmOid, out var hashAlgorithm))
            {
                result = SecureAssembly.WinVerifyTrustResult.SubjectFormUnknown;
                return false;
            }

            var computedHash = image.ComputeAuthenticodeHash(hashAlgorithm);
            if (!computedHash.SequenceEqual(indirectData.Digest))
            {
                result = SecureAssembly.WinVerifyTrustResult.SignatureOrFileCorrupt;
                return false;
            }

            return TryValidateSignerTrust(cms, verifyCertificateChain, out result);
        }

        private static bool TryValidateSignerTrust(SignedCms cms, bool verifyCertificateChain, out SecureAssembly.WinVerifyTrustResult result)
        {
            if (cms.SignerInfos.Count < 1 || cms.SignerInfos[0].Certificate is null)
            {
                result = SecureAssembly.WinVerifyTrustResult.SignatureOrFileCorrupt;
                return false;
            }

            if (!verifyCertificateChain)
            {
                result = SecureAssembly.WinVerifyTrustResult.Success;
                return true;
            }

            var verificationTime = DateTime.UtcNow;
            if (!TryGetVerificationTime(cms.SignerInfos[0], out var timestamp))
            {
                result = SecureAssembly.WinVerifyTrustResult.SubjectNotTrusted;
                return false;
            }

            if (timestamp.HasValue)
            {
                verificationTime = timestamp.Value;
            }

            result = VerifyCertificateChain(cms.SignerInfos[0].Certificate, cms.Certificates, CodeSigningEkuOid, verificationTime);
            return result == SecureAssembly.WinVerifyTrustResult.Success;
        }

        private static bool HasStrongSignature(SignerInfo signer)
        {
            if (signer.Certificate == null || !TryGetHashAlgorithm(signer.DigestAlgorithm.Value, out _))
            {
                return false;
            }

            using (var rsa = signer.Certificate.GetRSAPublicKey())
            {
                if (rsa != null)
                {
                    return rsa.KeySize >= 2048;
                }
            }

            using (var ecdsa = signer.Certificate.GetECDsaPublicKey())
            {
                return ecdsa != null && ecdsa.KeySize >= 256;
            }
        }

        private static bool HasEku(X509Certificate2 certificate, string oid)
        {
            var extensions = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().ToArray();
            return extensions.Length == 1 && extensions[0].EnhancedKeyUsages.Cast<Oid>().Any(usage => usage.Value == oid);
        }

        private static bool TryGetVerificationTime(SignerInfo signer, out DateTime? time)
        {
            time = null;
            var attributes = signer.UnsignedAttributes.Cast<CryptographicAttributeObject>().Where(attribute => attribute.Oid.Value == Rfc3161Oid).ToArray();
            if (attributes.Length == 0)
            {
                // Legacy countersignatures are not used to extend the signing certificate's lifetime.
                return true;
            }

            if (attributes.Length != 1 || attributes[0].Values.Count != 1)
            {
                return false;
            }

#if NET8_0_OR_GREATER
            var encoded = attributes[0].Values[0].RawData;
            if (!TryValidateTimestampToken(encoded, signer, out var token, out var tsa))
            {
                return false;
            }

            using (tsa)
            {
                var instant = token.TokenInfo.Timestamp.UtcDateTime;
                if (VerifyCertificateChain(tsa, token.AsSignedCms().Certificates, TimestampEkuOid, instant) != SecureAssembly.WinVerifyTrustResult.Success)
                {
                    return false;
                }

                time = instant;
                return true;
            }
#else
            return false;
#endif
        }

#if NET8_0_OR_GREATER
        // Cryptographic validation only. The caller must separately establish TSA chain trust.
        internal static bool TryValidateTimestampToken(byte[] encoded, SignerInfo signer, out Rfc3161TimestampToken token, out X509Certificate2 tsa)
        {
            tsa = null;
            if (!Rfc3161TimestampToken.TryDecode(encoded, out token, out var consumed) || consumed != encoded.Length ||
                !TryGetHashAlgorithm(token.TokenInfo.HashAlgorithmId.Value, out _) || token.TokenInfo.Timestamp > DateTimeOffset.UtcNow)
            {
                return false;
            }

            var cms = token.AsSignedCms();
            return cms.SignerInfos.Count == 1 && HasStrongSignature(cms.SignerInfos[0]) &&
                token.VerifySignatureForSignerInfo(signer, out tsa, null);
        }
#endif

        private static SecureAssembly.WinVerifyTrustResult VerifyCertificateChain(X509Certificate2 signer, X509Certificate2Collection extraCertificates, string eku, DateTime verificationTime)
        {
            using (var chain = new X509Chain())
            {
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(15);
                chain.ChainPolicy.VerificationTime = verificationTime;
                chain.ChainPolicy.ApplicationPolicy.Add(new Oid(eku));
                chain.ChainPolicy.ExtraStore.AddRange(extraCertificates);

                if (chain.Build(signer))
                {
                    return SecureAssembly.WinVerifyTrustResult.Success;
                }

                var statuses = chain.ChainStatus.Select(status => status.Status).ToArray();

                if (statuses.Any(status => (status & X509ChainStatusFlags.Revoked) != 0))
                {
                    return SecureAssembly.WinVerifyTrustResult.SubjectCertificateRevoked;
                }

                if (statuses.Any(status => (status & X509ChainStatusFlags.ExplicitDistrust) != 0))
                {
                    return SecureAssembly.WinVerifyTrustResult.SubjectExplicitlyDistrusted;
                }

                if (statuses.Any(status => (status & (X509ChainStatusFlags.UntrustedRoot | X509ChainStatusFlags.PartialChain)) != 0))
                {
                    return SecureAssembly.WinVerifyTrustResult.UntrustedRoot;
                }

                if (statuses.Any(status => (status & X509ChainStatusFlags.NotTimeValid) != 0))
                {
                    return SecureAssembly.WinVerifyTrustResult.SubjectCertExpired;
                }

                return SecureAssembly.WinVerifyTrustResult.SubjectNotTrusted;
            }
        }

        private static bool TryGetHashAlgorithm(string oid, out HashAlgorithmName hashAlgorithm)
        {
            switch (oid)
            {
                case "2.16.840.1.101.3.4.2.1":
                    hashAlgorithm = HashAlgorithmName.SHA256;
                    return true;
                case "2.16.840.1.101.3.4.2.2":
                    hashAlgorithm = HashAlgorithmName.SHA384;
                    return true;
                case "2.16.840.1.101.3.4.2.3":
                    hashAlgorithm = HashAlgorithmName.SHA512;
                    return true;
                default:
                    hashAlgorithm = default;
                    return false;
            }
        }

        /// <summary>
        /// Minimal PE image reader exposing the information required for Authenticode verification.
        /// </summary>
        private sealed class PeImage
        {
            private readonly byte[] bytes;
            private readonly int checksumOffset;
            private readonly int securityDirectoryEntryOffset;
            private readonly int sizeOfHeaders;
            private readonly int certificateTableOffset;
            private readonly int certificateTableSize;
            private readonly List<(int Pointer, int Size)> sections;

            private PeImage(byte[] bytes, int checksumOffset, int securityDirectoryEntryOffset, int sizeOfHeaders, int certificateTableOffset, int certificateTableSize, List<(int, int)> sections)
            {
                this.bytes = bytes;
                this.checksumOffset = checksumOffset;
                this.securityDirectoryEntryOffset = securityDirectoryEntryOffset;
                this.sizeOfHeaders = sizeOfHeaders;
                this.certificateTableOffset = certificateTableOffset;
                this.certificateTableSize = certificateTableSize;
                this.sections = sections;
            }

            internal static bool TryParse(byte[] bytes, out PeImage image)
            {
                image = null;

                if (!BitConverter.IsLittleEndian || bytes.Length < 0x40 || bytes[0] != (byte)'M' || bytes[1] != (byte)'Z')
                {
                    return false;
                }

                var peHeaderOffset = BitConverter.ToInt32(bytes, 0x3C);
                if (peHeaderOffset < 0x40 || peHeaderOffset > bytes.Length - 24 ||
                    bytes[peHeaderOffset] != (byte)'P' || bytes[peHeaderOffset + 1] != (byte)'E' ||
                    bytes[peHeaderOffset + 2] != 0 || bytes[peHeaderOffset + 3] != 0)
                {
                    return false;
                }

                var coffHeaderOffset = peHeaderOffset + 4;
                int numberOfSections = BitConverter.ToUInt16(bytes, coffHeaderOffset + 2);
                int sizeOfOptionalHeader = BitConverter.ToUInt16(bytes, coffHeaderOffset + 16);
                var optionalHeaderOffset = coffHeaderOffset + 20;

                if (sizeOfOptionalHeader > bytes.Length - optionalHeaderOffset || sizeOfOptionalHeader < 2 || numberOfSections == 0 || numberOfSections > 96)
                {
                    return false;
                }

                var magic = BitConverter.ToUInt16(bytes, optionalHeaderOffset);
                int numberOfRvaAndSizesOffset;
                int dataDirectoriesOffset;

                if (magic == Pe32Magic)
                {
                    numberOfRvaAndSizesOffset = optionalHeaderOffset + 92;
                    dataDirectoriesOffset = optionalHeaderOffset + 96;
                }
                else if (magic == Pe32PlusMagic)
                {
                    numberOfRvaAndSizesOffset = optionalHeaderOffset + 108;
                    dataDirectoriesOffset = optionalHeaderOffset + 112;
                }
                else
                {
                    return false;
                }

                var checksumOffset = optionalHeaderOffset + 64;
                var sizeOfHeadersOffset = optionalHeaderOffset + 60;
                var securityDirectoryEntryOffset = dataDirectoriesOffset + (SecurityDirectoryIndex * 8);

                var optionalHeaderEnd = optionalHeaderOffset + sizeOfOptionalHeader;
                if (securityDirectoryEntryOffset > optionalHeaderEnd - 8)
                {
                    return false;
                }

                var numberOfRvaAndSizes = BitConverter.ToUInt32(bytes, numberOfRvaAndSizesOffset);
                var sizeOfHeaders = BitConverter.ToUInt32(bytes, sizeOfHeadersOffset);

                if (numberOfRvaAndSizes <= SecurityDirectoryIndex || numberOfRvaAndSizes > (optionalHeaderEnd - dataDirectoriesOffset) / 8)
                {
                    return false;
                }

                if (sizeOfHeaders > bytes.Length || sizeOfHeaders < (uint)(securityDirectoryEntryOffset + 8))
                {
                    return false;
                }

                uint certificateTableOffset = 0;
                uint certificateTableSize = 0;

                if (numberOfRvaAndSizes > SecurityDirectoryIndex)
                {
                    certificateTableOffset = BitConverter.ToUInt32(bytes, securityDirectoryEntryOffset);
                    certificateTableSize = BitConverter.ToUInt32(bytes, securityDirectoryEntryOffset + 4);

                    if (certificateTableSize != 0 && (certificateTableOffset > bytes.Length || certificateTableSize > bytes.Length - certificateTableOffset))
                    {
                        return false;
                    }
                }

                var sectionTableOffset = optionalHeaderOffset + sizeOfOptionalHeader;
                if (sectionTableOffset + (numberOfSections * 40) > sizeOfHeaders)
                {
                    return false;
                }

                var sections = new List<(int, int)>(numberOfSections);
                for (var i = 0; i < numberOfSections; i++)
                {
                    var sectionOffset = sectionTableOffset + (i * 40);
                    var sizeOfRawData = BitConverter.ToUInt32(bytes, sectionOffset + 16);
                    var pointerToRawData = BitConverter.ToUInt32(bytes, sectionOffset + 20);

                    if (sizeOfRawData == 0)
                    {
                        continue;
                    }

                    if (pointerToRawData > bytes.Length || sizeOfRawData > bytes.Length - pointerToRawData)
                    {
                        return false;
                    }

                    sections.Add(((int)pointerToRawData, (int)sizeOfRawData));
                }

                // Deliberately reject ambiguous layouts instead of interpreting overlapping or gapped ranges.
                long end = sizeOfHeaders;
                foreach (var section in sections.OrderBy(section => section.Item1))
                {
                    if (section.Item1 != end)
                    {
                        return false;
                    }

                    end += section.Item2;
                }

                if (certificateTableSize != 0 &&
                    (certificateTableOffset < end || certificateTableOffset % 8 != 0 || certificateTableSize % 8 != 0 ||
                     (long)certificateTableOffset + certificateTableSize != bytes.Length))
                {
                    return false;
                }

                image = new PeImage(bytes, checksumOffset, securityDirectoryEntryOffset, (int)sizeOfHeaders, (int)certificateTableOffset, (int)certificateTableSize, sections);
                return true;
            }

            internal bool TryGetSignedData(out byte[] signedData)
            {
                signedData = null;

                if (certificateTableSize < 8)
                {
                    return false;
                }

                var length = BitConverter.ToUInt32(bytes, certificateTableOffset);
                var revision = BitConverter.ToUInt16(bytes, certificateTableOffset + 4);
                var certificateType = BitConverter.ToUInt16(bytes, certificateTableOffset + 6);

                if (length < 8 || length > (uint)certificateTableSize || ((long)length + 7) / 8 * 8 != certificateTableSize || revision != WinCertRevision2 || certificateType != WinCertTypePkcsSignedData)
                {
                    return false;
                }

                try
                {
                    var reader = new AsnReader(new ReadOnlyMemory<byte>(bytes, certificateTableOffset + 8, (int)length - 8), AsnEncodingRules.DER);
                    var encoded = reader.ReadEncodedValue();
                    // WIN_CERTIFICATE may contain up to seven alignment bytes, never arbitrary unsigned data.
                    var paddingStart = certificateTableOffset + 8 + encoded.Length;
                    if (bytes.Length - paddingStart > 7)
                    {
                        return false;
                    }

                    for (var i = paddingStart; i < bytes.Length; i++)
                    {
                        if (bytes[i] != 0)
                        {
                            return false;
                        }
                    }

                    signedData = encoded.ToArray();
                    return true;
                }
                catch (AsnContentException)
                {
                    return false;
                }
            }

            internal byte[] ComputeAuthenticodeHash(HashAlgorithmName hashAlgorithm)
            {
                using (var hash = IncrementalHash.CreateHash(hashAlgorithm))
                {
                    // Headers, excluding the checksum and the security data directory entry.
                    hash.AppendData(bytes, 0, checksumOffset);
                    hash.AppendData(bytes, checksumOffset + 4, securityDirectoryEntryOffset - (checksumOffset + 4));
                    hash.AppendData(bytes, securityDirectoryEntryOffset + 8, sizeOfHeaders - (securityDirectoryEntryOffset + 8));

                    // Section data, sorted by file offset.
                    var sumOfBytesHashed = sizeOfHeaders;
                    foreach (var section in sections.OrderBy(section => section.Pointer))
                    {
                        hash.AppendData(bytes, section.Pointer, section.Size);
                        sumOfBytesHashed += section.Size;
                    }

                    // Remaining data, excluding the certificate table itself.
                    if (bytes.Length > sumOfBytesHashed)
                    {
                        AppendExcludingCertificateTable(hash, sumOfBytesHashed, bytes.Length);
                    }

                    return hash.GetHashAndReset();
                }
            }

            private void AppendExcludingCertificateTable(IncrementalHash hash, int start, int end)
            {
                if (certificateTableSize == 0 || certificateTableOffset >= end || certificateTableOffset + certificateTableSize <= start)
                {
                    hash.AppendData(bytes, start, end - start);
                    return;
                }

                if (certificateTableOffset > start)
                {
                    hash.AppendData(bytes, start, certificateTableOffset - start);
                }

                var afterTable = certificateTableOffset + certificateTableSize;
                if (afterTable < end)
                {
                    hash.AppendData(bytes, afterTable, end - afterTable);
                }
            }
        }

        /// <summary>
        /// Represents the <c>SpcIndirectDataContent</c> structure carried as the content of an Authenticode signature.
        /// </summary>
        private sealed class SpcIndirectData
        {
            private SpcIndirectData(string digestAlgorithmOid, byte[] digest)
            {
                DigestAlgorithmOid = digestAlgorithmOid;
                Digest = digest;
            }

            internal string DigestAlgorithmOid { get; }

            internal byte[] Digest { get; }

            internal static bool TryParse(byte[] content, out SpcIndirectData indirectData)
            {
                indirectData = null;

                try
                {
                    var reader = new AsnReader(content, AsnEncodingRules.DER);

                    // Depending on the decoder, the content is either the full SEQUENCE or only its inner value.
                    var sequence = reader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence) && IsSingleElement(reader)
                        ? reader.ReadSequence()
                        : reader;

                    // SpcAttributeTypeAndOptionalValue
                    var data = sequence.ReadSequence();
                    if (data.ReadObjectIdentifier() != PeImageDataOid)
                    {
                        return false;
                    }

                    // The PE image data is metadata, not a path to open or trust.
                    if (data.HasData)
                    {
                        data.ReadEncodedValue();
                    }

                    data.ThrowIfNotEmpty();

                    // DigestInfo
                    var digestInfo = sequence.ReadSequence();
                    var algorithmIdentifier = digestInfo.ReadSequence();
                    var digestAlgorithmOid = algorithmIdentifier.ReadObjectIdentifier();
                    if (algorithmIdentifier.HasData)
                    {
                        algorithmIdentifier.ReadNull();
                    }

                    algorithmIdentifier.ThrowIfNotEmpty();
                    var digest = digestInfo.ReadOctetString();
                    digestInfo.ThrowIfNotEmpty();
                    sequence.ThrowIfNotEmpty();
                    reader.ThrowIfNotEmpty();

                    if (!TryGetHashAlgorithm(digestAlgorithmOid, out var algorithm) ||
                        digest.Length != (algorithm == HashAlgorithmName.SHA256 ? 32 : algorithm == HashAlgorithmName.SHA384 ? 48 : 64))
                    {
                        return false;
                    }

                    indirectData = new SpcIndirectData(digestAlgorithmOid, digest);
                    return true;
                }
                catch (AsnContentException)
                {
                    return false;
                }
            }

            private static bool IsSingleElement(AsnReader reader)
            {
                var probe = reader.Clone();
                probe.ReadEncodedValue();
                return !probe.HasData;
            }
        }
    }
}
