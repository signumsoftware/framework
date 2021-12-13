
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Npgsql;
using Signum.Engine.Json;
using Signum.Entities.Cache;
using Signum.Services;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;

namespace Signum.Engine.Cache;


public class SimpleHttpCacheInvalidator : ICacheMultiServerInvalidator {

    HttpClient client = new HttpClient();
    readonly string invalidationSecretHash;
    readonly string[] invalidateUrls;

    public SimpleHttpCacheInvalidator(string invalidationSecret, string[] invalidateUrls)
    {
        this.invalidationSecretHash = Convert.ToBase64String(Security.EncodePassword(invalidationSecret));
        this.invalidateUrls = invalidateUrls;
    }

    public event Action<string>? ReceiveInvalidation;

    public void Start()
    {
    }

    //Called from Controller
    public void InvalidateTable(InvalidateTableRequest request)
    {
        if (this.invalidationSecretHash != request.InvalidationSecretHash)
            throw new InvalidOperationException("invalidationSecret does not match");

        if (request.OriginMachineName == Environment.MachineName ||
            request.OriginApplicationName == Schema.Current.ApplicationName)
            return;

        ReceiveInvalidation?.Invoke(request.CleanName);
    }

    public void SendInvalidation(string cleanName)
    {
        var request = new InvalidateTableRequest
        {
            CleanName = cleanName,
            InvalidationSecretHash = this.invalidationSecretHash,
            OriginMachineName = Environment.MachineName,
            OriginApplicationName = Schema.Current.ApplicationName,
        };

        foreach (var url in invalidateUrls)
        {
            try
            {
                var fullUrl = url.TrimEnd('/') + "/api/cache/invalidateTable";

                var json = JsonContent.Create(request, options: EntityJsonContext.FullJsonSerializerOptions /*SignumServer.JsonSerializerOptions*/);

                var response = client.PostAsync(fullUrl, json).Result.EnsureSuccessStatusCode();
            }
            catch(Exception e)
            {
                e.LogException();
            }
        }
    }
}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class InvalidateTableRequest
{
    public string OriginMachineName;
    public string OriginApplicationName;
    public string CleanName;
    public string InvalidationSecretHash; 
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
