using Confluent.Kafka;

namespace Producer.Extensions;

public static class KafkaExtensions
{

    public static IProducer<Null, string> CreateProducer(
        IConfiguration configuration)
    {
        return new ProducerBuilder<Null, string>(
            new ProducerConfig()
            {
                BootstrapServers = configuration["Kafka:ProducerSettings:BootstrapServers"]
            }).Build();
    }
}
