using Domain.RefreshTokens;
using Optional;

namespace Application.Common.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<Option<RefreshToken>> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<RefreshToken> Create(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<RefreshToken> Update(RefreshToken refreshToken, CancellationToken cancellationToken);
}