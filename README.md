# nTreva

nTreva is an application that creates an XML file containing license information from installed NuGet packages. To run nTreva use the following command line.

    nTreva.exe -p <Directory_With_Installed_Packages> -o <Output_File> -c <PackageConfigFile1> -c <PackageConfigFile2> ... -c <PackageConfigFileN>

The output will be an file containing information about the installed packages (found in the different package.config files), their project URL and their license URL. If no project or license URL is found a default google search URL is generated. An example section as taken from the output file obtained by running nTreva on it's own workspace:

``` xml
<?xml version="1.0" encoding="utf-8" ?>
<packages>
  <package>
    <id>Lokad.Shared</id>
    <version>1.5.181.0</version>
    <url>http://www.google.com/search?q=Lokad.Shared</url>
    <licenseurl>http://www.opensource.org/licenses/bsd-license.php</licenseurl>
  </package>

  <package>
    <id>Nuclei</id>
    <version>0.6.0.0</version>
    <url>https://github.com/pvandervelde/Nuclei</url>
    <licenseurl>http://www.apache.org/licenses/LICENSE-2.0</licenseurl>
  </package>

  <package>
    <id>Nuclei.Build</id>
    <version>0.6.0.0</version>
    <url>https://github.com/pvandervelde/Nuclei</url>
    <licenseurl>http://www.apache.org/licenses/LICENSE-2.0</licenseurl>
  </package>

  <package>
    <id>Nuget.Core</id>
    <version>2.1.1</version>
    <url>http://nuget.codeplex.com/</url>
    <licenseurl>http://nuget.codeplex.com/license</licenseurl>
  </package>

</packages>
```

# Installation instructions
The application is available from NuGet or as ZIP archive from the [releases page](https://github.com/pvandervelde/nTreva/releases).

# How to build
The solution files are created in Visual Studio 2012 (using .NET 4.5) and the entire project can be build by invoking MsBuild on the ntreva.integration.msbuild script. This will build the binaries, the NuGet package and the ZIP archive. The binaries will be placed in the `build\bin\AnyCpu\Release` directory and the NuGet package and the ZIP archive will be placed in the `build\deploy` directory.

Note that the build scripts assume that:

* The binaries should be signed, however the SNK key file is not included in the repository so a new key file has to be [created][snkfile_msdn]. The key file is referenced through an environment variable called `SOFTWARE_SIGNING_KEY_PATH` that has as value the full path of the key file. 
* GIT can be found on the PATH somewhere so that it can be called to get the hash of the last commit in the current repository. This hash is embedded in the nTreva executable together with information about the build configuration and build time and date.

[snkfile_msdn]: http://msdn.microsoft.com/en-us/library/6f05ezxy(v=vs.110).aspx


 