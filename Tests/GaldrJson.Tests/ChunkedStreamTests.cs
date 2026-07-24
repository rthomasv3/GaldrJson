using System.Text;
using System.Text.Json;
using GaldrJson.AspNetCore;
using GaldrJson.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GaldrJson.Tests;

// Reproduces the ASP.NET request-body path: JsonSerializer.DeserializeAsync over a
// stream delivered in small chunks, using the same options AddGaldrJson configures.
// Any body larger than one read buffer crosses refill boundaries mid-object; the
// generated converters must not treat "no more buffered data" as end-of-object.
[TestClass]
public class ChunkedStreamTests
{
    // Feeds at most chunkSize bytes per read so every refill boundary is exercised.
    private sealed class ChunkedStream : Stream
    {
        private readonly byte[] _data;
        private readonly int _chunkSize;
        private int _position;

        public ChunkedStream(byte[] data, int chunkSize)
        {
            _data = data;
            _chunkSize = chunkSize;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _data.Length;
        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int available = Math.Min(Math.Min(count, _chunkSize), _data.Length - _position);
            Array.Copy(_data, _position, buffer, offset, available);
            _position += available;
            return available;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static JsonSerializerOptions CreateAspNetLikeOptions()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddGaldrJson();
        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions;
    }

    private static ChunkedBatchModel CreateBatch(int itemCount, int payloadRepeats)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < payloadRepeats; i++)
        {
            builder.Append("payload text with \"escapes\" and \\ slashes plus é and \U0001F680 padding\n");
        }
        string payload = builder.ToString();

        List<ChunkedItemModel> items = new List<ChunkedItemModel>();
        for (int i = 0; i < itemCount; i++)
        {
            items.Add(new ChunkedItemModel
            {
                Id = $"item-{i}",
                Kind = "upsert",
                Payload = payload + $"tail {i}",
            });
        }

        return new ChunkedBatchModel { Items = items };
    }

    [TestMethod]
    [DataRow(1024)]
    [DataRow(4096)]
    [DataRow(16 * 1024)]
    public async Task DeserializeAsync_Over_Chunked_Stream_Preserves_Every_Item(int chunkSize)
    {
        ChunkedBatchModel batch = CreateBatch(itemCount: 120, payloadRepeats: 40);
        JsonSerializerOptions options = CreateAspNetLikeOptions();
        byte[] json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(batch, options));

        using ChunkedStream stream = new ChunkedStream(json, chunkSize);
        ChunkedBatchModel parsed = await JsonSerializer.DeserializeAsync<ChunkedBatchModel>(stream, options);

        Assert.IsNotNull(parsed);
        Assert.HasCount(batch.Items.Count, parsed.Items);
        for (int i = 0; i < batch.Items.Count; i++)
        {
            Assert.AreEqual(batch.Items[i].Id, parsed.Items[i].Id, $"Item {i} lost Id");
            Assert.AreEqual(batch.Items[i].Kind, parsed.Items[i].Kind, $"Item {i} lost Kind");
            Assert.AreEqual(batch.Items[i].Payload, parsed.Items[i].Payload, $"Item {i} lost Payload");
        }
    }
}
