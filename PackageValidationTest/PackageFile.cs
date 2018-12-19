using System;
using System.Collections.Generic;
using System.IO;
using AuthenticodeExaminer;
using NuGet.Packaging;

namespace PackageValidationTest
{
    public class PackageFile
    {
        public PackageFile(IPackageFile packageFile, IEnumerable<AuthenticodeSignature> signatures, SignatureCheckResult isValidSignature, long fileSize)
        {
            PkgFile = packageFile;
            Sigs = signatures;
            IsValidSignature = isValidSignature;
            FileSize = fileSize;
        }

        public IPackageFile PkgFile { get; }
        public IEnumerable<AuthenticodeSignature> Sigs { get; }
        public SignatureCheckResult IsValidSignature { get; }
        public long FileSize { get; }
        public string FilePath => PkgFile.Path;
        public string Extension => Path.GetExtension(FilePath);
        public bool IsDll => string.Compare(Extension, ".dll", StringComparison.OrdinalIgnoreCase) == 0;

        public override string ToString()
        {
            string validSig = IsDll ? IsValidSignature.ToString() : "N/A";
            return $"Name: ({PkgFile.Path}) IsValidSig: {validSig}  FileSize({FileSize})";
        }
    }
}
