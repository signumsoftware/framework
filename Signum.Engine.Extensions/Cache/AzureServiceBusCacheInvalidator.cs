
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Npgsql;
using System.Diagnostics;

namespace Signum.Engine.Cache;

//Never Tested, works only in theory
//https://github.com/briandunnington/SynchronizedCache/blob/master/SynchronizedCache.cs
//https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/servicebus/Azure.Messaging.ServiceBus/MigrationGuide.md
public class AzureServiceBusCacheInvalidator : ICacheMultiServerInvalidator, IAsyncDisposable
{
    public event Action<string>? ReceiveInvalidation;

    ServiceBusAdministrationClient adminClient;
    ServiceBusClient client;
    
    ServiceBusSender sender;
    ServiceBusProcessor processor = null!; 

    string TopicName { get; set; }
    string SubscriptionName { get; set; }
    
    public AzureServiceBusCacheInvalidator(string namespaceConnectionString, string topicName = "cache-invalidation")
    {
        this.TopicName = topicName;
        this.SubscriptionName = Environment.MachineName + "-" + Schema.Current.ApplicationName;

        adminClient = new ServiceBusAdministrationClient(namespaceConnectionString);
        client = new ServiceBusClient(namespaceConnectionString);
        sender = client.CreateSender(this.TopicName);
    }

    private async Task Processor_ProcessMessageAsync(ProcessMessageEventArgs arg)
    {
        var cleanName = arg.Message.Body.ToString();
        ReceiveInvalidation?.Invoke(cleanName);

        await arg.CompleteMessageAsync(arg.Message);
    }

    public void SendInvalidation(string cleanName)
    {
        sender.SendMessageAsync(new ServiceBusMessage(cleanName)).Wait();
    }

    public void Start()
    {
        EnsureInitializationAsync().Wait();
        StartMessageListener().Wait();
    }

    async Task EnsureInitializationAsync()
    {

        if (!await adminClient.TopicExistsAsync(this.TopicName))
        {
            await adminClient.CreateTopicAsync(new CreateTopicOptions(this.TopicName)); /*Explore more options*/
        }

        if (!await adminClient.SubscriptionExistsAsync(this.TopicName, this.SubscriptionName))
        {
            await adminClient.CreateSubscriptionAsync(this.TopicName, this.SubscriptionName);
        }
    }

    async Task StartMessageListener()
    {
        processor = client.CreateProcessor(this.TopicName);
        processor.ProcessMessageAsync += Processor_ProcessMessageAsync;
        processor.ProcessMessageAsync += Processor_ProcessMessageAsync;

        await processor.StartProcessingAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await this.client.DisposeAsync();
    }
}
