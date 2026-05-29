using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AuthService.UnitTests.Infrastructure;

public class TokenServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TokenService _sut;
    private const string SecretKey = "test-secret-key-must-be-at-least-32-chars-long!";

    public TokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = SecretKey,
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "15"
            })
            .Build();

        _sut = new TokenService(config, _context);
    }

    // ── GenerateAccessToken ────────────────────────────────────────────────────

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        var token = _sut.GenerateAccessToken(Guid.NewGuid(), "user@example.com");
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateAccessToken_IsValidJwt()
    {
        var token = _sut.GenerateAccessToken(Guid.NewGuid(), "user@example.com");

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
        handler.ReadJwtToken(token).Issuer.Should().Be("TestIssuer");
    }

    [Fact]
    public void GenerateAccessToken_ContainsUserIdAndEmailClaims()
    {
        var userId = Guid.NewGuid();
        const string email = "user@example.com";

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_sut.GenerateAccessToken(userId, email));

        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == userId.ToString());
        jwt.Claims.Should().Contain(c => c.Value == email);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresIn15Minutes()
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_sut.GenerateAccessToken(Guid.NewGuid(), "x@y.com"));
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(10));
    }

    // ── GenerateRefreshToken ───────────────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_ReturnsValidBase64()
    {
        var token = _sut.GenerateRefreshToken();
        token.Should().NotBeNullOrEmpty();
        var act = () => Convert.FromBase64String(token);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_Returns64Bytes()
    {
        var bytes = Convert.FromBase64String(_sut.GenerateRefreshToken());
        bytes.Length.Should().Be(64);
    }

    [Fact]
    public void GenerateRefreshToken_IsUniqueEachCall()
    {
        _sut.GenerateRefreshToken().Should().NotBe(_sut.GenerateRefreshToken());
    }

    // ── ValidateToken ──────────────────────────────────────────────────────────

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsPrincipalWithCorrectSub()
    {
        var userId = Guid.NewGuid();
        var token = _sut.GenerateAccessToken(userId, "user@example.com");

        var principal = _sut.ValidateToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirst("sub")?.Value.Should().Be(userId.ToString());
    }

    [Fact]
    public void ValidateToken_WithGarbageString_ReturnsNull()
    {
        _sut.ValidateToken("not.a.jwt").Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithTamperedSignature_ReturnsNull()
    {
        var token = _sut.GenerateAccessToken(Guid.NewGuid(), "user@example.com");
        var tampered = token[..^5] + "XXXXX";
        _sut.ValidateToken(tampered).Should().BeNull();
    }

    // ── StoreRefreshTokenAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task StoreRefreshTokenAsync_PersistsTokenToDatabase()
    {
        var userId = Guid.NewGuid();
        await _sut.StoreRefreshTokenAsync(userId, "stored-token");

        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == "stored-token");
        stored.Should().NotBeNull();
        stored!.UserId.Should().Be(userId);
        stored.IsRevoked.Should().BeFalse();
        stored.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    // ── RevokeTokenAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeTokenAsync_WhenTokenExists_RevokesAndReturnsTrue()
    {
        var userId = Guid.NewGuid();
        _context.RefreshTokens.Add(MakeRefreshToken(userId, "token123"));
        await _context.SaveChangesAsync();

        var result = await _sut.RevokeTokenAsync(userId, "token123");

        result.Should().BeTrue();
        var stored = await _context.RefreshTokens.FirstAsync(t => t.Token == "token123");
        stored.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeTokenAsync_WhenTokenNotFound_ReturnsFalse()
    {
        (await _sut.RevokeTokenAsync(Guid.NewGuid(), "nonexistent")).Should().BeFalse();
    }

    // ── IsTokenRevokedAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task IsTokenRevokedAsync_WhenTokenRevoked_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        _context.RefreshTokens.Add(MakeRefreshToken(userId, "revokedtoken", isRevoked: true));
        await _context.SaveChangesAsync();

        (await _sut.IsTokenRevokedAsync(userId, "revokedtoken")).Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenRevokedAsync_WhenTokenActive_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        _context.RefreshTokens.Add(MakeRefreshToken(userId, "activetoken", isRevoked: false));
        await _context.SaveChangesAsync();

        (await _sut.IsTokenRevokedAsync(userId, "activetoken")).Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenRevokedAsync_WhenTokenDoesNotExist_ReturnsTrue()
    {
        (await _sut.IsTokenRevokedAsync(Guid.NewGuid(), "nonexistent")).Should().BeTrue();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static RefreshToken MakeRefreshToken(Guid userId, string token, bool isRevoked = false) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Token = token,
        IsRevoked = isRevoked,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow
    };

    public void Dispose() => _context.Dispose();
}
