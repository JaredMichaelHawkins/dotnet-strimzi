using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Consumer.Extensions;

public static class KafkaExtensions
{
    public static IConsumer<Ignore, string> CreateConsumer(
        IConfiguration configuration)
    {
        return new ConsumerBuilder<Ignore, string>(
            new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:ConsumerSettings:BootstrapServers"],
                GroupId = configuration["Kafka:ConsumerSettings:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Earliest
            }).Build();
    }
}