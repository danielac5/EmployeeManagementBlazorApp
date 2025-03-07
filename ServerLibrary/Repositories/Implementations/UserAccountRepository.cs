﻿using BaseLibrary.DTO;
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerLibrary.Data;
using ServerLibrary.Helpers;
using ServerLibrary.Repositories.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class UserAccountRepository : IUserAccount
{
    private readonly IOptions<JwtSection> _config;
    private readonly AppDbContext _appDbContext;

    public UserAccountRepository(IOptions<JwtSection> config, AppDbContext appDbContext)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _appDbContext = appDbContext ?? throw new ArgumentNullException(nameof(appDbContext));
    }

    public async Task<List<SystemRole>> GetRoles()
    {
        var roles = await _appDbContext.SystemRoles.ToListAsync();
        return roles;
    }


    public async Task<GeneralResponse> CreateAsync(Register user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var checkUser = await FindUserByEmail(user.Email!);
        if (checkUser != null)
        {
            return new GeneralResponse(false, "Utilizatorul este deja înregistrat");
        }

        // Save user
        var applicationUser = await AddToDatabase(new ApplicationUser()
        {
            FullName = user.Fullname,
            Email = user.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
        });

        // Check, create, assign role
        var checkAdminRole = await _appDbContext.SystemRoles.FirstOrDefaultAsync(r => r.Name!.Equals(Constants.Admin));
        if (checkAdminRole is null)
        {
            var createAdminRole = await AddToDatabase(new SystemRole() { Name = Constants.Admin });
            await AddToDatabase(new UserRole() { RoleId = createAdminRole.Id, UserId = applicationUser.Id });
            return new GeneralResponse(true, "Cont creat cu succes");
        }

        var checkUserRole = await _appDbContext.SystemRoles.FirstOrDefaultAsync(r => r.Name!.Equals(Constants.User));
        if (checkUserRole is null)
        {
            var response = await AddToDatabase(new SystemRole() { Name = Constants.User });
            await AddToDatabase(new UserRole() { RoleId = response.Id, UserId = applicationUser.Id });
        }
        else
        {
            await AddToDatabase(new UserRole() { RoleId = checkUserRole.Id, UserId = applicationUser.Id });
        }
        return new GeneralResponse(true, "Cont creat cu succes");
    }

    public async Task<LoginResponse> SignInAsync(Login user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var applicationUser = await FindUserByEmail(user.Email!);
        if (applicationUser is null) return new LoginResponse(false, "Utilizatorul nu a fost găsit");

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(user.Password, applicationUser.Password))
            return new LoginResponse(false, "Email-ul sau/și parola sunt incorecte");

        var getUserRole = await FindUserRole(applicationUser.Id);
        if (getUserRole is null) return new LoginResponse(false, "Rolul utilizatorului nu a fost găsit");

        var getRoleName = await FindRoleName(getUserRole.RoleId);
        if (getRoleName is null || string.IsNullOrEmpty(getRoleName.Name)) return new LoginResponse(false, "Rolul utilizatorului nu a fost găsit sau câmpul rol este nul");

        string jwtToken = GenerateToken(applicationUser, getRoleName.Name!);
        string refreshToken = GenerateRefreshToken();

        // Save the refreshToken to the database
        var findUser = await _appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(r => r.UserId == applicationUser.Id);
        if (findUser is not null)
        {
            findUser!.Token = refreshToken;
            await _appDbContext.SaveChangesAsync();
        }
        else
        {
            await AddToDatabase(new RefreshTokenInfo() { Token = refreshToken, UserId = applicationUser.Id });
        }
        return new LoginResponse(true, "Autentificare reușită", jwtToken, refreshToken);
    }

    private string GenerateToken(ApplicationUser user, string role)
    {
        ArgumentNullException.ThrowIfNull(role);

        var key = _config.Value.Key ?? throw new ArgumentNullException(nameof(_config.Value.Key), "Cheia JWT nu poate fi nul");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var userClaims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName!),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _config.Value.Issuer,
            audience: _config.Value.Audience,
            claims: userClaims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<UserRole?> FindUserRole(int userId) =>
        await _appDbContext.UserRoles.FirstOrDefaultAsync(r => r.UserId == userId);

    private async Task<SystemRole?> FindRoleName(int roleId) =>
        await _appDbContext.SystemRoles.FirstOrDefaultAsync(r => r.Id == roleId);

    private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private async Task<ApplicationUser?> FindUserByEmail(string email) =>
        await _appDbContext.ApplicationUsers.FirstOrDefaultAsync(u => u.Email!.ToLower() == email.ToLower());

    private async Task<T> AddToDatabase<T>(T model) where T : class
    {
        var result = _appDbContext.Add(model);
        await _appDbContext.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        var findToken = await _appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(r => r.Token == token.Token);
        if (findToken is null) return new LoginResponse(false, "Refresh token necesar");

        // Get user details
        var user = await _appDbContext.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == findToken.UserId);
        if (user is null) return new LoginResponse(false, "Token-ul nu a putut fi generat deoarece utilizatorul nu a fost găsit");

        var userRole = await FindUserRole(user.Id);
        var roleName = await FindRoleName(userRole!.RoleId);
        string jwtToken = GenerateToken(user, roleName!.Name!);
        string refreshToken = GenerateRefreshToken();

        // Update the refresh token
        var updateRefreshToken = await _appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(r => r.UserId == user.Id);
        if (updateRefreshToken is null) return new LoginResponse(false, "Token-ul nu a putut fi generat deoarece utilizatorul nu este autentificat");

        updateRefreshToken.Token = refreshToken;
        await _appDbContext.SaveChangesAsync();
        return new LoginResponse(true, "Token reîmprospătat cu succes", jwtToken, refreshToken);
    }

    public async Task<List<ManageUser>> GetUsers()
    {
        var allUsers = await GetApplicationUsers();
        var allUserRoles = await UserRoles();
        var allRoles = await SystemRoles();

        if (!allUsers.Any() || !allRoles.Any()) return new List<ManageUser>();

        var users = allUsers
            .Join(allUserRoles, user => user.Id, userRole => userRole.UserId, (user, userRole) => new { user, userRole })
            .Join(allRoles, ur => ur.userRole.RoleId, role => role.Id, (ur, role) => new ManageUser
            {
                UserId = ur.user.Id,
                Name = ur.user.FullName!,
                Email = ur.user.Email!,
                Role = role.Name!
            })
            .ToList();

        return users;
    }

    public async Task<GeneralResponse> UpdateUser(ManageUser user)
    {
        var getRole = await _appDbContext.SystemRoles.FirstOrDefaultAsync(r => r.Name!.ToLower().Equals(user.Role.ToLower()));
        if (getRole is null) return new GeneralResponse(false, "Rolul nu există în baza de date");

        var userRole = await _appDbContext.UserRoles.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        if (userRole is null) return new GeneralResponse(false, "Utilizatorul nu există în baza de date");

        userRole.RoleId = getRole.Id;
        await _appDbContext.SaveChangesAsync();
        return new GeneralResponse(true, "Datele utilizatorului au fost actualizate");
    }

    public async Task<GeneralResponse> DeleteUser(int userId)
    {
        var getUser = await _appDbContext.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (getUser is null) return new GeneralResponse(false, "Utilizatorul nu există în baza de date");

        _appDbContext.ApplicationUsers.Remove(getUser);
        await _appDbContext.SaveChangesAsync();
        return new GeneralResponse(true, "Utilizatorul a fost eliminat");
    }

    private async Task<List<ApplicationUser>> GetApplicationUsers() => await _appDbContext.ApplicationUsers.ToListAsync();

    private async Task<List<UserRole>> UserRoles() => await _appDbContext.UserRoles.ToListAsync();

    private async Task<List<SystemRole>> SystemRoles() => await _appDbContext.SystemRoles.ToListAsync();
}
