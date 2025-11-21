using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WorkClaim> LecturerClaims { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EmployeeProfile> EmployeeProfiles { get; set; }
    }
}