using MediatR;

namespace Identity.Application.Auth;

/// <summary>
/// Comando de cadastro de um novo usuario.
/// </summary>
/// <remarks>
/// Comandos sao <c>record</c> imutaveis de proposito: uma vez criado, o payload da
/// requisicao nao muda enquanto atravessa o pipeline (logging → validacao → handler).
/// Isso elimina a classe de bug em que um behavior altera o comando e o handler recebe
/// algo diferente do que foi validado.
/// </remarks>
/// <param name="Name">Nome de exibicao do usuario.</param>
/// <param name="Email">E-mail, unico no sistema.</param>
/// <param name="Password">Senha em texto puro — convertida em hash antes de qualquer gravacao.</param>
public sealed record RegisterUserCommand(string Name, string Email, string Password) : IRequest<AuthResponse>;
