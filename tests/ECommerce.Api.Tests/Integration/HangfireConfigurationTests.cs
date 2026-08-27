using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using MassTransit;
using Xunit;

namespace ECommerce.Api.Tests.Integration;

public class HangfireConfigurationTests
{
    [Fact]
    public async Task HangfireDashboard_ShouldReturn401_ForUnauthenticatedRequest()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.AddHangfire(config => config.UseInMemoryStorage());
                services.AddMassTransitTestHarness();
            });
        });
        var client = factory.CreateClient();

        var response = await client.GetAsync("/hangfire");

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized,
            $"Expected 401 Unauthorized but received {(int)response.StatusCode} {response.StatusCode}. Body: {responseBody}");
    }
}
