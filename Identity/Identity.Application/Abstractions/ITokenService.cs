using Identity.Application.Models;
using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

public interface ITokenService
{
    TokenPair Generate(User user);

    string GenerateRefreshToken();
}