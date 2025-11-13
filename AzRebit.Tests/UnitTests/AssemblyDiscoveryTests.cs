using System.Reflection;

using AwesomeAssertions;

using AzRebit.Shared.Model;
using AzRebit.Triggers.BlobTriggered.Model;

namespace AzRebit.Tests.UnitTests;

[TestClass]
public sealed class AssemblyDiscoveryTests
{
    private const int NumberOfExampleAzFunctions= 2;


    [TestMethod]
    public void DiscoverAzFunctions_DiscoverAzFunctionInTheProject_ShouldDiscoverAllAzFunctions()
    {
        // Test implementation goes here
        IEnumerable<AzFunction> allFunctions= AssemblyDiscovery.DiscoverAzFunctions();
        
        allFunctions.Should().HaveCountGreaterThan(0,because:"we have azure functions in the example project");
        allFunctions.Should().HaveCount(NumberOfExampleAzFunctions,because:"we have exactly {funcNumber} in the test project",NumberOfExampleAzFunctions);
        allFunctions.Should().Contain(f => f.Name.Equals("getcats", StringComparison.OrdinalIgnoreCase),because:"we  have that function name");

        //check addcat function trigger metadata
        var addCatBlobFunc =allFunctions.Select(f => f).FirstOrDefault(f => f.Name.Equals("AddCat",StringComparison.OrdinalIgnoreCase));
        addCatBlobFunc.TriggerMetadata.Should().BeAssignableTo<BlobTriggerAttributeMetadata>();
        addCatBlobFunc.TriggerMetadata.As<BlobTriggerAttributeMetadata>().ContainerName.Should().Be("cats-container", because:"that is the container defined in the example function");

        //check getcats function trigger metadata

    }

}
