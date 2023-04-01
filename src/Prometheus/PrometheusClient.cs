using System.Text.Json;
using Microsoft.Extensions.Options;

internal sealed class PrometheusClient
{
    private readonly ILogger logger;
    private readonly HttpClient httpClient;
    private readonly IEnumerable<string> sources = new string[]
    {
        "http://prometheus-server.faas.svc.cluster.local:9090"
    };

    public PrometheusClient( ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IOptions<PrometheusOptions> optionsAccessor )
    {
        logger = loggerFactory.CreateLogger<PrometheusClient>();
        httpClient = httpClientFactory.CreateClient( "prometheus" );

        var options = optionsAccessor.Value;

        if ( options.Sources?.Any() == true )
        {
            sources = options.Sources;
        }
    }

    public async Task<QueryResult?> QueryAsync( string query )
    {
        var tasks = sources.Select( 
            async source =>
            {
                try
                {
                    return await httpClient.GetFromJsonAsync<QueryResult>( $"{source}/api/v1/query?query={query}" );
                }
                catch ( Exception ex )
                {
                    logger.LogError( ex, ex.Message );

                    return ( null );
                }
            }
        );

        var results = await Task.WhenAll( tasks );

        var succesful = results.Where( x => x != null )
            .Where( x => x!.Status?.Equals( "success" ) == true )
            .Where( x => x!.Data?.Result.Any() == true )
            .Where( x => x!.Data?.Result.First().Value.Any() == true )
            .OrderByDescending( x => ( (JsonElement)x!.Data!.Result.First().Value.First() ).Deserialize<double>() )
            .ToArray();

        /*
        if the query is found in more than one source
        we return the most recent one
        */
        return succesful.FirstOrDefault();
    }
}
