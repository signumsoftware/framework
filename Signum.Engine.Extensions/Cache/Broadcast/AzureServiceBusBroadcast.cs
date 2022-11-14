
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Npgsql;
using Signum.Engine.Json;
using System.Diagnostics;

namespace Signum.Engine.Cache;

//Never Tested, works only in theory
//https://github.com/briandunnington/SynchronizedCache/blob/master/SynchronizedCache.cs
//https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/servicebus/Azure.Messaging.ServiceBus/MigrationGuide.md
public class AzureServiceBusBroadcast : IServerBroadcast, IAsyncDisposable
{
    public event Action<string, string>? Receive;

    ServiceBusAdministrationClient adminClient;
    ServiceBusClient client;
    
    ServiceBusSender sender;
    ServiceBusProcessor processor = null!; 

    string TopicName { get; set; }
    string SubscriptionName { get; set; }

    public DateTime StartTime;
    
    public AzureServiceBusBroadcast(string namespaceConnectionString, string topicName = "cache-invalidation")
    {
        this.TopicName = topicName;
        this.SubscriptionName = Environment.MachineName + "-" + Schema.Current.ApplicationName;

        adminClient = new ServiceBusAdministrationClient(namespaceConnectionString);
        client = new ServiceBusClient(namespaceConnectionString);
        sender = client.CreateSender(this.TopicName);
    }


    public void Send(string methodName, string argument)
    {
        sender.SendMessageAsync(new ServiceBusMessage(BinaryData.FromObjectAsJson(new AzureInvalidationMessage
        {
            CreationDate = DateTime.UtcNow,
            OriginMachineName = Environment.MachineName,
            OriginApplicationName = Schema.Current.ApplicationName,
            MethodName = methodName,
            Argument = argument,
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
        arg.Exception.LogException(ex => ex.ControllerName = nameof(AzureServiceBusBroadcast));
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

            Receive?.Invoke(message.MethodName, message.Argument);

            await arg.CompleteMessageAsync(arg.Message);
        }catch (Exception ex)
        {
            ex.LogException(ex => ex.ControllerName = nameof(AzureServiceBusBroadcast));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await this.client.DisposeAsync();
    }

    public override string ToString()
    {
        return $"{nameof(AzureServiceBusBroadcast)}(TopicName = {TopicName}, SubscriptionName = {SubscriptionName})";
    }

}

public class AzureInvalidationMessage
{
    public required DateTime CreationDate;
    public required string OriginMachineName;
    public required string OriginApplicationName;
    public required string MethodName;
    public required string Argument;
}
