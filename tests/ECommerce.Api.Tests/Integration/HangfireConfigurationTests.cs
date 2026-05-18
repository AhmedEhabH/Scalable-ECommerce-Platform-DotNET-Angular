using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ECommerce.Api.Tests.Integration;

public class HangfireConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HangfireConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HangfireDashboard_ShouldReturn401_ForUnauthenticatedRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/hangfire");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
