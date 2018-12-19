using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AuthenticodeExaminer;
using NuGetPe;

namespace PackageValidationTest
{
    public class PackageValidator
    {
        private readonly CommandLineOptions _options;
        public PackageValidator(CommandLineOptions options)
        {
            _options = options;
        }

        public async Task VerifyPackageAsync()
        {
            Console.WriteLine($"Loading {_options.FilePath}");
            var zipPackage = new ZipPackage(_options.FilePath);
            await zipPackage.VerifySignatureAsync();

            var files = zipPackage.GetFiles();

            var pkgFiles = new List<PackageFile>();

            foreach (var file in files)
            {
                Console.Write(".");
                string fileName = Path.GetFileName(file.Path);

                IReadOnlyList<AuthenticodeSignature> sigs;
                SignatureCheckResult isValidSig;
                using (var str = file.GetStream())
                using (var tempFile = new TemporaryFile(str, Path.GetExtension(fileName)))
                {
                    var extractor = new FileInspector(tempFile.FileName);

                    sigs = extractor.GetSignatures().ToList();
                    isValidSig = extractor.Validate();

                    long size = tempFile.Length;

                    pkgFiles.Add(new PackageFile(file, sigs, isValidSig, size));
                }
            }
            Console.WriteLine();

            ValidatePackage(zipPackage, pkgFiles);

            if (_options.IsVerbose)
            {
                ConsoleDumpDetails(zipPackage, pkgFiles);
            }
        }

        private const string ExpectedDescription = "This package contains the binaries of the Microsoft Authentication Library (MSAL).\n      MSAL makes it easy to obtain tokens from Azure AD v2 (work & school accounts, MSA) and Azure AD B2C, gaining access to Microsoft Cloud API and any other API secured by Microsoft identities. This version supports adding authentication functionality to your .NET based client on Windows desktop (.NET 4.5+), UWP, .NET Core, Xamarin iOS and Xamarin Android.";

        private void ValidatePackage(ZipPackage zipPackage, List<PackageFile> pkgFiles)
        {
            ValidateString("Title", "Microsoft Authentication Library for .NET", zipPackage.Title);
            ValidateString("Version", _options.ExpectedVersion, zipPackage.Version?.ToString());
            ValidateString("Authors", "Microsoft", string.Join(",", zipPackage?.Authors));

            ValidateString("Copyright", "© Microsoft Corporation. All rights reserved.", zipPackage.Copyright);
            ValidateString("Description", ExpectedDescription , zipPackage.Description);
            ValidateString("FullName", $"Microsoft.Identity.Client {_options.ExpectedVersion}" , zipPackage.GetFullName());
            ValidateString("IconUrl", null, zipPackage.IconUrl?.ToString());
            ValidateString("Id", "Microsoft.Identity.Client", zipPackage.Id);
            ValidateBool("IsPrerelease", false, zipPackage.IsPrerelease);
            ValidateBool("IsReleaseVersion", false, zipPackage.IsReleaseVersion());
            ValidateString("LicenseUrl", "https://go.microsoft.com/fwlink/?linkid=844762", zipPackage.LicenseUrl?.ToString());
            ValidateString("ProjectUrl", "https://go.microsoft.com/fwlink/?linkid=844761", zipPackage.ProjectUrl?.ToString());
            ValidateString("ReportAbuseUrl", null, zipPackage.ReportAbuseUrl?.ToString());
            ValidateString("Repository", null, zipPackage.Repository?.ToString());
            ValidateBool("RequireLicenseAcceptance", true, zipPackage.RequireLicenseAcceptance);
            ValidateBool("Serviceable", false, zipPackage.Serviceable);
            ValidateString("Summary", null, zipPackage.Summary);
            ValidateString("Tags", " Microsoft Authentication Library MSA MSAL B2C Azure Active Directory AAD Identity Authentication .NET Windows Store Xamarin iOS Android ", zipPackage.Tags);
            ValidateBool("Is Signed", true, zipPackage.VerificationResult?.IsSigned ?? false);
            ValidateBool("Is Signature Valid", true, zipPackage.VerificationResult?.IsValid ?? false);

            var fileDict = new Dictionary<string, PackageFile>();
            foreach (var p in pkgFiles)
            {
                fileDict[p.FilePath] = p;
            }

            ValidateInt("Number of Total Files", 25, fileDict.Values.ToList().Count);
            ValidateInt("Number of Dlls", 12, fileDict.Values.Where(x => x.IsDll).ToList().Count);
            ValidateInt("Number of Non-Dlls", 13, fileDict.Values.Where(x => !x.IsDll).ToList().Count);

            var platforms = new List<string>
            {
                "monoandroid81",
                "net45",
                "netcoreapp1.0",
                "netstandard1.3",
                "uap10.0",
                "xamarinios10",
            };

            foreach (var platform in platforms)
            {
                ValidatePackageFile(fileDict, $"{platform} - lib dll", $@"lib\{platform}\Microsoft.Identity.Client.dll", true);
                ValidatePackageFile(fileDict, $"{platform} - lib xml", $@"lib\{platform}\Microsoft.Identity.Client.xml", false);
                ValidatePackageFile(fileDict, $"{platform} - ref dll", $@"ref\{platform}\Microsoft.Identity.Client.dll", true);
                ValidatePackageFile(fileDict, $"{platform} - ref xml", $@"ref\{platform}\Microsoft.Identity.Client.xml", false);
            }

            // UAP has one additional file only in the lib directory.
            ValidatePackageFile(fileDict, "uap10.0 - lib pri", $@"lib\uap10.0\Microsoft.Identity.Client.pri", false);
        }

