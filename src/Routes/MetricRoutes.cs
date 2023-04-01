using System.Text.Json;

internal static class MetricsRoutes
{
    public static IEndpointRouteBuilder MapMetrics( this IEndpointRouteBuilder app )
    {
        app.MapGet( "/apis/custom.metrics.k8s.io/v1beta1", ListMetricsAsync );

        app.MapGet( "/apis/custom.metrics.k8s.io/v1beta1/namespaces/{ns}/{kind}/{name}/{metricName}"
            , NamespacedMetricsAsync );

        return ( app );
    }

    private static Task<IResult> ListMetricsAsync( Metrics metrics )
    {
        var list = new V1ApiResourceList
        {
            GroupVersion = "custom.metrics.k8s.io/v1beta1",
            Resources = metrics.GetNames().Select( metricName => new V1ApiResource
            {
                Kind = "MetricValueList",
                Name = metricName,
                Namespaced = true,
                Verbs = new string[] { "GET" }
            } ).ToArray()
        };

        return Task.FromResult( Results.Ok( list ) );
    }

    private static async Task<IResult> NamespacedMetricsAsync( HttpContext httpContext
        , string ns
        , string kind
        , string name
        , string metricName
        , ILoggerFactory loggerFactory
        , PrometheusClient prometheus
        , Metrics metrics )
    {
        var logger = loggerFactory.CreateLogger( "metrics" );

        logger.LogInformation( $"requested metric {ns}/{kind}/{name}/{metricName}" );

        // we only provide metrics for 'kind: functions.justfaas.com'
        if ( !kind.Equals( "functions.justfaas.com", StringComparison.OrdinalIgnoreCase ) )
        {
            logger.LogWarning( $"kind {kind} is not supported. returning an empty list." );

            return Results.Ok( V1Beta1MetricValueList.Empty );
        }

        // ignore non-mapped metrics
        if ( !metrics.GetNames().Contains( metricName ) )
        {
            logger.LogWarning( $"metric {metricName} is not supported. returning an empty list." );

            return Results.Ok( V1Beta1MetricValueList.Empty );
        }

        var query = metrics.GetQuery( metricName, new Dictionary<string, string>
        {
            { "namespace", ns },
            { "function", name }
        } );

        var queryResult = await prometheus.QueryAsync( query );

        if ( queryResult == null )
        {
            logger.LogWarning( $"query returned no results. returning an empty list." );

            return Results.Ok( V1Beta1MetricValueList.Empty );
        }

        var unixTimestamp = ( (JsonElement)queryResult.Data!.Result.First().Value.First() ).Deserialize<double>();
        var value = ( (JsonElement)queryResult.Data!.Result.First().Value.ElementAt( 1 ) ).Deserialize<string>();
        var timestamp = DateTimeOffset.UnixEpoch.AddSeconds( unixTimestamp );

        var list = new V1Beta1MetricValueList
        {
            Metadata = new Dictionary<string, string>
            {
                //
            },
            Items = new V1Beta1MetricValue[]
            {
                new V1Beta1MetricValue
                {
                    DescribedObject = new V1Beta1MetricValue.DescribedObjectSpec
                    {
                        Kind = kind,
                        ApiVersion = "v1",
                        Namespace = ns,
                        Name = name
                    },
                    MetricName = metricName,
                    Timestamp = timestamp,
                    Value = value
                }
            }
        };

        logger.LogInformation( $"returning metric {ns}/{kind}/{name}/{metricName}: {value}" );

        return Results.Ok( list );
    }
}
