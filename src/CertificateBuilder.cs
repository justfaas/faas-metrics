using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

internal static class CertificateBuilder
{
    public static string CertPassword
    {
        get
        {
            if ( string.IsNullOrEmpty( HttpsOptions.CertPassword ) )
            {
                // no password defined... use insecure password since we can't generate certs without passwords
                return "insecure-cert-password";
            }

            return System.Text.Encoding.UTF8.GetString( Convert.FromBase64String( HttpsOptions.CertPassword ) );
        }
    }

    public static bool HasCertificate { get; private set; }
    public static DateTime CertNotBefore { get; private set; }
    public static DateTime CertNotAfter { get; private set; }

    public static void Build( string filepath )
    {
        if ( !File.Exists( filepath ) )
        {
            CreateOrReplace( filepath );
        }

        // TODO: validate certificate
        using ( var cert = new X509Certificate2( filepath, CertPassword ) )
        {
            CertNotBefore = cert.NotBefore;
            CertNotAfter = cert.NotAfter;

            var isValid = ( CertNotBefore < DateTime.UtcNow ) && ( CertNotAfter > DateTime.UtcNow.AddDays( 1 ) );

            if ( !isValid )
            {
                CreateOrReplace( filepath );
            }
        }

        HasCertificate = true;
    }

    private static void CreateOrReplace( string filepath )
    {
        using ( var parent = RSA.Create( 4096 ) )
        using ( var rsa = RSA.Create( 2048 ) )
        {
            var parentRequest = new CertificateRequest(
                "CN=faactory.io",
                parent,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            parentRequest.CertificateExtensions.Add( new X509BasicConstraintsExtension( 
                certificateAuthority: true,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true
            ) );

            parentRequest.CertificateExtensions.Add( new X509SubjectKeyIdentifierExtension(
                key: parentRequest.PublicKey,
                critical: false
            ) );

            using ( var parentCert = parentRequest.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays( -45 ),
                DateTimeOffset.UtcNow.AddDays( 365 )
            ) )
            {
                var request = new CertificateRequest(
                    "CN=faas.faactory.io",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );

                request.CertificateExtensions.Add( new X509BasicConstraintsExtension(
                    certificateAuthority: false,
                    hasPathLengthConstraint: false,
                    pathLengthConstraint: 0,
                    critical: false
                ) );

                request.CertificateExtensions.Add( new X509KeyUsageExtension(
                    keyUsages: X509KeyUsageFlags.CrlSign 
                        | X509KeyUsageFlags.DigitalSignature 
                        | X509KeyUsageFlags.KeyCertSign
                        | X509KeyUsageFlags.KeyEncipherment
                        | X509KeyUsageFlags.NonRepudiation,
                    critical: false
                ) );

                request.CertificateExtensions.Add( new X509EnhancedKeyUsageExtension(
                    enhancedKeyUsages: new OidCollection
                    {
                        new Oid( "1.3.6.1.4.1.311.84.1.1" ),
                        new Oid( "1.3.6.1.5.5.7.3.1" ),
                        new Oid( "1.3.6.1.5.5.7.3.8" ),
                    },
                    critical: true
                ) );

                request.CertificateExtensions.Add( new X509SubjectKeyIdentifierExtension(
                    key: request.PublicKey,
                    critical: false
                ) );

                using ( var cert = request.Create(
                    parentCert,
                    DateTimeOffset.UtcNow.AddDays( -1 ),
                    DateTimeOffset.UtcNow.AddDays( 90 ),
                    BitConverter.GetBytes( DateTime.UtcNow.Ticks )
                ) )
                using ( var certWithPrivateKey = cert.CopyWithPrivateKey( rsa ) )
                {
                    var buffer = certWithPrivateKey.Export( X509ContentType.Pfx, CertPassword );

                    File.WriteAllBytes( filepath, buffer );
                }
            }
        }
    }
}
