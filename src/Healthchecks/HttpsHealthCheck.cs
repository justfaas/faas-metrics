using Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class HttpsHealthCheck : IHealthCheck
{
    private static int count = 0;

    public Task<HealthCheckResult> CheckHealthAsync( HealthCheckContext context, CancellationToken cancellationToken = default )
    {
        if ( !CertificateBuilder.HasCertificate )
        {
            // no HTTPS; skip certificate validation
            return Task.FromResult( HealthCheckResult.Healthy() );
        }

        var isValid = 
            ( CertificateBuilder.CertNotBefore < DateTime.UtcNow ) && 
            ( CertificateBuilder.CertNotAfter > DateTime.UtcNow.AddDays( 1 ) );

        if ( !isValid )
        {
            return Task.FromResult( context.Fail( "TLS certificate has expired." ) );
        }

        count++;

        return Task.FromResult( HealthCheckResult.Healthy() );
    }
}
