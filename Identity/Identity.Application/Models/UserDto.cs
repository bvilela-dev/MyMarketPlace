namespace Identity.Application.Models;

public sealed record UserDto(Guid Id, string Name, string Email, DateTime CreatedAt, IReadOnlyCollection<AddressDto> Addresses);