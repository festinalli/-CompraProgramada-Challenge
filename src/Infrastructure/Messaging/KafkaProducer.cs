using Application.Abstractions;
using Confluent.Kafka;
using Domain.Entities;
using System.Text.Json;

namespace Infrastructure.Messaging
{
    public class KafkaProducer : IKafkaProducer
    {
        private readonly ProducerConfig _config;
        private const string TopicName = "corretora-ir-events";

        public KafkaProducer(string bootstrapServers)
        {
            _config = new ProducerConfig { BootstrapServers = bootstrapServers };
        }

        public async Task PublicarEventoIR(EventoIR evento)
        {
            using var producer = new ProducerBuilder<string, string>(_config).Build();
            
            var message = new Message<string, string>
            {
                Key = evento.ClienteId.ToString(),
                Value = JsonSerializer.Serialize(new
                {
                    evento.Id,
                    evento.ClienteId,
                    evento.CPF,
                    Tipo = evento.Tipo.ToString(),
                    evento.ValorBase,
                    evento.Aliquota,
                    evento.ValorImposto,
                    evento.DataEvento,
                    evento.Ticker,
                    evento.Descricao
                })
            };

            await producer.ProduceAsync(TopicName, message);
        }
    }
}
