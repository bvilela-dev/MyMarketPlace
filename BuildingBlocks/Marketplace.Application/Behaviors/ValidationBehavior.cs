using FluentValidation;
using MediatR;

namespace Marketplace.Application.Behaviors;

/// <summary>
/// Executa os validadores do FluentValidation antes de qualquer handler do MediatR.
/// </summary>
/// <remarks>
/// <para>
/// <b>O que e um pipeline behavior?</b> O MediatR envolve o handler numa cadeia de
/// middlewares, igual ao pipeline do ASP.NET Core. Cada behavior recebe o request e um
/// delegate <c>next</c> apontando para o proximo elo:
/// </para>
/// <code>
/// Send(command)
///   └─ LoggingBehavior      (mede tempo, loga entrada/saida)
///        └─ ValidationBehavior  (voce esta aqui)
///             └─ CreateOrderCommandHandler   (regra de negocio, ja com dados validos)
/// </code>
/// <para>
/// <b>Por que isso vale a pena?</b> Sem o behavior, todo handler comecaria com dez
/// linhas de <c>if (string.IsNullOrWhiteSpace(...)) throw ...</c>. Concentrando a
/// validacao aqui, o handler assume que os dados ja chegaram validos e cuida so da
/// regra de negocio — que e o que realmente importa testar.
/// </para>
/// <para>
/// <b>Quem transforma isso em HTTP 400?</b> A <see cref="ValidationException"/>
/// lancada aqui sobe ate o <c>GlobalExceptionHandlingMiddleware</c>, que a converte
/// num <c>ValidationProblemDetails</c> (RFC 9457) com a lista de campos invalidos.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">Tipo do comando ou query sendo validado.</typeparam>
/// <typeparam name="TResponse">Tipo da resposta devolvida pelo handler.</typeparam>
/// <param name="validators">
/// Todos os validadores registrados para <typeparamref name="TRequest"/>. O container
/// injeta uma colecao vazia quando nao existe validador — e nesse caso o behavior
/// simplesmente repassa a chamada adiante.
/// </param>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Atalho: sem validadores registrados nao ha por que alocar contexto nem Task.WhenAll.
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        // Todos os validadores rodam em paralelo; nenhum depende do resultado do outro.
        var results = await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        // Erros de TODOS os validadores sao agregados de uma vez. A alternativa
        // (falhar no primeiro erro) obrigaria o cliente a corrigir o formulario
        // campo a campo, uma requisicao por vez.
        var failures = results.SelectMany(result => result.Errors).Where(failure => failure is not null).ToArray();

        if (failures.Length != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
