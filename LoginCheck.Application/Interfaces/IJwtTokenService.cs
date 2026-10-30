namespace LoginCheck.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(string userName, string email, int userId, string role);
}
