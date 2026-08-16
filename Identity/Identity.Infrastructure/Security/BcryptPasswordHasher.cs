using Identity.Application.Abstractions;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Hash de senha com BCrypt.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que BCrypt e nao SHA-256?</b> Um hash rapido e otimo para integridade e
/// pessimo para senha: uma GPU calcula bilhoes de SHA-256 por segundo, o que torna
/// viavel testar todas as senhas de um vazamento. BCrypt e <i>deliberadamente lento</i>
/// e com custo ajustavel — o mesmo hardware que fazia bilhoes passa a fazer milhares.
/// </para>
/// <para>
/// <b>Salt automatico.</b> O BCrypt gera um salt aleatorio por senha e o embute no
/// proprio hash resultante. Por isso duas contas com a senha "123456" tem hashes
/// diferentes, e uma rainbow table nao ajuda o atacante.
/// </para>
/// <para>
/// <b>Work factor 12.</b> Padrao do pacote e ~250 ms por hash em hardware atual. Cada
/// incremento dobra o custo — bom equilibrio entre resistencia a forca bruta e nao
/// transformar o login num vetor de negacao de servico.
/// </para>
/// </remarks>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Custo do BCrypt (2^12 iteracoes).
    /// </summary>
    private const int WorkFactor = 12;

    /// <inheritdoc />
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    /// <inheritdoc />
    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash malformado (dado corrompido, ou o hash "dummy" usado no login para
            // igualar o tempo de resposta). Falhar como "senha invalida" e o correto:
            // deixar a excecao subir viraria HTTP 500 e revelaria o estado do registro.
            return false;
        }
    }
}
