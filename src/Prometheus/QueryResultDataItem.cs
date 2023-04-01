public sealed class QueryResultDataItem
{
    public Dictionary<string, string> Metric { get; set; } = new Dictionary<string, string>();
    public IEnumerable<object> Value { get; set; } = Enumerable.Empty<object>();
}
