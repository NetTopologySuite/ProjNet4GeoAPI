using System;
using System.IO;
using ProjNet;
using PublicApiGenerator;
using Xunit;

namespace ProjNET.Tests
{
    public class PublicApiBaselineTests
    {
        private const string BaselineFileName = "PublicAPI.Shipped.txt";
        private const string UpdateBaselineEnvironmentVariable = "PROJNET_UPDATE_PUBLIC_API_BASELINE";

        [Xunit.Fact]
        public void PublicApiMatchesBaseline()
        {
            var baselinePath = GetBaselinePath();
            var currentPublicApi = NormalizeLineEndings(typeof(CoordinateSystemServices).Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                IncludeAssemblyAttributes = false,
                ExcludeAttributes = new[]
                {
                    "System.Runtime.Versioning.TargetFrameworkAttribute",
                    "System.Reflection.AssemblyMetadataAttribute"
                }
            }));

            if (Environment.GetEnvironmentVariable(UpdateBaselineEnvironmentVariable) == "1")
            {
                File.WriteAllText(baselinePath, currentPublicApi + Environment.NewLine);
                return;
            }

            if (!File.Exists(baselinePath))
                throw new InvalidOperationException($"Public API baseline file was not found at '{baselinePath}'. Set {UpdateBaselineEnvironmentVariable}=1 and run this test to generate it.");

            var baseline = NormalizeLineEndings(File.ReadAllText(baselinePath));
            Assert.Equal(baseline, currentPublicApi);
        }

        private static string GetBaselinePath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var solutionPath = Path.Combine(directory.FullName, "ProjNet4GeoAPI.sln");
                if (File.Exists(solutionPath))
                    return Path.Combine(directory.FullName, "src", "ProjNet", BaselineFileName);

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Unable to locate repository root from test output directory.");
        }

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd();
        }
    }
}

