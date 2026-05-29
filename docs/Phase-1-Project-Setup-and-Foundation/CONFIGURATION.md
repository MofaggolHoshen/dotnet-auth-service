# Phase 1: Configuration Reference

**Last Updated:** 2026-05-29

---

## NuGet Packages Installation Log

### Installation Commands Executed

```powershell
# Infrastructure Packages
dotnet add src\AuthService.Infrastructure\AuthService.Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add src\AuthService.Infrastructure\AuthService.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add src\AuthService.Infrastructure\AuthService.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
dotnet add src\AuthService.Infrastructure\AuthService.Infrastructure.csproj package System.IdentityModel.Tokens.Jwt --version 7.6.2
dotnet add src\AuthService.Infrastructure\AuthService.Infrastructure.csproj package Microsoft.IdentityModel.Tokens --version 7.6.2
dotnet add src\AuthService.Infrastructure\AuthService.Infrastructure.csproj package MailKit --version 4.7.1
dotnet add src\AuthService.Infrastructure\AuthService.Infrastructure.csproj package SendGrid --version 9.29.3
dotnet add src\AuthService.Infrastructure\AuthService.Infrastructure.csproj package BCrypt.Net-Next --version 4.0.3

# Application Packages
dotnet add src\AuthService.Application\AuthService.Application.csproj package FluentValidation --version 11.9.2

# API Packages
dotnet add src\AuthService.API\AuthService.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0

# UnitTests Packages
dotnet add tests\AuthService.UnitTests\AuthService.UnitTests.csproj package Moq --version 4.20.70
dotnet add tests\AuthService.UnitTests\AuthService.UnitTests.csproj package FluentAssertions --version 6.12.0

# IntegrationTests Packages
dotnet add tests\AuthService.IntegrationTests\AuthService.IntegrationTests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
dotnet add tests\AuthService.IntegrationTests\AuthService.IntegrationTests.csproj package Moq --version 4.20.70
dotnet add tests\AuthService.IntegrationTests\AuthService.IntegrationTests.csproj package FluentAssertions --version 6.12.0
```

---

## Project File Structure

### AuthService.Domain.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```
**Dependencies:** None (core framework only)

### AuthService.Application.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\AuthService.Domain\AuthService.Domain.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="11.9.2" />
  </ItemGroup>
</Project>
```
**Dependencies:** Domain, FluentValidation

### AuthService.Infrastructure.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\AuthService.Domain\AuthService.Domain.csproj" />
    <ProjectReference Include="..\AuthService.Application\AuthService.Application.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.6.2" />
    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="7.6.2" />
    <PackageReference Include="MailKit" Version="4.7.1" />
    <PackageReference Include="SendGrid" Version="9.29.3" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>
</Project>
```
**Dependencies:** Domain, Application, EF Core, JWT, Email, Crypto

### AuthService.API.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\AuthService.Domain\AuthService.Domain.csproj" />
    <ProjectReference Include="..\AuthService.Application\AuthService.Application.csproj" />
    <ProjectReference Include="..\AuthService.Infrastructure\AuthService.Infrastructure.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
  </ItemGroup>
</Project>
```
**Dependencies:** Domain, Application, Infrastructure, JWT Bearer

### AuthService.UnitTests.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\..\src\AuthService.Domain\AuthService.Domain.csproj" />
    <ProjectReference Include="..\..\src\AuthService.Application\AuthService.Application.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.6.6" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>
</Project>
```
**Dependencies:** Domain, Application, xUnit, Moq, FluentAssertions

### AuthService.IntegrationTests.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\..\src\AuthService.Domain\AuthService.Domain.csproj" />
    <ProjectReference Include="..\..\src\AuthService.Application\AuthService.Application.csproj" />
    <ProjectReference Include="..\..\src\AuthService.API\AuthService.API.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.6.6" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>
