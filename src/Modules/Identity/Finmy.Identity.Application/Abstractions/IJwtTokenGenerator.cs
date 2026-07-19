namespace Finmy.Identity.Application.Abstractions;

public record AccessTokenOutcome(string Value, DateTime ExpiresAt);

public interface IJwtTokenGenerator
{
    AccessTokenOutcome GenerateToken(string userId, string email, IEnumerable<string> roles);
}
