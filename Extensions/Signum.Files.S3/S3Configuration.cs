using Amazon.S3;
using Amazon.Runtime;
using Amazon;

namespace Signum.Files.S3;

/// <summary>
/// Strongly typed configuration for S3 client creation.
/// "S3": {
///   "Endpoint": "http://localhost:9000",
///   "AccessKey": "minioadmin",
///   "SecretKey": "minioadmin",
///   "ForcePathStyle": "true"
/// }
/// </summary>
public class S3Configuration
{
    /// <summary>
    /// S3 endpoint URL (e.g., http://localhost:9000)
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// AWS Access Key
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// AWS Secret Key
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// AWS Session Token (optional)
    /// </summary>
    public string? SessionToken { get; set; }

    /// <summary>
    /// AWS Region (e.g., eu-west-1)
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Forces path style URLs (default: true)
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// Used for multi-tenant scenarios to share a single bucket name, like OBD
    /// </summary>
    public string? SharedBucketName { get; set; }

    public bool? CreateBucket { get; set; }

    /// <summary>
    /// Creates a configured AmazonS3Client based on this configuration.
    /// </summary>
    public IAmazonS3? ToAmazonS3Client()
    {
        if (string.IsNullOrEmpty(AccessKey) && string.IsNullOrEmpty(SecretKey))
            return null;

        if (string.IsNullOrEmpty(AccessKey) || string.IsNullOrEmpty(SecretKey))
            throw new InvalidOperationException($"S3 configuration must provide both AccessKey and SecretKey.");

        AWSCredentials creds = !string.IsNullOrEmpty(SessionToken) ?
            new SessionAWSCredentials(AccessKey, SecretKey, SessionToken)
            : new BasicAWSCredentials(AccessKey, SecretKey);

        var config = new AmazonS3Config
        {
            ForcePathStyle = ForcePathStyle,
        };


        if (!string.IsNullOrEmpty(Endpoint))
        {
            var endpoint = Endpoint;
            if (!endpoint.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) && Port != null) 
                endpoint = (Port == 433 ? "https://" : "http://") + Endpoint; //OpenShift OBC exposes BUCKET_HOST and BUCKET_PORT nativiely
            
            config.ServiceURL = endpoint;
            config.UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        }
        else if (!string.IsNullOrEmpty(Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(Region);
        }

        return new AmazonS3Client(creds, config);
    }
}
