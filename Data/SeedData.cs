
using Microsoft.AspNetCore.Identity;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Create roles
            string[] roles = { "Photographer", "Client" };
            foreach (string role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create default photographer
            var photographerEmail = "photographer@myphotobiz.com";
            var photographer = await userManager.FindByEmailAsync(photographerEmail);

            if (photographer == null)
            {
                photographer = new ApplicationUser
                {
                    UserName = photographerEmail,
                    Email = photographerEmail,
                    FirstName = "John",
                    LastName = "Photographer",
                    IsPhotographer = true,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(photographer, "Password123!");
                await userManager.AddToRoleAsync(photographer, "Photographer");
            }
        }
    }
}