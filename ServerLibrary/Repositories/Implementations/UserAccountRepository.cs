using BaseLibrary.DTO;
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

public class UserAccountRepository(IOptions<JwtSection> config, AppDbContext appDbContext) : IUserAccount
{
    public async Task<GeneralResponse> CreateAsync(Register user)
    {
        if (user is null)
        {
            return new GeneralResponse(false, "Modelul este gol");
        }

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
        var checkAdminRole = await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.Admin));
        if (checkAdminRole is null)
        {
            var createAdminRole = await AddToDatabase(new SystemRole() { Name = Constants.Admin });
            await AddToDatabase(new UserRole() { RoleId = createAdminRole.Id, UserId = applicationUser.Id });
            return new GeneralResponse(true, "Cont creat cu succes");
        }

        var checkUserRole = await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.User));
        SystemRole response = new();
        if (checkUserRole is null)
        {
            response = await AddToDatabase(new SystemRole() { Name = Constants.User });
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
        if (user is null) return new LoginResponse(false, "Modelul este gol");

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
        var findUser = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.UserId == applicationUser.Id);
        if (findUser is not null)
        {
            findUser!.Token = refreshToken;
            await appDbContext.SaveChangesAsync();
        }
        else
        {
            await AddToDatabase(new RefreshTokenInfo() { Token = refreshToken, UserId = applicationUser.Id });
        }
        return new LoginResponse(true, "Autentificare reușită", jwtToken, refreshToken);
    }

    private string GenerateToken(ApplicationUser user, string role)
    {
        if (role == null)
        {
            throw new ArgumentNullException(nameof(role)!, "Câmpul rol nu poate fi ul");
        }

        if (config.Value.Key == null)
        {
            throw new ArgumentNullException(nameof(config.Value.Key)!, "Cheia JWT nu poate fi nul");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Value.Key!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var userClaims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName!),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Role, role!)
        };

        var token = new JwtSecurityToken(
            issuer: config.Value.Issuer,
            audience: config.Value.Audience,
            claims: userClaims,
            expires: DateTime.Now.AddSeconds(2),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<UserRole> FindUserRole(int userId) =>
        await appDbContext.UserRoles.FirstOrDefaultAsync(_ => _.UserId == userId);

    private async Task<SystemRole> FindRoleName(int roleId) =>
        await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Id == roleId);

    private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private async Task<ApplicationUser> FindUserByEmail(string email) =>
        await appDbContext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Email!.ToLower()!.Equals(email!.ToLower()));

    private async Task<T> AddToDatabase<T>(T model)
    {
        var result = appDbContext.Add(model!);
        await appDbContext.SaveChangesAsync();
        return (T)result.Entity;
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshToken token)
    {
        if (token is null) return new LoginResponse(false, "Modelul este gol");

        var findToken = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.Token!.Equals(token.Token));
        if (findToken is null) return new LoginResponse(false, "Refresh token necesar");

        // Get user details
        var user = await appDbContext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Id == findToken.UserId);
        if (user is null) return new LoginResponse(false, "Token-ul nu a putut fi generat deoarece utilizatorul nu a fost găsit");

        var userRole = await FindUserRole(user.Id);
        var roleName = await FindRoleName(userRole.RoleId);
        string jwtToken = GenerateToken(user, roleName.Name!);
        string refreshToken = GenerateRefreshToken();

        // Check if the refreshToken table contains the specific infos
        var updateRefreshToken = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.UserId == user.Id);
        if (updateRefreshToken is null) return new LoginResponse(false, "Token-ul nu a putut fi generat deoarece utilizatorul nu este autentificat");

        updateRefreshToken.Token = refreshToken;
        await appDbContext.SaveChangesAsync();
        return new LoginResponse(true, "Token reîmprospătat cu succes", jwtToken, refreshToken);
    }

    public async Task<List<ManageUser>> GetUsers()
    {
        var allUsers = await GetApplicationUsers();
        var allUserRoles = await UserRoles();
        var allRoles = await SystemRoles();

        if (allUsers.Count == 0 || allRoles.Count == 0) return new List<ManageUser>(); // Return empty list instead of null
        var users = new List<ManageUser>();

        foreach (var user in allUsers)
        {
            var userRole = allUserRoles.FirstOrDefault(u => u.UserId == user.Id);
            if (userRole == null) continue; // Skip if user role is null
            var roleName = allRoles.FirstOrDefault(r => r.Id == userRole.RoleId);
            if (roleName == null) continue; // Skip if role name is null

            users.Add(new ManageUser() { UserId = user.Id, Name = user.FullName!, Email = user.Email!, Role = roleName.Name! });
        }

        return users;
    }

    public async Task<GeneralResponse> UpdateUser(ManageUser user)
    {
        var getRole = (await SystemRoles()).FirstOrDefault(r => r.Name!.Equals(user.Role));
        if (getRole == null) return new GeneralResponse(false, "Rolul nu a fost găsit");

        var userRole = await appDbContext.UserRoles.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        if (userRole == null) return new GeneralResponse(false, "Rolul utilizatorului nu a fost găsit");

        userRole.RoleId = getRole.Id;
        await appDbContext.SaveChangesAsync();
        return new GeneralResponse(true, "Rolul utilizatorului a fost modificat cu succes");
    }

    public async Task<List<SystemRole>> GetRoles() => await SystemRoles();

    public async Task<GeneralResponse> DeleteUser(int id)
    {
        var getUser = await appDbContext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Id == id);
        if (getUser == null) return new GeneralResponse(false, "Utilizatorul nu a fost găsit");

        var getUserRole = await appDbContext.UserRoles.FirstOrDefaultAsync(_ => _.UserId == id);
        if (getUserRole == null) return new GeneralResponse(false, "Rolul utilizatorului nu a fost găsit");

        var getRefreshToken = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.UserId == id);
        if (getRefreshToken == null) return new GeneralResponse(false, "Refresh token-ul nu a fost găsit");

        appDbContext.ApplicationUsers.Remove(getUser);
        appDbContext.UserRoles.Remove(getUserRole);
        appDbContext.RefreshTokenInfos.Remove(getRefreshToken);
        await appDbContext.SaveChangesAsync();

        return new GeneralResponse(true, "Utilizatorul a fost șters cu succes");
    }

    // Helper methods to retrieve lists
    private async Task<List<ApplicationUser>> GetApplicationUsers() =>
        await appDbContext.ApplicationUsers.ToListAsync();

    private async Task<List<SystemRole>> SystemRoles() =>
        await appDbContext.SystemRoles.ToListAsync();

    private async Task<List<UserRole>> UserRoles() =>
        await appDbContext.UserRoles.ToListAsync();
}
