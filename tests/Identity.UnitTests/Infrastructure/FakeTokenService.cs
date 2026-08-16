using System.Security.Cryptography;
using System.Text;
using Identity.Application.Abstractions;
using Identity.Application.Models;
using Identity.Domain.Entities;

namespace Identity.UnitTests.Infrastructure;

/// <summary>
/// Implementacao de <see cref="ITokenService"/> para testes, sem assinatura de JWT.
/// </summary>
/// <remarks>
/// Gera tokens previsiveis e rapidos. O que interessa nos testes de caso de uso e o
/// <i>fluxo</i> (emitiu token? gravou o hash? rotacionou?), nao a criptografia — que e
/// responsabilidade da implementacao real e do proprio .NET.
/// <para>
/// O hash usa o mesmo algoritmo do <c>JwtTokenService</c> para que os testes consigam
/// conferir "o que foi gravado corresponde ao token entregue".
/// </para>
/// </remarks>
public sealed class FakeTokenService : ITokenService
{
    // Contador ESTATICO de proposito: varios FakeTokenService podem coexistir no mesmo
    // teste (um no cadastro, outro no login). Com contador por instancia, os dois
    // emitiriam "refresh-token-1" e o banco ficaria com duas linhas de hash identico —
    // um falso positivo de colisao que nao existe no servico real, onde o token tem
    // 64 bytes aleatorios.
    private static int _counter;

    /// <inheritdoc />
    public TokenPair Generate(User user)
    {
        var sequencial = Interlocked.Increment(ref _counter);

        return new TokenPair(
            $"access-token-{sequencial}",
            DateTime.UtcNow.AddMinutes(15),
            $"refresh-token-{sequencial}",
            DateTime.UtcNow.AddDays(30));
    }

    /// <inheritdoc />
    public string HashRefreshToken(string refreshToken) => Hash(refreshToken);

    /// <summary>
    /// Calcula o hash de um refresh token, igual ao servico real.
    /// </summary>
    /// <param name="refreshToken">Token em texto puro.</param>
    /// <returns>Hash SHA-256 em Base64.</returns>
    public static string Hash(string refreshToken)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
