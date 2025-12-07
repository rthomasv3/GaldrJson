using System.Net;
using System.Net.Http.Json;
using GaldrJson.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace GaldrJson.Tests;

// ============================================================================
// TEST MODELS
// ============================================================================

[GaldrJsonSerializable]
public class GoodResponse
{
    public string Name { get; set; } = "Works";
    public int Age { get; set; } = 42;
}

public class BadResponse  // ← no attribute on purpose
{
    public string Secret { get; set; } = "you forgot the attribute";
}

// ============================================================================
// TESTS
// ============================================================================

[TestClass]
public class MinimalApiTests
{
    private static WebApplication _app;
    private static HttpClient _client;

    [ClassInitialize]
    public static async Task Init(TestContext testContext)
    {
        _client = new HttpClient();

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddGaldrJson();
        _app = builder.Build();

        _app.MapGet("/good", () => new GoodResponse());
        _app.MapGet("/bad", () => new BadResponse());
        _app.MapGet("/string", () => "hello");
        _app.MapGet("/int", () => 123);

        _app.RunAsync();
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _app.DisposeAsync();
    }

    [TestMethod]
    public async Task Returns_GoodResponse_UsingGaldrJson()
    {
        await WaitForAppStart();

        GoodResponse response = await _client.GetFromJsonAsync<GoodResponse>($"{_app.Urls.FirstOrDefault()}/good");

        Assert.IsNotNull(response);
        Assert.AreEqual("Works", response.Name);
        Assert.AreEqual(42, response.Age);
    }

    [TestMethod]
    public async Task Throws_HelpfulException_When_Type_Is_Not_Attributed()
    {
        await WaitForAppStart();

        var response = await _client.GetAsync($"{_app.Urls.FirstOrDefault()}/bad");
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private async Task WaitForAppStart()
    {
        CancellationTokenSource source = new();
        source.CancelAfter(1000);

        // wait for start...
        while (_app.Urls == null || _app.Urls.Count <= 0)
        {
            await Task.Delay(100, source.Token);
        }
    }
}
