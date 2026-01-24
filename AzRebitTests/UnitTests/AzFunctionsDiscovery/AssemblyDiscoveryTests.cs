using System.Reflection;

using AwesomeAssertions;

using AzRebit;
using AzRebit.Domain.Entities;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace AzRebitTests.UnitTests.AzFunctionsDiscovery;

public class AssemblyDiscoveryTests
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
    public void Discovers_connection_string_from_appsettings(string connectionInAzureTriggerDefinition, string expectedAppSettingDefinedName)
    {
        Environment.SetEnvironmentVariable(expectedAppSettingDefinedName, "ConnectionString=DefaultStorageAccountSomething");
        var appsettingName = AssemblyDiscovery.ResolveConnectionStringAppSettingName(connectionInAzureTriggerDefinition);

        appsettingName.Should().Be(expectedAppSettingDefinedName);
    }

    [Fact]
    public void Discovers_all_functions_in_assembly()
    {
        //arrange
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "ConnectionStringPlaceHolder");
        var serviceCollection = Substitute.For<IServiceCollection>();

        // act
        IEnumerable<AzFunction> allFunctions = AssemblyDiscovery.DiscoverAndAddAzFunctions(serviceCollection, new HashSet<string>());

        //assert
        allFunctions.Should().NotBeEmpty();

        // Additional check: all functions should have valid names
        allFunctions.Should().AllSatisfy(name =>
            name.Should().NotBeNull());
    }

    [Theory]
    [InlineData("getcats")]
    public void Excludes_specified_functions_from_assembly_discovery(params string[] excludedFunctions)
    {
        //arrange
        var serviceCollection = Substitute.For<IServiceCollection>();
        var excludedSet = new HashSet<string>(excludedFunctions, StringComparer.OrdinalIgnoreCase);

        //act
        IEnumerable<AzFunction> allFunctions = AssemblyDiscovery.DiscoverAndAddAzFunctions(serviceCollection, excludedSet);

        //assert
        allFunctions.Should().HaveCountGreaterThan(0);

        allFunctions.Should().NotContain(f => excludedFunctions.Contains(f.Name), because: "that function is on the excluded list");

    }

}
