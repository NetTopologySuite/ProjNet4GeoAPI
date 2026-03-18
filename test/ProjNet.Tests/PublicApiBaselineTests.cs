// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNET.Tests
{
    using System;
    using System.IO;
    using ProjNet;
    using PublicApiGenerator;
    using Xunit;

    public class PublicApiBaselineTests
    {
        private const string BaselineFileName = "PublicAPI.Shipped.txt";
        private const string UpdateBaselineEnvironmentVariable = "PROJNET_UPDATE_PUBLIC_API_BASELINE";

        [Xunit.Fact]
        public void PublicApiMatchesBaseline()
        {
            string baselinePath = GetBaselinePath();
            string currentPublicApi = NormalizeLineEndings(typeof(CoordinateSystemServices).Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                IncludeAssemblyAttributes = false,
                ExcludeAttributes = new[]
                {
                    "System.Runtime.Versioning.TargetFrameworkAttribute",
                    "System.Reflection.AssemblyMetadataAttribute"
                },
            }));

            if (Environment.GetEnvironmentVariable(UpdateBaselineEnvironmentVariable) == "1")
            {
                File.WriteAllText(baselinePath, currentPublicApi + Environment.NewLine);
                return;
            }

            if (!File.Exists(baselinePath))
            {
                throw new InvalidOperationException($"Public API baseline file was not found at '{baselinePath}'. Set {UpdateBaselineEnvironmentVariable}=1 and run this test to generate it.");
            }

            string baseline = NormalizeLineEndings(File.ReadAllText(baselinePath));
            Assert.Equal(baseline, currentPublicApi);
        }

        private static string GetBaselinePath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string solutionPath = Path.Combine(directory.FullName, "ProjNet4GeoAPI.sln");
                if (File.Exists(solutionPath))
                {
                    return Path.Combine(directory.FullName, "src", "ProjNet", BaselineFileName);
                }

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
