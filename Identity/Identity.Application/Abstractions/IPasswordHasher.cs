namespace Identity.Application.Abstractions;

/// <summary>
/// Operacoes de hash de senha.
/// </summary>
/// <remarks>
/// A abstracao existe para permitir trocar o algoritmo sem tocar nos casos de uso —
/// migrar de BCrypt para Argon2id, por exemplo, mexe em uma unica classe da
/// infraestrutura.
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>
    /// Converte uma senha em texto puro no hash a ser persistido.
    /// </summary>
    /// <param name="password">Senha informada pelo usuario.</param>
    /// <returns>Hash com salt embutido.</returns>
    string Hash(string password);

    /// <summary>
    /// Confere uma senha contra o hash armazenado.
    /// </summary>
    /// <param name="password">Senha informada.</param>
    /// <param name="passwordHash">Hash guardado no banco.</param>
    /// <returns><see langword="true"/> quando a senha confere.</returns>
    bool Verify(string password, string passwordHash);
}
