using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace PackageValidationTest
{
    public class CommandLineOptions
    {
        [Option("filePath", Required = true, HelpText = "Path to nuget package file.")]
        public string FilePath { get; set; }

        [Option("version", Required = true, HelpText = "Expected version of nuget package.")]
        public string ExpectedVersion { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Verbose output")]
        public bool IsVerbose { get; set; }
    }
}
