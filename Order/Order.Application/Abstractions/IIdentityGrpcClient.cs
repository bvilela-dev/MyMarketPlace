using Marketplace.Contracts.Grpc;

namespace Order.Application.Abstractions;

/// <summary>
/// Operacoes do Identity necessarias ao Order.
/// </summary>
public interface IIdentityGrpcClient
{
    /// <summary>
    /// Verifica se o endereco informado pertence ao usuario informado.
    /// </summary>
    /// <param name="userId">Usuario autenticado.</param>
    /// <param name="addressId">Endereco de entrega escolhido.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Resultado da validacao, com os dados do endereco quando valido.</returns>
    Task<UserAddressValidationDto> ValidateUserAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
}
