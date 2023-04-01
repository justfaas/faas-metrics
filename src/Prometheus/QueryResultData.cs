public sealed class QueryResultData
{
    public string? ResultType { get; set; }
    public IEnumerable<QueryResultDataItem> Result { get; set; } = Enumerable.Empty<QueryResultDataItem>();
}
