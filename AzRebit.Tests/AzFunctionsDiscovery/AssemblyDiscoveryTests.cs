using System.Data;
using System.Reflection;

using AwesomeAssertions;

using AzRebit;
using AzRebit.Model;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

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
    [DataRow("AzureWebJobsStorage", "AzureWebJobsStorage")]
    [DataRow("MyBlobConnection", "AzureWebJobsStorage__MyBlobConnection")]
    [DataRow("SpecialStorageAccount", "AzureWebJobsStorage:SpecialStorageAccount")]
    [DataRow("", "AzureWebJobsStorage")]
    public void GivenDifferentConnectionStringNames_WhenFunctionProjectStarts_ShouldSuccessfullyAssignProperConnectionNames(string connectionInAzureTriggerDefinition,string expectedAppSettingDefinedName)
    {
        Environment.SetEnvironmentVariable(expectedAppSettingDefinedName, "ConnectionString=DefaultStorageAccountSomething");
        var appsettingName=AssemblyDiscovery.ResolveConnectionStringAppSettingName(connectionInAzureTriggerDefinition);

        appsettingName.Should().Be(expectedAppSettingDefinedName);
    }

    [TestMethod]
    [DataRow(["GetCats", "CheckCats", "TransferCats", "TransformCats"])]
    public void GivenAzFunctionProject_WhenAzureFunctionsExists_ShouldDiscoverAll(string[] availableFunctions)
    {
        //arrange
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "ConnectionStringPlaceHolder");
        var serviceCollection = Substitute.For<IServiceCollection>();

        // act
        IEnumerable<AzFunction> allFunctions = AssemblyDiscovery.DiscoverAndAddAzFunctions(serviceCollection, new HashSet<string>());

        //assert
        allFunctions.Should().HaveCount(availableFunctions.Count());
    }

    [TestMethod]
    [DataRow(["getcats"])]
    public void GivenAzFunctionProject_WhenAzureFunctionsExists_ShouldExcludeDefinedFunctionNames(string[] excludedFunctions)
    {
        //arrange
        var serviceCollection = Substitute.For<IServiceCollection>();
        var excludedSet = new HashSet<string>(excludedFunctions, StringComparer.OrdinalIgnoreCase);
        
        //act
        IEnumerable<AzFunction> allFunctions = AssemblyDiscovery.DiscoverAndAddAzFunctions(serviceCollection,excludedSet);

        //assert
        allFunctions.Should().HaveCountGreaterThan(0);
        
        allFunctions.Should().NotContain(f => excludedFunctions.ToList()
        .Contains(f.Name),because: "that function is on the excluded list");

    }

}
