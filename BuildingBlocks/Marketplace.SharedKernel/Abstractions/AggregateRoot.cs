namespace Marketplace.SharedKernel.Abstractions;

/// <summary>
/// Raiz de agregado: a unica entidade do agregado que o mundo externo pode referenciar.
/// </summary>
/// <remarks>
/// <para>
/// Um <b>agregado</b> e um grupo de entidades que precisa ser salvo e validado como um
/// bloco unico. A raiz e a porta de entrada: por exemplo, <c>Order</c> e a raiz e
/// <c>OrderItem</c> so existe atraves dela. Ninguem carrega ou altera um
/// <c>OrderItem</c> diretamente.
/// </para>
/// <para>
/// Consequencias praticas adotadas neste projeto:
/// <list type="bullet">
///   <item>Repositorios/DbSets sao expostos apenas para raizes de agregado.</item>
///   <item>Uma transacao grava um agregado — o que garante que as invariantes
///   internas (ex.: total do pedido = soma dos itens) nunca fiquem inconsistentes.</item>
///   <item>A comunicacao entre agregados diferentes e feita por eventos, nunca por
///   referencia direta de objeto.</item>
/// </list>
/// </para>
/// </remarks>
public abstract class AggregateRoot : Entity;
