using Microsoft.AspNetCore.Identity;
using PurrfectTodo.Data;

namespace PurrfectTodo.Services;

/// <summary>
/// Seeds the database with roles and the system (admin) account on application startup.
/// </summary>
public class DataSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<DataSeeder> logger)
{
    /// <summary>
    /// Runs all seed operations: roles, then admin account.
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedSystemAccountAsync();
        await SeedTestAccountAsync();
    }

    private async Task SeedRolesAsync()
    {
        string[] roles = [RoleNames.Admin, RoleNames.User];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    logger.LogError(
                        "Failed to create role {Role}: {Errors}",
                        role,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedSystemAccountAsync()
    {
        const string email = "admin@purrfecttodo.local";
        var password = configuration["SystemAccount:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            password = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "A1!";
            logger.LogWarning(
                "SystemAccount:Password not configured. Generated random password for {Email}.",
                email);
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin"
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                logger.LogError(
                    "Failed to create system account: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }
        else
        {
            // Rotate password on every startup so the configured secret stays in sync.
            var removeResult = await userManager.RemovePasswordAsync(user);
            if (removeResult.Succeeded)
                await userManager.AddPasswordAsync(user, password);
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
            await userManager.AddToRoleAsync(user, RoleNames.Admin);
    }

    private async Task SeedTestAccountAsync()
    {
        const string email = "test@purrfecttodo.local";
        const string password = "Test1234!";

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "User"
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                logger.LogError(
                    "Failed to create test account: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }
            logger.LogInformation("Test account created: {Email}", email);
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.User))
            await userManager.AddToRoleAsync(user, RoleNames.User);
    }
}
