using System.Reflection;

using AwesomeAssertions;
using AzRebit;
using AzRebit.Shared.Model;
using AzRebit.Triggers.BlobTriggered.Model;

namespace AzFunctionsDiscovery;

[TestClass]
public sealed class AssemblyDiscoveryTests
{
    private const int NumberOfExampleAzFunctions= 3;
    public TestContext? TestContext { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        // Explicitly load the Azure Functions example assembly so it's available to AppDomain.CurrentDomain.GetAssemblies()
        _ = Assembly.Load("AzRebit.FunctionExample");
    }

    [TestMethod]
    public void DiscoverAzFunctions_DiscoverAzFunctionInTheProject_ShouldDiscoverAllAzFunctions()
    {
        //arrange
        // act
        IEnumerable<AzFunction> allFunctions= AssemblyDiscovery.DiscoverAzFunctions();
        
        //assert
        allFunctions.Should().HaveCountGreaterThan(0,because:"we have azure functions in the example project");
        allFunctions.Should().HaveCount(NumberOfExampleAzFunctions,because:"we should have exactly {0} in the test project",NumberOfExampleAzFunctions);
        allFunctions.Should().Contain(f => f.Name.Equals("getcats", StringComparison.OrdinalIgnoreCase),because:"we  have that function name");

        //check if blob trigger is found
        var addCatBlobFunc =allFunctions.Select(f => f).FirstOrDefault(f => f.Name.Equals("TransferCats",StringComparison.OrdinalIgnoreCase));
        addCatBlobFunc.TriggerMetadata.Should().BeAssignableTo<BlobTriggerAttributeMetadata>();
        addCatBlobFunc.TriggerMetadata.As<BlobTriggerAttributeMetadata>().ContainerName.Should().Be("cats-container", because:"that is the container defined in the example function");

    }

    [TestMethod]
    [DataRow(new string[] {"getcats" })]
    public void DiscoverAzFunctions_DiscoverAzFunctionInTheProject_ShouldIgnoreAzFunctionsThatAreOnTheExcludedList(string[] excludedFunctions)
    {
        //arrange
        var excludedSet = new HashSet<string>(excludedFunctions, StringComparer.OrdinalIgnoreCase);
        
        //act
        IEnumerable<AzFunction> allFunctions = AssemblyDiscovery.DiscoverAzFunctions(excludedSet);

        //assert
        allFunctions.Should().HaveCountGreaterThan(0);
        allFunctions.Should().NotContain(f => f.Name.Equals("getcats", StringComparison.OrdinalIgnoreCase), because: "that function is on the excluded list");


    }

}
