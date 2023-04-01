using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public sealed class MetricsOptions
{
    public IEnumerable<MetricSpec> Metrics { get; set; } = Enumerable.Empty<MetricSpec>();

    public static IEnumerable<MetricSpec> ReadMetricsFromFile( string filepath )
    {
        if ( !File.Exists( filepath ) )
        {
            return Enumerable.Empty<MetricSpec>();
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention( CamelCaseNamingConvention.Instance )
            .IgnoreUnmatchedProperties()
            .Build();

        var options = deserializer.Deserialize<MetricsOptions>( File.ReadAllText( filepath ) );

        return ( options.Metrics );
    }
}
