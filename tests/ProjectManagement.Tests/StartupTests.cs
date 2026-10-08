using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProjectManagement.Tests;

public class StartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public StartupTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task LivenessEndpointReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DomainApiRemainsUnimplemented()
    {
        var response = await _client.GetAsync("/api/v1/employees");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
