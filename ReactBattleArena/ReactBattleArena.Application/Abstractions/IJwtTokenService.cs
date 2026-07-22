using ReactBattleArena.Domain.Users;

namespace ReactBattleArena.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateToken(User user);
}