        private void ValidatePackageFile(Dictionary<string, PackageFile> fileDict, string fileDescription, string key, bool shouldBeDll)
        {
            ValidateBool($"{fileDescription} Exists", true, fileDict.ContainsKey(key));

            if (fileDict.TryGetValue(key, out PackageFile pkgFile))
            {
                if (shouldBeDll)
                {
                    // TODO: need to get assembly version stamp out of dll ValidateBool("NetDesktop Version", true, pkgFile.PkgFile);
                    ValidateGeneric($"{fileDescription} Signed", SignatureCheckResult.Valid, pkgFile.IsValidSignature);
                }
            }
        }

        private void ValidateInt(string label, int expected, int actual)
        {
            ValidateGeneric(label, expected, actual);
        }

        private void ValidateBool(string label, bool expected, bool actual)
        {
            ValidateGeneric(label, expected, actual);
        }

        private void ValidateString(string label, string expected, string actual)
        {
            ValidateGeneric(label, expected, actual, (exp, act) => string.Compare(exp, act, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private void ValidateGeneric<T>(string label, T expected, T actual) where T : IComparable
        {
            ValidateGeneric(label, expected, actual, (exp, act) => exp.Equals(act));
        }

        private void ValidateGeneric<T>(string label, T expected, T actual, Func<T, T, bool> compare)
        {
            Console.Write($"{label}: ");
            if (compare(expected, actual))
            {
                WriteTextInColor("OK", ConsoleColor.Green);
                Console.WriteLine();
            }
            else
            {
                WriteTextInColor("FAIL! ", ConsoleColor.Red);
                Console.WriteLine($"expected({expected}) actual({actual})");
            }
        }


        private static void WriteTextInColor(string message, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ForegroundColor = oldColor;
        }

        private void ConsoleDumpDetails(ZipPackage zipPackage, List<PackageFile> pkgFiles)
        {
            Console.WriteLine();
            Console.WriteLine("Verbose Output...");
            Console.WriteLine();

            Console.WriteLine($"Authors: {zipPackage.Authors}");
            Console.WriteLine($"Copyright: {zipPackage.Copyright}");
            Console.WriteLine($"Description: {zipPackage.Description}");
            Console.WriteLine($"DevelopmentDependency: {zipPackage.DevelopmentDependency}");
            Console.WriteLine($"FullName: {zipPackage.GetFullName()}");
            Console.WriteLine($"IconUrl: {zipPackage.IconUrl}");
            Console.WriteLine($"Id: {zipPackage.Id}");
            Console.WriteLine($"IsAbsoluteLatestVersion: {zipPackage.IsAbsoluteLatestVersion}");
            Console.WriteLine($"IsLatestVersion: {zipPackage.IsLatestVersion}");
            Console.WriteLine($"IsPrerelease: {zipPackage.IsPrerelease}");
            Console.WriteLine($"IsReleaseVersion: {zipPackage.IsReleaseVersion()}");
            Console.WriteLine($"Language: {zipPackage.Language}");
            Console.WriteLine($"LicenseMetadata: {zipPackage.LicenseMetadata}");
            Console.WriteLine($"LicenseUrl: {zipPackage.LicenseUrl}");
            Console.WriteLine($"MinClientVersion: {zipPackage.MinClientVersion}");
            Console.WriteLine($"PackageAssemblyReferences: {string.Join(",", zipPackage.PackageAssemblyReferences.ToList())}");
            Console.WriteLine($"PackageTypes: {string.Join(",", zipPackage.PackageTypes.ToList())}");
            Console.WriteLine($"ProjectUrl: {zipPackage.ProjectUrl}");
            Console.WriteLine($"Published: {zipPackage.Published}");
            Console.WriteLine($"PublisherSignature: {zipPackage.PublisherSignature}");
            Console.WriteLine($"ReleaseNotes: {zipPackage.ReleaseNotes}");
            Console.WriteLine($"ReportAbuseUrl: {zipPackage.ReportAbuseUrl}");
            Console.WriteLine($"Repository: {zipPackage.Repository}");
            Console.WriteLine($"RequireLicenseAcceptance: {zipPackage.RequireLicenseAcceptance}");
            Console.WriteLine($"Serviceable: {zipPackage.Serviceable}");
            Console.WriteLine($"Source: {zipPackage.Source}");
            Console.WriteLine($"Summary: {zipPackage.Summary}");
            Console.WriteLine($"Tags: {zipPackage.Tags}");
            Console.WriteLine($"Signature Verification Result (signed): {zipPackage.VerificationResult.IsSigned}");
            Console.WriteLine($"Signature Verification Result (valid): {zipPackage.VerificationResult.IsValid}");

            Console.WriteLine($"Title: {zipPackage.Title}");
            Console.WriteLine($"Version: {zipPackage.Version}");

            foreach (var pkgFile in pkgFiles)
            {
                Console.WriteLine(pkgFile.ToString());
            }
        }
    }
}
