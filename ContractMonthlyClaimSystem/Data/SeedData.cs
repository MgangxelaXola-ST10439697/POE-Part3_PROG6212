using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContractMonthlyClaimSystem.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = services.GetRequiredService<UserManager<IdentityUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();

            // Apply any pending migrations
            await db.Database.MigrateAsync();

            // Seed roles for academic claim system
            await EnsureRolesExistAsync(roleMgr, new[]
            {
                "Admin",
                "Lecturer",
                "ProgrammeCoordinator",
                "AcademicManager"
            });

            // Seed predefined users for the academic system
            var userDefinitions = new List<UserSeedInfo>
            {
                new("admin@cmcs.com", "Admin@123!", "Admin", "System", "Administrator", "Administration", 0),
                new("coordinator@cmcs.com", "Coordinator@123!", "ProgrammeCoordinator", "Sarah", "Johnson", "Computer Science", 0),
                new("manager@cmcs.com", "Manager@123!", "AcademicManager", "Michael", "Williams", "Academic Affairs", 0),
                new("lecturer@cmcs.com", "Lecturer@123!", "Lecturer", "David", "Smith", "Information Technology", 350)
            };

            foreach (var userInfo in userDefinitions)
            {
                await CreateSeedUserAsync(userMgr, db, userInfo);
            }

            await db.SaveChangesAsync();
        }

        private static async Task EnsureRolesExistAsync(RoleManager<IdentityRole> roleManager, IEnumerable<string> roleNames)
        {
            foreach (var role in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task CreateSeedUserAsync(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext db,
            UserSeedInfo seedInfo)
        {
            var existingUser = await userManager.FindByEmailAsync(seedInfo.Email);
            if (existingUser != null) return;

            var newUser = new IdentityUser
            {
                Email = seedInfo.Email,
                UserName = seedInfo.Email,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(newUser, seedInfo.Password);
            await userManager.AddToRoleAsync(newUser, seedInfo.RoleName);

            db.EmployeeProfiles.Add(new EmployeeProfile
            {
                UserId = newUser.Id,
                Name = seedInfo.FirstName,
                Surname = seedInfo.LastName,
                Department = seedInfo.Department,
                DefaultRatePerJob = seedInfo.Rate,
                RoleName = seedInfo.RoleName
            });

            await db.SaveChangesAsync();
        }

        private record UserSeedInfo(
            string Email,
            string Password,
            string RoleName,
            string FirstName,
            string LastName,
            string Department,
            decimal Rate);
    }
}