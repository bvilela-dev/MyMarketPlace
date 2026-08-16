using Grpc.Core;
using Identity.API.Grpc;
using Marketplace.Contracts.Grpc;
using Marketplace.SharedKernel.Exceptions;
using Order.Application.Abstractions;

namespace Order.Infrastructure.Grpc;

/// <summary>
/// Cliente gRPC do Identity usado pelo Order.
/// </summary>
/// <param name="client">Stub gRPC gerado a partir de <c>identity.proto</c>.</param>
public sealed class IdentityGrpcClient(UserValidation.UserValidationClient client) : IIdentityGrpcClient
{
    /// <inheritdoc />
    public async Task<UserAddressValidationDto> ValidateUserAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
    {
        ValidateUserAddressResponse response;

        try
        {
            response = await client.ValidateUserAddressAsync(
                new ValidateUserAddressRequest
                {
                    UserId = userId.ToString(),
                    AddressId = addressId.ToString()
                },
                cancellationToken: cancellationToken);
        }
        catch (RpcException exception) when (exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new BusinessRuleException("Servico de identidade indisponivel no momento. Tente novamente em instantes.");
        }

        if (!response.IsValid)
        {
            // Resposta "invalida" nao traz os campos do endereco; devolver o DTO com
            // strings vazias deixa claro que nada deve ser lido dele.
            return new UserAddressValidationDto(false, userId, addressId, "", "", "", "", "", "");
        }

        return new UserAddressValidationDto(
            true,
            Guid.Parse(response.UserId),
            Guid.Parse(response.AddressId),
            response.Street,
            response.Number,
            response.City,
            response.State,
            response.ZipCode,
            response.Country);
    }
}
