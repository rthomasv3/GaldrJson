using System.Text;
using GaldrJson.AspNetCore;
using GaldrJson.Tests.Models;
using Microsoft.AspNetCore.Builder;

namespace GaldrJson.Tests;

// Reproduces multi-buffer request bodies through the real minimal-API pipeline:
// any POST larger than one transport read must bind without losing properties at
// buffer refill boundaries.
[TestClass]
public class LargeBodyApiTests
{
    private static readonly GaldrJsonOptions ClientOptions =
        new GaldrJsonOptions { PropertyNamingPolicy = PropertyNamingPolicy.CamelCase };

    private static WebApplication _app;
    private static HttpClient _client;

    [ClassInitialize]
    public static void Init(TestContext testContext)
    {
        _client = new HttpClient();

        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddGaldrJson(new GaldrJsonOptions { PropertyNamingPolicy = PropertyNamingPolicy.CamelCase });
        _app = builder.Build();

        _app.MapPost("/echo", (ChunkedBatchModel batch) => batch);
        // Summarizes server-side so a failure here indicts request binding alone,
        // independent of response serialization.
        _app.MapPost("/summary", (ChunkedBatchModel batch) =>
        {
            ChunkedSummaryModel summary = new ChunkedSummaryModel
            {
                Count = batch.Items?.Count ?? 0,
                FirstEmptyPayloadIndex = -1,
            };

            for (int i = 0; i < summary.Count; i++)
            {
                string itemPayload = batch.Items[i].Payload;
                summary.TotalPayloadChars += itemPayload?.Length ?? 0;
                if (string.IsNullOrEmpty(itemPayload))
                {
                    summary.EmptyPayloadCount++;
                    if (summary.FirstEmptyPayloadIndex < 0)
                    {
                        summary.FirstEmptyPayloadIndex = i;
                    }
                }
            }

            return summary;
        });
        _app.Urls.Add("http://127.0.0.1:5078");
        _ = _app.RunAsync();
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _app.DisposeAsync();
    }

    [TestMethod]
    public async Task Large_Post_Body_Binds_Without_Losing_Properties()
    {
        List<ChunkedItemModel> items = CreateItems();

        HttpResponseMessage response = await Post("/echo", items);
        ChunkedBatchModel echoed = GaldrJson.Deserialize<ChunkedBatchModel>(await response.Content.ReadAsStringAsync(), ClientOptions);

        Assert.IsNotNull(echoed);
        Assert.HasCount(items.Count, echoed.Items);
        for (int i = 0; i < items.Count; i++)
        {
            Assert.AreEqual(items[i].Id, echoed.Items[i].Id, $"Item {i} lost Id");
            Assert.AreEqual(items[i].Kind, echoed.Items[i].Kind, $"Item {i} lost Kind");
            Assert.AreEqual(items[i].Payload, echoed.Items[i].Payload, $"Item {i} lost Payload");
        }
    }

    [TestMethod]
    public async Task Large_Post_Body_Binds_All_Payloads_Server_Side()
    {
        List<ChunkedItemModel> items = CreateItems();
        long expectedChars = items.Sum(item => (long)item.Payload.Length);

        HttpResponseMessage response = await Post("/summary", items);
        ChunkedSummaryModel summary = GaldrJson.Deserialize<ChunkedSummaryModel>(await response.Content.ReadAsStringAsync(), ClientOptions);

        Assert.IsNotNull(summary);
        Assert.AreEqual(items.Count, summary.Count);
        Assert.AreEqual(0, summary.EmptyPayloadCount, $"first empty payload at index {summary.FirstEmptyPayloadIndex}");
        Assert.AreEqual(expectedChars, summary.TotalPayloadChars);
    }

    private static List<ChunkedItemModel> CreateItems()
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < 40; i++)
        {
            builder.Append("payload text with \"escapes\" and \\ slashes plus é and \U0001F680 padding\n");
        }
        string payload = builder.ToString();

        List<ChunkedItemModel> items = new List<ChunkedItemModel>();
        for (int i = 0; i < 120; i++)
        {
            items.Add(new ChunkedItemModel
            {
                Id = $"item-{i}",
                Kind = "upsert",
                Payload = payload + $"tail {i}",
            });
        }

        return items;
    }

    private static async Task<HttpResponseMessage> Post(string path, List<ChunkedItemModel> items)
    {
        string json = GaldrJson.Serialize(new ChunkedBatchModel { Items = items }, ClientOptions);
        StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = null;
        for (int attempt = 0; attempt < 20 && response == null; attempt++)
        {
            try
            {
                response = await _client.PostAsync($"http://127.0.0.1:5078{path}", content);
            }
            catch (HttpRequestException)
            {
                await Task.Delay(100);
            }
        }

        Assert.IsNotNull(response);
        response.EnsureSuccessStatusCode();
        return response;
    }
}
