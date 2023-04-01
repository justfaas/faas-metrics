public sealed class V1Beta1MetricValueList
{
    public string Kind => "MetricValueList";
    public string ApiVersion => "custom.metrics.k8s.io/v1beta1";
    public Dictionary<string, string>? Metadata { get; set; }
    public IEnumerable<V1Beta1MetricValue>? Items { get; set; }

    public static readonly V1Beta1MetricValueList Empty = new V1Beta1MetricValueList
    {
        Metadata = new Dictionary<string, string>(),
        Items = Enumerable.Empty<V1Beta1MetricValue>()
    };
}
