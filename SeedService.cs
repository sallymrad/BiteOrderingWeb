using BiteOrderWeb.Data;
using BiteOrderWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BiteOrderWeb.Services
{
    public class SeedService
    {
        public static async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();

            try
            {
                logger.LogInformation("Ensuring the database is created.");
                await context.Database.EnsureCreatedAsync();

                logger.LogInformation("Seeding roles.");
                await AddRoleAsync(roleManager, "SuperAdmin");
                await AddRoleAsync(roleManager, "Admin");
                await AddRoleAsync(roleManager, "Client");
                await AddRoleAsync(roleManager, "Driver");

                logger.LogInformation("Seeding or updating SuperAdmin user.");

                var newEmail = "biteordering@gmail.com";
                var newPassword = "SALLYsuperadmin123"; 

                var usersInRole = await userManager.GetUsersInRoleAsync("SuperAdmin");
                var user = usersInRole.FirstOrDefault();

                if (user != null)
                {
                    //  Update email or name if needed
                    if (user.Email != newEmail || user.FullName != "BiteOrdering")
                    {
                        user.FullName = "BiteOrdering";
                        user.Email = newEmail;
                        user.UserName = newEmail;
                        user.NormalizedEmail = newEmail.ToUpper();
                        user.NormalizedUserName = newEmail.ToUpper();

                        var updateResult = await userManager.UpdateAsync(user);
                        if (!updateResult.Succeeded)
                        {
                            logger.LogError("Failed to update SuperAdmin: {Errors}",
                                string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                        }
                        else
                        {
                            logger.LogInformation("SuperAdmin info updated.");
                            var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<Users>>();
                            await signInManager.RefreshSignInAsync(user);
                        }
                    }

                    //  Reset password
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var resetResult = await userManager.ResetPasswordAsync(user, token, newPassword);
                    if (resetResult.Succeeded)
                    {
                        logger.LogInformation("SuperAdmin password reset successfully.");
                    }
                    else
                    {
                        logger.LogError("Failed to reset password: {Errors}",
                            string.Join(", ", resetResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    //  Create SuperAdmin if none exists
                    var superAdmin = new Users
                    {
                        FullName = "BiteOrdering",
                        Email = newEmail,
                        UserName = newEmail,
                        NormalizedEmail = newEmail.ToUpper(),
                        NormalizedUserName = newEmail.ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString()
                    };

                    var result = await userManager.CreateAsync(superAdmin, newPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                        logger.LogInformation("SuperAdmin created and role assigned.");
                    }
                    else
                    {
                        logger.LogError("Failed to create SuperAdmin: {Errors}",
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        private static async Task AddRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}

