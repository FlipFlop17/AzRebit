using System.Reflection;

using AwesomeAssertions;

using AzRebit.Model;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace AzRebit.Tests.UnitTests.AzFunctionsDiscovery;

public sealed class AssemblyDiscoveryTests
{
    public AssemblyDiscoveryTests()
    {
        _ = Assembly.Load("AzRebit.FunctionExample");
    }

    [Theory]
    [InlineData("AzureWebJobsStorage", "AzureWebJobsStorage")]
    [InlineData("MyBlobConnection", "AzureWebJobsStorage__MyBlobConnection")]
    [InlineData("SpecialStorageAccount", "AzureWebJobsStorage:SpecialStorageAccount")]
    [InlineData("", "AzureWebJobsStorage")]
    public void GivenDifferentConnectionStringNames_WhenFunctionProjectStarts_ShouldSuccessfullyAssignProperConnectionNames(string connectionInAzureTriggerDefinition,string expectedAppSettingDefinedName)
    {
        Environment.SetEnvironmentVariable(expectedAppSettingDefinedName, "ConnectionString=DefaultStorageAccountSomething");
        var appsettingName=AssemblyDiscovery.ResolveConnectionStringAppSettingName(connectionInAzureTriggerDefinition);

        appsettingName.Should().Be(expectedAppSettingDefinedName);
    }

    [Theory]
    [InlineData("GetCats", "CheckCats", "TransferCats", "TransformCats")]
    public void GivenAzFunctionProject_WhenAzureFunctionsExists_ShouldDiscoverAll(params string[] availableFunctions)
    {
        //arrange
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "ConnectionStringPlaceHolder");
        var serviceCollection = Substitute.For<IServiceCollection>();

        // act
        IEnumerable<AzFunction> allFunctions = AssemblyDiscovery.DiscoverAndAddAzFunctions(serviceCollection, new HashSet<string>());

        //assert
        allFunctions.Should().HaveCount(availableFunctions.Count());
    }

    [Theory]
    [InlineData("getcats")]
    public void GivenAzFunctionProject_WhenAzureFunctionsExists_ShouldExcludeDefinedFunctionNames(params string[] excludedFunctions)
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
