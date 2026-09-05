using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ThirteenThirtyOne.IntegrationTests;

public sealed class HostTests
{
    [Theory]
    [InlineData("Development", "Information")]
    [InlineData("Production", "Warning")]
    public async Task HostBootsWithEnvironmentConfigurationAndLiveness(string environment, string expectedLogLevel)
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        Assert.Equal(expectedLogLevel, configuration["Logging:LogLevel:Microsoft.AspNetCore"]);
        Assert.True(configuration.GetValue<bool>("Logging:Console:FormatterOptions:UseUtcTimestamp"));
    }

    [Fact]
    public async Task RootDoesNotExposeABusinessEndpoint()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
