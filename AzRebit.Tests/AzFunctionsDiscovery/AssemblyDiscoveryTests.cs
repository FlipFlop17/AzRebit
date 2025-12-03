using System.Data;
using System.Reflection;

using AwesomeAssertions;
using AzRebit;
using AzRebit.FunctionExample.Features;
using AzRebit.Shared.Model;
using AzRebit.Triggers.BlobTriggered.Model;

namespace AzFunctionsDiscovery;

[TestClass]
public sealed class AssemblyDiscoveryTests
{
    public TestContext? TestContext { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        // Explicitly load the Azure Functions example assembly so it's available to AppDomain.CurrentDomain.GetAssemblies()
        _ = Assembly.Load("AzRebit.FunctionExample");
    }

    [TestMethod]
    [DataRow(["GetCats","CheckCats","TransferCats","TransformCats"])]
    public void GivenAzFunctionProject_WhenAzureFunctionsExists_ShouldDiscoverAll(string[] availableFunctions)
    {
        //arrange
        // act
        IEnumerable<AzFunction> allFunctions= AssemblyDiscovery.DiscoverAzFunctions();
        
        //assert
        allFunctions.Should().HaveCount(availableFunctions.Count());
    }

    [TestMethod]
    [DataRow(["getcats"])]
    public void GivenAzFunctionProject_WhenAzureFunctionsExists_ShouldExcludeDefinedFunctionNames(string[] excludedFunctions)
    {
        //arrange
        var excludedSet = new HashSet<string>(excludedFunctions, StringComparer.OrdinalIgnoreCase);
        
        //act
        IEnumerable<AzFunction> allFunctions = AssemblyDiscovery.DiscoverAzFunctions(excludedSet);

        //assert
        allFunctions.Should().HaveCountGreaterThan(0);
        
        allFunctions.Should().NotContain(f => excludedFunctions.ToList()
        .Contains(f.Name),because: "that function is on the excluded list");

    }

}
