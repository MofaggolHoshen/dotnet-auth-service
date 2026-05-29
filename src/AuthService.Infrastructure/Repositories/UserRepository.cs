using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByEmailVerificationTokenAsync(string token)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
    }

    public async Task<User?> GetByPasswordResetTokenAsync(string token)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

        return token?.User;
    }
}
