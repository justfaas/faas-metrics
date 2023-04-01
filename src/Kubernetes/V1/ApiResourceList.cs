public sealed class V1ApiResourceList
{
    public string Kind => "APIResourceList";
    public string ApiVersion => "v1";
    public required string GroupVersion { get; set; }
    public IEnumerable<V1ApiResource> Resources { get; set; } = Enumerable.Empty<V1ApiResource>();
}

public sealed class V1ApiResource
{
    public required string Kind { get; set; }
    public required string Name { get; set; }
    public string SingularName { get; set; } = string.Empty;
    public bool Namespaced { get; set; }
    public IEnumerable<string>? Verbs { get; set; }
}
