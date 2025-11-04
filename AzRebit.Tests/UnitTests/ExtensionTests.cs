using System.Reflection;

using AwesomeAssertions;

using AzRebit.BlobTriggered.Model;

using static AzRebit.Shared.Model.TriggerTypes;


namespace AzRebit.Tests.UnitTests;

[TestClass]
public sealed class ExtensionTests
{

    public TestContext TestContext { get; set; }

    [TestMethod]
    public void Should_DiscoverAllFunctions_WhenExampleAssemblyIsLoaded()
    {
        var exampleAssembly = Assembly.Load("AzFunctionResubmit.Example");

        // Act
        var functions = ResubmitExtension.CollectFunctionDetails();

        functions.Should().HaveCountGreaterThan(0);

        var functionsFromExample = functions.Where(f =>
            exampleAssembly.GetTypes().Any(t =>
                t.GetMethods().Any(m =>
                    m.GetCustomAttribute<Microsoft.Azure.Functions.Worker.FunctionAttribute>()?.Name == f.Name
                )
            )
        );

        functionsFromExample.Should().HaveSameCount(functions);
        // Assert

        TestContext.WriteLine($"Discovered {functions.Count()} functions:");
        foreach (var func in functions)
        {
            TestContext.WriteLine($"- {func.Name}");
        }
    }
    [TestMethod]
    public void Should_CollectTriggerDetails_ForAddCatFunction()
    {
        var exampleAssembly = Assembly.Load("AzFunctionResubmit.Example");

        // Act
        var functions = ResubmitExtension.CollectFunctionDetails();

        functions.Should().HaveCountGreaterThan(0);

        //--check if the function AddCat(blobtriggered) has blob trigger detailes fetched correctly
        var blobTriggeredFunction= functions.FirstOrDefault(f => f.Name == "AddCat");

        var blobTrigger= (BlobTriggerDetails)blobTriggeredFunction.TriggerDetails;
        blobTrigger.TypeOfTriger.Should().Be(TriggerType.Blob);
        blobTrigger.ContainerName.Should().Be("my-container");

    }
}
