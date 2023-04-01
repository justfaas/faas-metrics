using System.CommandLine;
using System.Text;

// command line arguments
/*
We don't use the command line configuration extensions
because they don't support multiple values; the command line parser does.
*/
var sourceOption = new System.CommandLine.Option<string[]>( "--source" );
var prometheusSources = sourceOption.Parse( args )
    .GetValueForOption<string[]>( sourceOption )!;

// builder
var builder = WebApplication.CreateBuilder( args );

// configuration
builder.Configuration.AddJsonFile( "certs.json", optional: true )
    .AddCommandLine( args, new Dictionary<string, string>
    {
        { "--cert.file", "certFilename" },
        { "--metrics.file", "metricsFilename" }
    });

// logging
builder.Logging.ClearProviders()
    .AddSimpleConsole( options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    } )
    .AddFilter( "Microsoft.AspNetCore.Http.Result.OkObjectResult", LogLevel.Warning )
    .AddFilter( "Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning )
    ;

// services
builder.Services.Configure<PrometheusOptions>( options =>
{
    options.Sources = prometheusSources;
} );

builder.Services.ConfigureHttpJsonOptions( options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
} );

builder.Services.AddSingleton<Metrics>()
    .Configure<MetricsOptions>( options =>
    {
        var metricsFilename = builder.Configuration["metricsFilename"];

        if ( metricsFilename != null )
        {
            options.Metrics = MetricsOptions.ReadMetricsFromFile( metricsFilename );
        }
    });
builder.Services.AddTransient<PrometheusClient>()
    .AddHttpClient( "prometheus" );

// health checks
/*
The HttpsHealthCheck monitors the TLS self-signed certificate (if it exists).
If the certificate is no longer valid, the application becomes unhealthy.
When restarted, a new certificate is created.
*/
var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck( "https"
        , new HttpsHealthCheck()
        , Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy
        , new string[] { "https", "liveness" } );

// kestrel
builder.WebHost.ConfigureKestrel( kestrel =>
{
    kestrel.ListenAnyIP( 8080 );

    string certFilename = HttpsOptions.CertFilename;
    string certPassword = CertificateBuilder.CertPassword;

    if ( string.IsNullOrEmpty( certFilename ) && builder.Configuration["certFilename"] != null )
    {
        certFilename = builder.Configuration["certFilename"]!;
    }

    if ( string.IsNullOrEmpty( certPassword ) && builder.Configuration["certPassword"] != null )
    {
        certPassword = Encoding.UTF8.GetString(
            Convert.FromBase64String( builder.Configuration["certPassword"]! )
        );
    }

    // set up self-signed TLS
    if ( !string.IsNullOrEmpty( certFilename ) )
    {
        // this creates a new self-signed certificate if there isn't one yet or if it has expired.
        CertificateBuilder.Build( certFilename );

        kestrel.ListenAnyIP( 6443, options =>
        {
            options.UseHttps( certFilename, certPassword );
        } );
    }
} );

// app
var app = builder.Build();

app.MapHealthChecks( "/healthz" );
app.MapMetrics();

app.Run();
