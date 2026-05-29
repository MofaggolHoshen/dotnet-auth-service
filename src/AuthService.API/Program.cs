using System.Text;
using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using AuthService.Application.Validators;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Email;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Add database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not configured");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("AuthService.Infrastructure"))
);

// Add infrastructure services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Add email sender implementations
builder.Services.AddScoped<SmtpEmailSender>();
builder.Services.AddScoped<SendGridEmailSender>();
builder.Services.AddScoped<StubEmailSender>();

// Register IEmailSender using factory pattern
builder.Services.AddScoped<IEmailSender>(provider => EmailSenderFactory.Create(provider));

// Add application services
builder.Services.AddScoped<IAuthService, AuthService.Application.Services.AuthService>();

// Add validators
builder.Services.AddValidatorsFromAssembly(typeof(RegisterRequestValidator).Assembly);

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

if (secretKey.Length < 32)
{
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");
}

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "AuthService",
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"] ?? "AuthServiceClients",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Auth Service API",
        Version = "v1",
        Description = "JWT Authentication REST API with registration, login, email verification, and password reset."
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service API v1"));
}

app.UseMiddleware<AuthService.API.Middleware.ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

