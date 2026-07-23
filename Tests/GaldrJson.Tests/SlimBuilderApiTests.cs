using System.Net.Http.Json;
using GaldrJson.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace GaldrJson.Tests;

[TestClass]
public class SlimBuilderApiTests
{
    private static WebApplication _app;
    private static HttpClient _client;

    [ClassInitialize]
    public static void Init(TestContext testContext)
    {
        _client = new HttpClient();

        // CreateSlimBuilder ships an empty TypeInfoResolver chain (same state Native AOT
        // leaves CreateBuilder in) - these tests pass only because AddGaldrJson supplies
        // metadata for GaldrJson-serializable types itself.
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddGaldrJson();
        _app = builder.Build();

        _app.MapGet("/good", () => new GoodResponse());
        _app.Urls.Add("http://127.0.0.1:5077");
        _app.RunAsync();
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _app.DisposeAsync();
    }

    [TestMethod]
    public async Task Returns_Dto_On_SlimBuilder_UsingGaldrJson()
    {
        GoodResponse response = null;

        for (int attempt = 0; attempt < 20 && response == null; attempt++)
        {
            try
            {
                response = await _client.GetFromJsonAsync<GoodResponse>("http://127.0.0.1:5077/good");
            }
            catch (HttpRequestException)
            {
                await Task.Delay(100);
            }
        }

        Assert.IsNotNull(response);
        Assert.AreEqual("Works", response.Name);
        Assert.AreEqual(42, response.Age);
    }
}
