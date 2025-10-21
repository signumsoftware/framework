using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Amazon.Runtime;
using Amazon;

namespace Signum.Files.S3;

public static class S3ConfigurationExtensions
{
    /// <summary>
    /// Creates a configured AmazonS3Client based on a .NET IConfiguration section.
    /// Expected configuration keys under the section: Endpoint, AccessKey, SecretKey, Region, ForcePathStyle, SessionToken
    /// Example appsettings.json:
    /// "S3": {
    ///   "Endpoint": "http://localhost:9000",
    ///   "AccessKey": "minioadmin",
    ///   "SecretKey": "minioadmin",
    ///   "ForcePathStyle": "true"
    /// }
    /// </summary>
    public static IAmazonS3? GetAmazonS3Client(this IConfigurationSection section)
    {
        var accessKey = section["AccessKey"];
        var secretKey = section["SecretKey"];
        var sessionToken = section["SessionToken"];

        if (accessKey.IsNullOrEmpty() && secretKey.IsNullOrEmpty())
            return null;

        if (accessKey.IsNullOrEmpty() || secretKey.IsNullOrEmpty())
            throw new InvalidOperationException($"S3 configuration section '{section.Path}' must at least provide AccessKey and SecretKey.");

        AWSCredentials creds = sessionToken.HasText() ? 
             new SessionAWSCredentials(accessKey, secretKey, sessionToken)
            : new BasicAWSCredentials(accessKey, secretKey);

        var config = new AmazonS3Config
        {
            ForcePathStyle = section["ForcePathStyle"]?.ToBool() ?? true,
        };

        var endpoint = section["Endpoint"];
        var region = section["Region"];
        if (endpoint.HasText())
        {
            config.ServiceURL = endpoint;
            config.UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        }
        else if (!string.IsNullOrEmpty(region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
        }

        return new AmazonS3Client(creds, config);
    }
}
