using Marketplace.SharedKernel.Abstractions;

namespace Marketplace.SharedKernel.Tests.Abstractions;

/// <summary>
/// Testes do acumulo e da limpeza de eventos de dominio.
/// </summary>
public sealed class EntityTests
{
    private sealed record TestEvent(string Nome) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    }

    private sealed class TestAggregate : AggregateRoot
    {
        public TestAggregate() => Id = Guid.NewGuid();

        public void Executar(string nome) => Raise(new TestEvent(nome));
    }

    [Fact]
    public void Agregado_novo_nao_tem_eventos()
        => new TestAggregate().DomainEvents.ShouldBeEmpty();

    [Fact]
    public void Eventos_sao_acumulados_na_ordem_em_que_ocorrem()
    {
        var agregado = new TestAggregate();

        agregado.Executar("primeiro");
        agregado.Executar("segundo");

        agregado.DomainEvents.Count.ShouldBe(2);
        agregado.DomainEvents.OfType<TestEvent>().Select(e => e.Nome)
            .ShouldBe(["primeiro", "segundo"]);
    }

    [Fact]
    public void Limpar_eventos_evita_publicacao_duplicada()
    {
        var agregado = new TestAggregate();
        agregado.Executar("acao");

        agregado.ClearDomainEvents();

        agregado.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Colecao_de_eventos_e_somente_leitura()
        => new TestAggregate().DomainEvents.ShouldBeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
}
