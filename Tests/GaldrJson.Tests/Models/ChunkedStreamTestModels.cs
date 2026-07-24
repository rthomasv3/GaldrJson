namespace GaldrJson.Tests.Models;

[GaldrJsonSerializable]
internal class ChunkedBatchModel
{
    public List<ChunkedItemModel> Items { get; set; }
}

[GaldrJsonSerializable]
internal class ChunkedItemModel
{
    public string Id { get; set; }
    public string Kind { get; set; }
    public string Payload { get; set; }
}

[GaldrJsonSerializable]
internal class ChunkedSummaryModel
{
    public int Count { get; set; }
    public int EmptyPayloadCount { get; set; }
    public int FirstEmptyPayloadIndex { get; set; }
    public long TotalPayloadChars { get; set; }
}
