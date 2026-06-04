using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Abstractions
{
    /// <summary>Publicação de eventos de IR (abstração — implementada na Infraestrutura).</summary>
    public interface IKafkaProducer
    {
        Task PublicarEventoIR(EventoIR evento);
    }
}
