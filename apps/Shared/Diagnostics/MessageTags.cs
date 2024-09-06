namespace Shared.Diagnostics;

/// <summary>
/// Messaging tags
/// reference: https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/kafka.md
/// </summary>
public static class MessageTags
{
    /// <summary>
    /// The messaging system as identified by the client instrumentation.
    /// Should always be set to 'kafka'
    /// </summary>
    public const string System = "messaging.system";

    /// <summary>
    /// Message keys in Shared are used for grouping alike messages to ensure they're processed on the same partition.
    /// They differ from messaging.message.id in that they're not unique. If the key is null, the attribute MUST NOT be set.
    /// </summary>
    public const string Key = "messaging.kafka.message.key";

    /// <summary>
    /// Topic name.
    /// </summary>
    public const string DestinationTopic = "messaging.destination.name";

    /// <summary>
    /// A value used by the messaging system as an identifier for the message, represented as a string.
    /// </summary>
    public const string Id = "messaging.message.id";


    /// <summary>
    /// A string identifying the type of the messaging operation.
    /// Prefer to use these values before custom: publish; create; receive; process; settle;
    /// </summary>
    public const string Operation = "messaging.operation.type";


    /// <summary>
    /// Shared consumer group id.
    /// </summary>
    public const string ConsumerGroup = "messaging.consumer.group.name";

    /// <summary>
    /// String representation of the partition id the message (or batch) is sent to or received from.
    /// </summary>
    public const string Partition = "messaging.destination.partition.id";

    /// <summary>
    /// The offset of a record in the corresponding Shared partition.
    /// </summary>
    public const string Offset = "messaging.kafka.offset";

    /// <summary>
    /// A unique identifier for the client that consumes or produces a message.
    /// </summary>
    public const string ClientId = "messaging.client.id";

    /// <summary>
    /// The number of messages sent, received, or processed in the scope of the batching operation.
    /// </summary>
    public const string BatchMessageCount = "messaging.batch.message_count";
}

public static class MessagingDefaultValues
{
    /// <summary>
    /// The default messaging system as identified by the client instrumentation.
    /// </summary>
    public const string SystemKafka = "kafka";

    public const string OperationPublish = "publish";
    public const string OperationCreate = "create";
    public const string OperationReceive = "receive";
    public const string OperationProcess = "process";
    public const string OperationSettle = "settle";
}
