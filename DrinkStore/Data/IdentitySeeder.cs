using Microsoft.AspNetCore.Identity;

namespace DrinkStore.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAsync(
            IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { "Customer", "ShiftManager" };

            foreach (var role in roles)
            {
                if (await roleManager.RoleExistsAsync(role))
                    continue;

                var result = await roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Không thể tạo role: {role}");
                }
            }
        }
    }
}
