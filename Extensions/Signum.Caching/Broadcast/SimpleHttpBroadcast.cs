using Signum.API.Json;
using System.Net.Http;
using System.Net.Http.Json;

namespace Signum.Cache.Broadcast;

public class SimpleHttpBroadcast : IServerBroadcast
{

    HttpClient client = new HttpClient();
    readonly string bordcastSecretHash;
    readonly string[] broadcastUrls;

    public SimpleHttpBroadcast(string broadcastSecret, string broadcastUrls)
    {
        this.bordcastSecretHash = Convert.ToBase64String(PasswordEncoding.EncodePassword("", broadcastSecret).Last());
        this.broadcastUrls = broadcastUrls
            .SplitNoEmpty(new char[] { ';', ',' } /*In theory ; and , are valid in a URL, but since we talk only domain names or IPs...*/)
            .Select(a => a.Trim())
            .Where(a => a.HasText())
            .ToArray();
    }

    public event Action<string, string>? Receive;

    public void Start()
    {
    }

    //Called from Controller
    public void InvalidateTable(InvalidateTableRequest request)
    {
        if (this.bordcastSecretHash != request.SecretHash)
            throw new InvalidOperationException("invalidationSecret does not match");

        if (request.OriginMachineName == Environment.MachineName &&
            request.OriginApplicationName == Schema.Current.ApplicationName)
            return;

        Receive?.Invoke(request.MethodName, request.Argument);
    }

    public void Send(string methodName, string argument)
    {
        var request = new InvalidateTableRequest
        {
            MethodName = methodName,
            Argument = argument,
            SecretHash = this.bordcastSecretHash,
            OriginMachineName = Environment.MachineName,
            OriginApplicationName = Schema.Current.ApplicationName,
        };

        foreach (var url in broadcastUrls)
        {
            string? errorBody = null; 
            try
            {
                var fullUrl = url.TrimEnd('/') + "/api/cache/invalidateTable";

                var json = JsonContent.Create(request, options: EntityJsonContext.FullJsonSerializerOptions /*SignumServer.JsonSerializerOptions*/);

                var response = client.PostAsync(fullUrl, json).Result;

                if (!response.IsSuccessStatusCode)
                {
                    errorBody = response.Content.ReadAsStringAsync().Result;
                }

            }
           
            catch (Exception e)
            {
                e.LogException(a =>
                {
                    a.ControllerName = nameof(SimpleHttpBroadcast);
                    a.Data.Text = errorBody;
                });
            }
        }
    }

    public override string ToString()
    {
        return $"{nameof(SimpleHttpBroadcast)}(Urls={broadcastUrls.ToString(", ")})";
    }


}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class InvalidateTableRequest
{
    public string OriginMachineName;
    public string OriginApplicationName;
    public string SecretHash;
    public string Argument;
    public string MethodName;
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
