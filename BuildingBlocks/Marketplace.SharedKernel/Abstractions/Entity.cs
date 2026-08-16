namespace Marketplace.SharedKernel.Abstractions;

/// <summary>
/// Classe base das entidades de dominio.
/// </summary>
/// <remarks>
/// <para>
/// Uma <b>entidade</b> tem identidade propria: dois objetos com os mesmos dados mas
/// <see cref="Id"/> diferentes sao entidades diferentes. E o oposto de um
/// <see cref="ValueObject"/>, que e comparado pelo conteudo.
/// </para>
/// <para>
/// A entidade tambem acumula <b>eventos de dominio</b> (<see cref="IDomainEvent"/>).
/// A regra pratica e: o metodo de negocio altera o estado e registra o fato ocorrido
/// via <see cref="Raise"/>; quem persiste decide quando publicar. Isso mantem o
/// dominio livre de qualquer dependencia de mensageria.
/// </para>
/// </remarks>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Identificador unico da entidade.
    /// </summary>
    /// <remarks>
    /// O setter e <c>protected</c> para que apenas a propria entidade defina sua
    /// identidade (normalmente no construtor). Codigo externo nunca troca o Id.
    /// </remarks>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Eventos de dominio ainda nao despachados.
    /// </summary>
    /// <remarks>
    /// Exposto como somente leitura para impedir que a lista interna seja manipulada
    /// de fora da entidade.
    /// </remarks>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Registra um evento de dominio ocorrido dentro do agregado.
    /// </summary>
    /// <param name="domainEvent">Fato de negocio que acabou de acontecer.</param>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Limpa os eventos pendentes.
    /// </summary>
    /// <remarks>
    /// Deve ser chamado depois que os eventos foram efetivamente despachados,
    /// evitando publicacao duplicada caso a mesma instancia continue em memoria.
    /// </remarks>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
