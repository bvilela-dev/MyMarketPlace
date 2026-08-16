namespace Marketplace.Contracts.Events;

/// <summary>
/// Um novo usuario foi cadastrado.
/// </summary>
/// <remarks>
/// Publicado pelo <b>Identity</b> atraves do outbox, na mesma transacao que grava o
/// usuario. Consumido pelo <b>Notification</b>, que envia o e-mail de boas-vindas.
/// <para>
/// E o exemplo mais didatico de por que o outbox existe: enviar o e-mail de boas-vindas
/// nao pode fazer o cadastro falhar, mas tambem nao pode se perder se o RabbitMQ
/// estiver fora do ar no exato instante do cadastro.
/// </para>
/// </remarks>
/// <param name="EventId">Identificador unico desta ocorrencia.</param>
/// <param name="UserId">Identificador do usuario criado.</param>
/// <param name="Name">Nome de exibicao do usuario.</param>
/// <param name="Email">E-mail do usuario.</param>
/// <param name="CreatedAtUtc">Momento (UTC) do cadastro.</param>
public sealed record UserCreatedEvent(Guid EventId, Guid UserId, string Name, string Email, DateTime CreatedAtUtc);
