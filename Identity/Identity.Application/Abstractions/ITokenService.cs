using Identity.Application.Models;
using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

/// <summary>
/// Geracao e conferencia de tokens de autenticacao.
/// </summary>
/// <remarks>
/// Interface declarada na camada de aplicacao e implementada na infraestrutura
/// (<c>JwtTokenService</c>). E a <b>inversao de dependencia</b> da Clean Architecture:
/// o caso de uso define o que precisa, a infraestrutura decide como fazer. Trocar JWT
/// por PASETO amanha nao toca em nenhum handler.
/// </remarks>
public interface ITokenService
{
    /// <summary>
    /// Gera o par access token + refresh token para um usuario autenticado.
    /// </summary>
    /// <param name="user">Usuario autenticado.</param>
    /// <returns>Par de tokens, com os respectivos vencimentos.</returns>
    TokenPair Generate(User user);

    /// <summary>
    /// Calcula o hash de um refresh token para busca e armazenamento.
    /// </summary>
    /// <remarks>
    /// O banco guarda somente o hash. Para validar um refresh token recebido, o fluxo
    /// e sempre: calcular o hash do valor apresentado e procurar por ele — nunca o
    /// contrario.
    /// </remarks>
    /// <param name="refreshToken">Valor do refresh token em texto puro.</param>
    /// <returns>Hash SHA-256 em Base64.</returns>
    string HashRefreshToken(string refreshToken);
}
