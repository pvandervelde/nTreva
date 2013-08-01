//-----------------------------------------------------------------------
// <copyright company="NTreva">
//     Copyright 2013 NTreva. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Mono.Options;
using NTreva.Properties;
using Nuclei;
using NuGet;

namespace NTreva
{
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1400:AccessModifierMustBeDeclared",
            Justification = "Access modifiers should not be declared on the entry point for a command line application. See FxCop.")]
    class Program
    {
        /// <summary>
        /// Defines the exit code for a normal application exit (i.e. without errors).
        /// </summary>
        private const int NormalApplicationExitCode = 0;

        /// <summary>
        /// Defines the exit code for an application exit due to an unhandled exception.
        /// </summary>
        private const int UnhandledExceptionApplicationExitCode = 1;

        /// <summary>
        /// Defines the exit code for an application exit due to input errors.
        /// </summary>
        private const int InputErrorsOccuredApplicationExitCode = 2;

        [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1400:AccessModifierMustBeDeclared",
            Justification = "Access modifiers should not be declared on the entry point for a command line application. See FxCop.")]
        static int Main(string[] args)
        {
            ShowHeader();

            bool showHelp = false;
            string packageDir = null;
            string outputFile = null;
            var configDirs = new List<string>();

            var options = new OptionSet 
                {
                    {
                        Resources.CommandLine_Param_PackageDir_Key,
                        Resources.CommandLine_Param_PackageDir_Description,
                        v => packageDir = Path.GetFullPath(v)
                    },
                    { 
                        Resources.CommandLine_Param_Output_Key, 
                        Resources.CommandLine_Param_Output_Description, 
                        v => outputFile = Path.GetFullPath(v)
                    },
                    {
                        Resources.CommandLine_Param_ConfigDir_Key,
                        Resources.CommandLine_Param_ConfigDir_Description,
                        v => configDirs.Add(Path.GetFullPath(v))
                    },
                    {
                        Resources.CommandLine_Param_Help_Key,
                        Resources.CommandLine_Param_Help_Description,
                        v => showHelp = v != null 
                    }
                };

            try
            {
                options.Parse(args);
            }
            catch (OptionException)
            {
                WriteErrorToConsole(Resources.Output_Error_InvalidInput);
                return UnhandledExceptionApplicationExitCode;
            }

            if (showHelp)
            {
                ShowHelp(options);
                return NormalApplicationExitCode;
            }

            if (string.IsNullOrWhiteSpace(outputFile) ||
                string.IsNullOrWhiteSpace(packageDir) ||
                (configDirs.Count == 0))
            {
                WriteErrorToConsole(Resources.Output_Error_MissingValues);
                ShowHelp(options);
                return InputErrorsOccuredApplicationExitCode;
            }

            WriteLicenseInformationToOutputFile(configDirs, packageDir, outputFile);

            return NormalApplicationExitCode;
        }

        private static void ShowHeader()
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Header_ApplicationAndVersion,
                    GetVersion()));
            Console.WriteLine(GetCopyright());
            Console.WriteLine(GetLibraryLicenses());
        }

        private static void ShowHelp(OptionSet argProcessor)
        {
            Console.WriteLine(Resources.Help_Usage_Intro);
            Console.WriteLine();
            argProcessor.WriteOptionDescriptions(Console.Out);
        }

        private static void WriteErrorToConsole(string errorText)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errorText);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static void WriteToConsole(string text)
        {
            Console.WriteLine(text);
        }

        private static string GetVersion()
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
            Debug.Assert(attribute.Length == 1, "There should be a copyright attribute.");

            return (attribute[0] as AssemblyFileVersionAttribute).Version;
        }

        private static string GetCopyright()
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            Debug.Assert(attribute.Length == 1, "There should be a copyright attribute.");

            return (attribute[0] as AssemblyCopyrightAttribute).Copyright;
        }

        private static string GetLibraryLicenses()
        {
            var licenseXml = EmbeddedResourceExtracter.LoadEmbeddedStream(
                Assembly.GetExecutingAssembly(),
                @"NTreva.Properties.licenses.xml");
            var doc = XDocument.Load(licenseXml);
            var licenses = from element in doc.Descendants("package")
                           select new
                               {
                                   Id = element.Element("id").Value,
                                   Version = element.Element("version").Value,
                                   Source = element.Element("url").Value,
                                   License = element.Element("licenseurl").Value,
                               };

            var builder = new StringBuilder();
            builder.AppendLine(Resources.Header_OtherPackages_Intro);
            foreach (var license in licenses)
            {
                builder.AppendLine(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Header_OtherPackages_IdAndLicense,
                        license.Id,
                        license.Version,
                        license.Source));
            }

            return builder.ToString();
        }

        private static void WriteLicenseInformationToOutputFile(List<string> configDirs, string packageDir, string outputFile)
        {
            var packageBuilder = new StringBuilder();
            var licenseTemplate = EmbeddedResourceExtracter.LoadEmbeddedTextFile(
                Assembly.GetExecutingAssembly(),
                @"NTreva.Templates.LicenseInformation.xml");

            // Determine all the packages we need to scan from the packages.config files
            var usedPackages = new List<Tuple<string, string>>();
            foreach (var dir in configDirs)
            {
                var configFile = Path.Combine(dir, "packages.config");

                var file = new PackageReferenceFile(configFile);
                var packageReferences = file.GetPackageReferences().ToList();

                // Note that we ignore dependencies here because packages.config already contains the full closure
                foreach (var package in packageReferences)
                {
                    // GetPackageReferences returns all records without validating values. We'll throw if we encounter packages
                    // with malformed ids.
                    if (string.IsNullOrEmpty(package.Id))
                    {
                        throw new InvalidDataException(Resources.Exceptions_Messages_InvalidPackageId);
                    }

                    if (!usedPackages.Exists(t => string.Equals(t.Item1, package.Id)))
                    {
                        usedPackages.Add(new Tuple<string, string>(package.Id, package.Version.ToString()));
                    }
                }
            }

            usedPackages.Sort();
            foreach (var tuple in usedPackages)
            {
                WriteToConsole(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Output_ProcessingPackage,
                        tuple.Item1));

                // Go find the package from the packages directory
                var packageFilePath = Path.Combine(
                    Path.Combine(
                        packageDir, 
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}.{1}",
                            tuple.Item1,
                            tuple.Item2)), 
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}.{1}.{2}",
                        tuple.Item1,
                        tuple.Item2,
                        "nupkg"));
                if (File.Exists(packageFilePath))
                {
                    var packageFile = new ZipPackage(packageFilePath);

                    var licenseBuilder = new StringBuilder(licenseTemplate);
                    licenseBuilder.Replace(@"${Package.Id}$", packageFile.Id);

                    var version = packageFile.Version ?? new SemanticVersion(new Version());
                    licenseBuilder.Replace(@"${Package.Version}$", version.ToString());

                    var projectUrl = packageFile.ProjectUrl
                            ?? new Uri(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    Resources.GoogleSearchQuery,
                                    tuple.Item1.Replace(' ', '+')));
                    licenseBuilder.Replace(@"${Package.Url}$", projectUrl.ToString());

                    var licenseUrl = packageFile.LicenseUrl
                        ?? new Uri(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Resources.GoogleSearchQuery,
                                tuple.Item1.Replace(' ', '+')));
                    licenseBuilder.Replace(@"${Package.LicenseUrl}$", licenseUrl.ToString());

                    if (packageBuilder.Length > 0)
                    {
                        packageBuilder.Append(Environment.NewLine);
                    }

                    packageBuilder.Append(licenseBuilder);
                }
            }

            var packagesTemplate = EmbeddedResourceExtracter.LoadEmbeddedTextFile(
                Assembly.GetExecutingAssembly(),
                @"NTreva.Templates.Packages.xml");

            var builder = new StringBuilder(packagesTemplate);
            builder.Replace(@"${Packages}$", packageBuilder.ToString());

            using (var writer = new StreamWriter(new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None), Encoding.UTF8))
            {
                writer.Write(builder.ToString());
            }

            WriteToConsole(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Output_Completed,
                    outputFile));
        }
    }
}
