using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Consumer.Extensions;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Shared.Diagnostics;

namespace Consumer;

public record WeatherForecast(Guid Id, DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class Worker : BackgroundService
{
    private readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;
    private readonly ILogger<Worker> _logger;
    private readonly string _topic;
    private readonly string _groupId;
    private readonly IConsumer<Ignore, string> _consumer;

    public Worker(ILogger<Worker> logger,
        IConfiguration configuration)
    {
        Task.Delay(TimeSpan.FromSeconds(15)).Wait();
        _logger = logger;
        var configuration1 = configuration;
        _topic = configuration1["Kafka:TopicName"]!;
        _groupId = configuration1["Kafka:ConsumerSettings:GroupId"]!;
        _consumer = KafkaExtensions.CreateConsumer(configuration1);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Topic = {topic}", _topic);
        _logger.LogInformation("Group Id = {groupId}", _groupId);
        _logger.LogInformation("Waiting for messages");
        _consumer.Subscribe(_topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Run(() =>
            {
                var result = _consumer.Consume(cancellationToken);
                _logger.LogInformation("Message recieved");
                var parentContext = _propagator.Extract(default, result.Message.Headers, this.ExtractTraceContextFromHeaders);
                Baggage.Current = parentContext.Baggage;

                var messageContent = result.Message.Value;
                var partition = result.Partition.Value;

                // Semantic convention - OpenTelemetry messaging specification:
                // ref: https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
                var activityName = $"receive {_topic}";

                using var activity = OpenTelemetryExtensions.CreateActivitySource()
                    .StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext);
                activity?.SetTag(MessageTags.DestinationTopic, _topic);
                activity?.SetTag(MessageTags.ConsumerGroup, _groupId);
                activity?.SetTag(MessageTags.ClientId,
                    $"Consumer-{Environment.MachineName}");
                activity?.SetTag(MessageTags.Partition, partition);
                activity?.SetTag(MessageTags.System, "kafka");
                activity?.SetTag(MessageTags.Operation, "receive");

                ProcessResult(messageContent);
            }, cancellationToken);
        }
    }

    private void ProcessResult(string data)
    {
        try
        {
            WeatherForecast? result = JsonSerializer.Deserialize<WeatherForecast>(data,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            _logger.LogInformation("Event result model: {result}", result);
        }
        catch
        {
            _logger.LogError("Invalid data in result");
        }

    }

    private IEnumerable<string> ExtractTraceContextFromHeaders(Headers headers, string key)
    {
        try
        {
            var header = headers.FirstOrDefault(h => h.Key == key);
            if (header is not null)
                return new[] { Encoding.UTF8.GetString(header.GetValueBytes()) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failure while extracting trace context: {message}", ex.Message);
        }

        return [];
    }
}
