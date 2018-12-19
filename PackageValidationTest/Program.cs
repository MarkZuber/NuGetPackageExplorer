using System;
using System.Collections.Generic;
using System.Diagnostics;
using CommandLine;

namespace PackageValidationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser
                .Default
                .ParseArguments<CommandLineOptions>(args)
                .WithParsed(options => new PackageValidator(options).VerifyPackageAsync().ConfigureAwait(false).GetAwaiter().GetResult())
                .WithNotParsed(HandleParseError);

            if (Debugger.IsAttached)
            {
                Console.ReadLine();
            }
        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                Console.Error.WriteLine(error.ToString());
            }
        }
    }
}
