
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Npgsql;
using Signum.Engine.Json;
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

    public DateTime StartTime;
    
    public AzureServiceBusCacheInvalidator(string namespaceConnectionString, string topicName = "cache-invalidation")
    {
        this.TopicName = topicName;
        this.SubscriptionName = Environment.MachineName + "-" + Schema.Current.ApplicationName;

        adminClient = new ServiceBusAdministrationClient(namespaceConnectionString);
        client = new ServiceBusClient(namespaceConnectionString);
        sender = client.CreateSender(this.TopicName);
    }



    public void SendInvalidation(string cleanName)
    {
        sender.SendMessageAsync(new ServiceBusMessage(BinaryData.FromObjectAsJson(new AzureInvalidationMessage
        {
            CreationDate = DateTime.UtcNow,
            OriginMachineName = Environment.MachineName,
            OriginApplicationName = Schema.Current.ApplicationName,
            CleanName = cleanName,
        }, EntityJsonContext.FullJsonSerializerOptions))).Wait();
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
        processor = client.CreateProcessor(this.TopicName, this.SubscriptionName);
        processor.ProcessMessageAsync += Processor_ProcessMessageAsync;
        processor.ProcessErrorAsync += Processor_ProcessErrorAsync;
        StartTime = DateTime.UtcNow;
        await processor.StartProcessingAsync();
    }

    private Task Processor_ProcessErrorAsync(ProcessErrorEventArgs arg)
    {
        arg.Exception.LogException(ex => ex.ControllerName = nameof(AzureServiceBusCacheInvalidator));
        return Task.CompletedTask;
    }

    private async Task Processor_ProcessMessageAsync(ProcessMessageEventArgs arg)
    {
        try
        {
            var message = arg.Message.Body.ToObjectFromJson<AzureInvalidationMessage>(EntityJsonContext.FullJsonSerializerOptions);

            if (message.CreationDate < StartTime)
                return;

            if (message.OriginMachineName == Environment.MachineName &&
               message.OriginApplicationName == Schema.Current.ApplicationName)
                return;

            ReceiveInvalidation?.Invoke(message.CleanName);

            await arg.CompleteMessageAsync(arg.Message);
        }catch (Exception ex)
        {
            ex.LogException(ex => ex.ControllerName = nameof(AzureServiceBusCacheInvalidator));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await this.client.DisposeAsync();
    }
}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class AzureInvalidationMessage
{
    public DateTime CreationDate;
    public string OriginMachineName;
    public string OriginApplicationName;
    public string CleanName;
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
