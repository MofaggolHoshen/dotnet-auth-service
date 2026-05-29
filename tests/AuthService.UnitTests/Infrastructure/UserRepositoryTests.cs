using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AuthService.UnitTests.Infrastructure;

public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _sut;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _sut = new UserRepository(_context);
    }

    // ── AddAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsUserToDatabase()
    {
        var user = MakeUser("add@example.com");
        await _sut.AddAsync(user);

        var stored = await _context.Users.FindAsync(user.Id);
        stored.Should().NotBeNull();
        stored!.Email.Should().Be("add@example.com");
    }

    // ── GetByIdAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsUser()
    {
        var user = await SeedUser("byid@example.com");

        var result = await _sut.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        (await _sut.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    // ── GetByEmailAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEmailAsync_WhenExists_ReturnsUser()
    {
        await SeedUser("byemail@example.com");

        var result = await _sut.GetByEmailAsync("byemail@example.com");

        result.Should().NotBeNull();
        result!.Email.Should().Be("byemail@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_WhenNotExists_ReturnsNull()
    {
        (await _sut.GetByEmailAsync("ghost@example.com")).Should().BeNull();
    }

    // ── ExistsByEmailAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsByEmailAsync_WhenExists_ReturnsTrue()
    {
        await SeedUser("exists@example.com");
        (await _sut.ExistsByEmailAsync("exists@example.com")).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByEmailAsync_WhenNotExists_ReturnsFalse()
    {
        (await _sut.ExistsByEmailAsync("nobody@example.com")).Should().BeFalse();
    }

    // ── GetByEmailVerificationTokenAsync ──────────────────────────────────────

    [Fact]
    public async Task GetByEmailVerificationTokenAsync_WhenTokenMatches_ReturnsUser()
    {
        var user = MakeUser("verify@example.com");
        user.EmailVerificationToken = "verify-token-abc";
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByEmailVerificationTokenAsync("verify-token-abc");

        result.Should().NotBeNull();
        result!.Email.Should().Be("verify@example.com");
    }

    [Fact]
    public async Task GetByEmailVerificationTokenAsync_WhenTokenNotFound_ReturnsNull()
    {
        (await _sut.GetByEmailVerificationTokenAsync("nonexistent")).Should().BeNull();
    }

    // ── GetByPasswordResetTokenAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetByPasswordResetTokenAsync_WhenTokenMatches_ReturnsUser()
    {
        var user = MakeUser("reset@example.com");
        user.PasswordResetToken = "reset-token-xyz";
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByPasswordResetTokenAsync("reset-token-xyz");

        result.Should().NotBeNull();
        result!.Email.Should().Be("reset@example.com");
    }

    [Fact]
    public async Task GetByPasswordResetTokenAsync_WhenTokenNotFound_ReturnsNull()
    {
        (await _sut.GetByPasswordResetTokenAsync("nonexistent")).Should().BeNull();
    }

    // ── GetByRefreshTokenAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByRefreshTokenAsync_WhenActiveTokenExists_ReturnsUser()
    {
        var user = await SeedUser("refreshuser@example.com");
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "active-refresh-token",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetByRefreshTokenAsync("active-refresh-token");

        result.Should().NotBeNull();
        result!.Email.Should().Be("refreshuser@example.com");
    }

    [Fact]
    public async Task GetByRefreshTokenAsync_WhenTokenRevoked_ReturnsNull()
    {
        var user = await SeedUser("revokeduser@example.com");
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "revoked-token",
            IsRevoked = true,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetByRefreshTokenAsync("revoked-token");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByRefreshTokenAsync_WhenTokenNotFound_ReturnsNull()
    {
        (await _sut.GetByRefreshTokenAsync("nonexistent")).Should().BeNull();
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsChangesToDatabase()
    {
        var user = await SeedUser("update@example.com");

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        await _sut.UpdateAsync(user);

        var stored = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        stored.IsEmailVerified.Should().BeTrue();
        stored.EmailVerificationToken.Should().BeNull();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static User MakeUser(string email) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
        IsEmailVerified = false,
        Status = UserStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private async Task<User> SeedUser(string email)
    {
        var user = MakeUser(email);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return user;
    }

    public void Dispose() => _context.Dispose();
}
