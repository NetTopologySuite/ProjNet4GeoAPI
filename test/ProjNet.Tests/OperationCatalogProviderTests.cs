namespace ProjNET.Tests
{
    using System.Linq;
    using ProjNet.Data;
    using Xunit;

    public class OperationCatalogProviderTests
    {
        [Fact]
        public void ManagedOperationProvider_LoadsGeneratedOperationCatalog()
        {
            var provider = new ManagedCoordinateOperationDefinitionProvider();
            var definitions = provider.GetDefinitions().ToList();

            Assert.True(definitions.Count > 2500);
            Assert.Contains(definitions, operation => operation.SourceSrid > 0 && operation.TargetSrid > 0);
            Assert.Contains(definitions, operation => !string.IsNullOrWhiteSpace(operation.MethodName));
            Assert.Contains(definitions, operation => !string.IsNullOrWhiteSpace(operation.ParameterFileName));
        }
    }
}
