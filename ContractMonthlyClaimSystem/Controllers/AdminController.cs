using Microsoft.AspNetCore.Mvc;
using ContractMonthlyClaimSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Access denied. Administrators only.";
                return RedirectToAction("Index", "Home");
            }

            var totalClaims = await _context.Claims.CountAsync();
            var pendingClaims = await _context.Claims.CountAsync(c => c.Status.Contains("Pending"));
            var approvedClaims = await _context.Claims.CountAsync(c => c.Status == "Manager Approved");
            var rejectedClaims = await _context.Claims.CountAsync(c => c.Status.Contains("Rejected"));

            var totalAmount = await _context.Claims.SumAsync(c => c.TotalAmount);
            var approvedAmount = await _context.Claims
                .Where(c => c.Status == "Manager Approved")
                .SumAsync(c => c.TotalAmount);

            ViewBag.TotalClaims = totalClaims;
            ViewBag.PendingClaims = pendingClaims;
            ViewBag.ApprovedClaims = approvedClaims;
            ViewBag.RejectedClaims = rejectedClaims;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.ApprovedAmount = approvedAmount;

            var recentClaims = await _context.Claims
                .OrderByDescending(c => c.DateSubmitted)
                .Take(10)
                .ToListAsync();

            return View(recentClaims);
        }

        public IActionResult UserManagement()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            var users = GetMockUsers();
            return View(users);
        }

        public async Task<IActionResult> Reports()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            var claims = await _context.Claims
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();

            return View(claims);
        }

        public async Task<IActionResult> GenerateCSVReport()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return Unauthorized();
            }

            var claims = await _context.Claims
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Claim ID,Lecturer Name,Email,Hours Worked,Hourly Rate,Total Amount,Status,Date Submitted");

            foreach (var claim in claims)
            {
                csv.AppendLine($"{claim.Id},{claim.LecturerName},{claim.LecturerEmail},{claim.HoursWorked},{claim.HourlyRate},{claim.TotalAmount},{claim.Status},{claim.DateSubmitted:yyyy-MM-dd HH:mm}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"ClaimsReport_{DateTime.Now:yyyyMMdd}.csv");
        }

        private List<UserModel> GetMockUsers()
        {
            return new List<UserModel>
            {
                new UserModel { Id = 1, Username = "lecturer", FullName = "Dr. John Doe", Email = "john@university.ac.za", Role = "Lecturer", IsActive = true },
                new UserModel { Id = 2, Username = "coordinator", FullName = "Prof. Sarah Smith", Email = "sarah@university.ac.za", Role = "Programme Coordinator", IsActive = true },
                new UserModel { Id = 3, Username = "manager", FullName = "Prof. David Johnson", Email = "david@university.ac.za", Role = "Academic Manager", IsActive = true },
                new UserModel { Id = 4, Username = "admin", FullName = "Emma Wilson", Email = "emma@university.ac.za", Role = "Admin", IsActive = true }
            };
        }
    }

    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }
}