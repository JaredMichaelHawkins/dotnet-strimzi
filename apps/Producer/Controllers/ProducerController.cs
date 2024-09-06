using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Producer.Extensions;
using Shared.Diagnostics;

namespace Producer.Controllers;

[ApiController]
[Route("[controller]")]
public class ProducerController : ControllerBase
{
    private static readonly TextMapPropagator TextMapPropagator = Propagators.DefaultTextMapPropagator;
    private readonly ILogger<ProducerController> _logger;
    private readonly IConfiguration _config;
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];
    public ProducerController(ILogger<ProducerController> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    [HttpGet]
    public WeatherForecast Get()
    {
        // ref:
        // https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/examples/MicroserviceExample
        using var activity = OpenTelemetryExtensions.CreateActivitySource()
            .StartActivity("Producing");

        var weather = new WeatherForecast(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Random.Shared.Next(-20, 55),
            Summaries[Random.Shared.Next(Summaries.Length)]
        );

        string topicName = _config["Kafka:TopicName"]!;
        string serializedModel = JsonSerializer.Serialize(weather);

        // Semantic convention - OpenTelemetry messaging specification:
        // ref: https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
        var activityName = $"publish {topicName}";

        using var producer = KafkaExtensions.CreateProducer(_config);
        using var sendActivity = OpenTelemetryExtensions.CreateActivitySource()
            .StartActivity(activityName, ActivityKind.Producer);

        ActivityContext contextToInject = default;
        if (sendActivity != null)
        {
            contextToInject = sendActivity.Context;
        }
        else if (Activity.Current != null)
        {
            contextToInject = Activity.Current.Context;
        }

        var headers = new Headers();
        TextMapPropagator.Inject(new PropagationContext(contextToInject, Baggage.Current), headers,
            InjectTraceContextIntoHeaders);

        sendActivity?.SetTag(MessageTags.System, "kafka");
        sendActivity?.SetTag(MessageTags.DestinationTopic, topicName);
        sendActivity?.SetTag(MessageTags.Operation, "publish");
        sendActivity?.SetTag(MessageTags.Id, weather.Id);
        sendActivity?.SetTag(MessageTags.ClientId,
            $"Producer-{Environment.MachineName}");

        var result = producer.ProduceAsync(topicName,
            new Message<Null, string>
                { Value = serializedModel, Headers = headers }).Result;

        _logger.LogInformation("Kafka - Sending to topic {topicName} completed", topicName);

        return weather;
    }

    private void InjectTraceContextIntoHeaders(Headers headers, string key, string value)
    {
        try
        {
            headers.Add(key, Encoding.UTF8.GetBytes(value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject trace context.");
        }
    }
}
