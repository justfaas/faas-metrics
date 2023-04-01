using System.Linq;
using Microsoft.Extensions.Options;

internal sealed class Metrics
{
    private readonly Dictionary<string, MetricSpec> metrics;

    public Metrics( IOptions<MetricsOptions> optionsAccessor )
    {
        var options = optionsAccessor.Value;

        metrics = options.Metrics.ToDictionary( x => x.Name, x => x );
    }

    public IEnumerable<string> GetNames() => metrics.Keys;

    public string GetQuery( string metricName, IDictionary<string, string>? labels )
    {
        var query = new Query
        {
            Format = metricName
        };

        if ( metrics.TryGetValue( metricName, out var spec ) )
        {
            // map metric query
            query = Query.Parse( spec.Query ?? spec.Name );
        }

        if ( labels?.Any() == true )
        {
            foreach ( var label in labels )
            {
                query.Labels[label.Key] = label.Value;
            }
        }

        var missingLabels = query.Labels.Where( label => label.Value == null )
            .ToArray();

        if ( missingLabels.Any() )
        {
            var missingKeys = missingLabels.Select( label => label.Key );

            throw new ArgumentException( "Missing labels: " + string.Join( ',', missingKeys ) );
        }

        return query.ToString();
    }

    private class Query
    {
        public required string Format { get; init; }
        public Dictionary<string, string?> Labels { get; init; } = new Dictionary<string, string?>();

        public override string ToString()
        {
            if ( !Labels.Any() )
            {
                return ( Format );
            }

            var labels = string.Join( ',', Labels.Select( x => $"{x.Key}=\"{x.Value}\"" ) );

            return Format.Replace( "--labels--", labels );
        }

        public static Query Parse( string query )
        {
            var labelIdx = query.IndexOf( '{' );

            if ( labelIdx < 0 )
            {
                // query with no labels
                return new Query
                {
                    Format = query
                };
            }

            var closureIdx = query.IndexOf( '}' );

            if ( closureIdx < labelIdx )
            {
                throw new ArgumentException( "Invalid label format!" );
            }

            var format = string.Concat( 
                query.Substring( 0, labelIdx ),
                "{--labels--}",
                query.Substring( closureIdx + 1 )
            );

            var labels = query.Substring( labelIdx + 1, closureIdx - labelIdx - 1 )
                .Split( ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries )
                .ToDictionary( key => key, _ => (string?)null );

            return new Query
            {
                Format = format,
                Labels = labels
            };
        }
    }
}
