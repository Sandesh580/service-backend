using LoginCheck.Application.DTOs;
using LoginCheck.Infrastructure.Data;
using LoginCheck.Infrastructure.Identity;
using LoginCheck.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LoginCheck.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndReturnToken()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsMyVeryStrongSecretKey1234567890!",
                ["Jwt:Issuer"] = "LoginCheck",
                ["Jwt:Audience"] = "LoginCheckUsers"
            })
            .Build();

        await using var context = new ApplicationDbContext(options);
        var service = new AuthService(context, new JwtTokenService(configuration));

        var result = await service.RegisterAsync(new RegisterRequestDto
        {
            UserName = "testuser",
            Email = "test@example.com",
            Password = "Password123!"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("testuser", result.UserName);
    }
}
