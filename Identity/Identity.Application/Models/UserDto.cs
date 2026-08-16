namespace Identity.Application.Models;

/// <summary>
/// Perfil publico de um usuario.
/// </summary>
/// <remarks>
/// Repare no que <b>nao</b> esta aqui: <c>PasswordHash</c> e a lista de refresh tokens.
/// Devolver a entidade de dominio direto no endpoint vazaria os dois. E a razao pratica
/// de existir um DTO separado, alem do desacoplamento entre modelo interno e contrato.
/// </remarks>
/// <param name="Id">Identificador do usuario.</param>
/// <param name="Name">Nome de exibicao.</param>
/// <param name="Email">E-mail normalizado.</param>
/// <param name="CreatedAtUtc">Momento (UTC) do cadastro.</param>
/// <param name="Addresses">Enderecos cadastrados.</param>
public sealed record UserDto(Guid Id, string Name, string Email, DateTime CreatedAtUtc, IReadOnlyCollection<AddressDto> Addresses);