</Project>
```
**Dependencies:** Domain, Application, API, xUnit, WebApplicationFactory, Moq, FluentAssertions

---

## Dependency Graph

```
AuthService.Domain (no dependencies)
    ↑
    ├─ AuthService.Application
    │   ↑
    │   ├─ AuthService.Infrastructure
    │   │   ├─ Microsoft.EntityFrameworkCore
    │   │   ├─ Microsoft.EntityFrameworkCore.SqlServer
    │   │   ├─ System.IdentityModel.Tokens.Jwt
    │   │   ├─ Microsoft.IdentityModel.Tokens
    │   │   ├─ MailKit
    │   │   ├─ SendGrid
    │   │   └─ BCrypt.Net-Next
    │   │
    │   ├─ AuthService.API
    │   │   └─ Microsoft.AspNetCore.Authentication.JwtBearer
    │   │
    │   ├─ AuthService.UnitTests
    │   │   ├─ Moq
    │   │   └─ FluentAssertions
    │   │
    │   └─ AuthService.IntegrationTests
    │       ├─ Microsoft.AspNetCore.Mvc.Testing
    │       ├─ Moq
    │       └─ FluentAssertions
    │
    └─ FluentValidation
```

---

## Package Purposes

### Database & ORM
- **Microsoft.EntityFrameworkCore** - Object-relational mapper
- **Microsoft.EntityFrameworkCore.SqlServer** - SQL Server integration
- **Microsoft.EntityFrameworkCore.Tools** - CLI tools (migrations, scaffolding)

### Authentication & Security
- **System.IdentityModel.Tokens.Jwt** - JWT token creation/validation
- **Microsoft.IdentityModel.Tokens** - Token signing/verification
- **Microsoft.AspNetCore.Authentication.JwtBearer** - JWT middleware
- **BCrypt.Net-Next** - Password hashing

### Email
- **MailKit** - SMTP email client
- **SendGrid** - SendGrid API integration

### Validation
- **FluentValidation** - Fluent validation library

### Testing
- **xunit** - Test framework (included via template)
- **Moq** - Mocking library
- **FluentAssertions** - Fluent assertions
- **Microsoft.AspNetCore.Mvc.Testing** - WebApplicationFactory

---

## Build Commands

```bash
# Build entire solution
dotnet build AuthService.sln

# Build specific project
dotnet build src/AuthService.API/AuthService.API.csproj

# Clean build
dotnet clean AuthService.sln && dotnet build AuthService.sln

# Restore packages
dotnet restore AuthService.sln

# List projects
dotnet sln AuthService.sln list
```

---

## Test Commands

```bash
# Run all tests
dotnet test AuthService.sln

# Run unit tests only
dotnet test tests/AuthService.UnitTests/AuthService.UnitTests.csproj

# Run integration tests only
dotnet test tests/AuthService.IntegrationTests/AuthService.IntegrationTests.csproj

# Run with verbose output
dotnet test AuthService.sln --verbosity normal

# Run specific test
dotnet test --filter "TestName" AuthService.sln
```

---

## Known Issues & Notes

### ⚠️ Security Advisory
- **MailKit 4.7.1** has a known moderate severity vulnerability
  - Link: https://github.com/advisories/GHSA-9j88-vvj5-vhgr
  - Impact: Low in typical scenarios
  - Recommended Action: Monitor for security patches

### ✅ All Other Packages
- No high-severity vulnerabilities
- All packages are stable and well-maintained
- Compatible with .NET 8.0

---

## Environment Requirements

- **.NET SDK:** 8.0.0 or higher
- **SQL Server:** 2019 SP1 or higher, or SQL Server LocalDB
- **Visual Studio:** 2022 17.0+ (optional, can use VS Code)
- **PowerShell:** 5.1+ (Windows) or PowerShell Core

---

## Verification Steps

```bash
# 1. Check .NET version
dotnet --version

# 2. Restore packages
cd C:\Users\mofag\Source\repos\dotnet-auth-service
dotnet restore AuthService.sln

# 3. Build solution
dotnet build AuthService.sln

# 4. Verify no errors
# (should show "Build succeeded.")

# 5. List all projects
dotnet sln AuthService.sln list
```

Expected output for step 5:
```
Project(s)
-----------
src\AuthService.Domain\AuthService.Domain.csproj
src\AuthService.Application\AuthService.Application.csproj
src\AuthService.Infrastructure\AuthService.Infrastructure.csproj
src\AuthService.API\AuthService.API.csproj
tests\AuthService.UnitTests\AuthService.UnitTests.csproj
tests\AuthService.IntegrationTests\AuthService.IntegrationTests.csproj
```

---

## Next Phase Preparation

Before starting Phase 2 (Domain Entities), ensure:
- ✅ Solution builds without errors
- ✅ All projects are visible and accessible
- ✅ No circular dependencies
- ✅ Project structure matches documentation
