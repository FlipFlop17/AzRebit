using Xunit.Abstractions;

namespace AzRebitTests.IntegrationTests.ManualSaveTest;

[Collection("FunctionApp")]
public class ManualSaveTests
{
    private readonly ITestOutputHelper _output;
    private readonly FunctionAppFixture _host;

    public ManualSaveTests(ITestOutputHelper output, FunctionAppFixture host)
    {
        _output = output;
        _host = host;
    }

    [Fact]
    public async Task When_ManualSave_is_invoked_Should_save_the_payload_at_resubmit_location()
    {
        //arrange
        var functionName = "CheckCats";

        //act
        //assert

    }

}
