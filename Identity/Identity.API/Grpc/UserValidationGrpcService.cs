using Grpc.Core;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Grpc;

/// <summary>
/// Servico gRPC que valida o par usuario/endereco para os demais microsservicos.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que gRPC entre servicos e REST para o cliente?</b> gRPC usa HTTP/2 com
/// serializacao binaria (Protobuf): payload menor, conexao multiplexada e contrato
/// forte gerado em tempo de compilacao. Numa chamada interna, chamada milhares de vezes
/// por minuto, isso e ganho real. Para o navegador, REST/JSON continua sendo o caminho
/// pratico.
/// </para>
/// <para>
/// <b>Correcao aplicada:</b> a versao anterior usava <c>Guid.Parse</c> direto no valor
/// recebido. Um identificador malformado lancava <c>FormatException</c>, que o gRPC
/// traduzia num <c>StatusCode.Unknown</c> generico — e o Order, protegido por Polly,
/// ainda repetia a chamada tres vezes antes de desistir. Agora um id invalido responde
/// imediatamente <c>IsValid = false</c>.
/// </para>
/// </remarks>
/// <param name="dbContext">Contexto de leitura do banco do Identity.</param>
public sealed class UserValidationGrpcService(IdentityDbContext dbContext) : UserValidation.UserValidationBase
{
    /// <summary>
    /// Verifica se o endereco informado pertence ao usuario informado.
    /// </summary>
    /// <param name="request">Identificadores de usuario e endereco.</param>
    /// <param name="context">Contexto da chamada gRPC.</param>
    /// <returns>
    /// Resultado da validacao; quando valido, ja acompanha os campos do endereco para
    /// evitar uma segunda chamada.
    /// </returns>
    public override async Task<ValidateUserAddressResponse> ValidateUserAddress(ValidateUserAddressRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId) || !Guid.TryParse(request.AddressId, out var addressId))
        {
            return new ValidateUserAddressResponse { IsValid = false };
        }

        // Consulta direta na tabela de enderecos com as duas condicoes: resolve em um
        // unico SELECT, sem carregar o usuario nem sua colecao inteira de enderecos.
        var address = await dbContext.Addresses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == addressId && candidate.UserId == userId,
                context.CancellationToken);

        if (address is null)
        {
            return new ValidateUserAddressResponse { IsValid = false };
        }

        return new ValidateUserAddressResponse
        {
            IsValid = true,
            UserId = userId.ToString(),
            AddressId = address.Id.ToString(),
            Street = address.Street,
            Number = address.Number,
            City = address.City,
            State = address.State,
            ZipCode = address.ZipCode,
            Country = address.Country
        };
    }
}
