namespace Marketplace.Contracts.Grpc;

/// <summary>
/// Resultado da validacao de um par usuario/endereco feita pelo Identity via gRPC.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que gRPC e nao um evento aqui?</b> Porque o Order precisa da resposta
/// <i>antes</i> de decidir se cria o pedido — e uma pergunta sincrona, nao uma
/// notificacao. A regra pratica usada no projeto: comunicacao sincrona (gRPC) para
/// consulta que bloqueia a decisao; assincrona (eventos) para propagar fatos ja
/// consumados.
/// </para>
/// <para>
/// Quando valido, a resposta ja traz os campos do endereco, evitando uma segunda
/// chamada so para busca-los.
/// </para>
/// </remarks>
/// <param name="IsValid">Indica se o usuario existe e o endereco pertence a ele.</param>
/// <param name="UserId">Identificador do usuario validado.</param>
/// <param name="AddressId">Identificador do endereco validado.</param>
/// <param name="Street">Logradouro.</param>
/// <param name="Number">Numero.</param>
/// <param name="City">Cidade.</param>
/// <param name="State">Estado ou provincia.</param>
/// <param name="ZipCode">CEP.</param>
/// <param name="Country">Pais.</param>
public sealed record UserAddressValidationDto(
    bool IsValid,
    Guid UserId,
    Guid AddressId,
    string Street,
    string Number,
    string City,
    string State,
    string ZipCode,
    string Country);
