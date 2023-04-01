public class V1Beta1MetricValue
{
    public DescribedObjectSpec DescribedObject { get; set; } = new DescribedObjectSpec();
    public string? MetricName { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public string? Value { get; set; }

    public class DescribedObjectSpec
    {
        public string? Kind { get; set; }
        public string? ApiVersion { get; set; }
        public string? Namespace { get; set; }
        public string? Name { get; set; }
    }
}
