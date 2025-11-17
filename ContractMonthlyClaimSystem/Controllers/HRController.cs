using Microsoft.AspNetCore.Mvc;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HRController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== HR DASHBOARD =====
        public async Task<IActionResult> Dashboard()
        {
            // Check authorization
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "HR")
            {
                TempData["ErrorMessage"] = "Access denied. HR access only.";
                return RedirectToAction("Index", "Home");
            }

            // Get statistics
            var totalClaims = await _context.Claims.CountAsync();
            var pendingClaims = await _context.Claims.CountAsync(c => c.Status == "Pending");
            var approvedClaims = await _context.Claims.CountAsync(c => c.Status == "Approved");
            var rejectedClaims = await _context.Claims.CountAsync(c => c.Status == "Rejected");

            var totalAmount = await _context.Claims.SumAsync(c => c.TotalAmount);
            var approvedAmount = await _context.Claims
                .Where(c => c.Status == "Approved")
                .SumAsync(c => c.TotalAmount);

            ViewBag.TotalClaims = totalClaims;
            ViewBag.PendingClaims = pendingClaims;
            ViewBag.ApprovedClaims = approvedClaims;
            ViewBag.RejectedClaims = rejectedClaims;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.ApprovedAmount = approvedAmount;

            // Recent claims
            var recentClaims = await _context.Claims
                .OrderByDescending(c => c.DateSubmitted)
                .Take(5)
                .ToListAsync();

            return View(recentClaims);
        }

        // ===== USER MANAGEMENT =====
        public IActionResult ManageUsers()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "HR")
            {
                TempData["ErrorMessage"] = "Access denied. HR access only.";
                return RedirectToAction("Index", "Home");
            }

            // In a real system, you'd have a Users table
            // For now, we'll use a mock list
            var users = GetMockUsers();
            return View(users);
        }

        // GET: Add User
        public IActionResult AddUser()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "HR")
            {
                TempData["ErrorMessage"] = "Access denied. HR access only.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Add User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(string username, string fullName, string email, string role, string password)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "HR")
            {
                TempData["ErrorMessage"] = "Access denied. HR access only.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(role))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return View();
            }

            // In a real system, save to database
            // For demo, just show success
            TempData["SuccessMessage"] = $"User '{fullName}' added successfully with role '{role}'!";
            return RedirectToAction(nameof(ManageUsers));
        }

        // GET: Edit User
        public IActionResult EditUser(int id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "HR")
            {
                TempData["ErrorMessage"] = "Access denied. HR access only.";
                return RedirectToAction("Index", "Home");
            }

            var users = GetMockUsers();
            var user = users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            return View(user);
        }

        // POST: Edit User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(int id, string fullName, string email, string role, bool isActive)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "HR")
            {
                TempData["ErrorMessage"] = "Access denied. HR access only.";
                return RedirectToAction("Index", "Home");
            }

            // In a real system, update database
            TempData["SuccessMessage"] = $"User information updated successfully!";
            return RedirectToAction(nameof(ManageUsers));
        }

        // ===== REPORTS =====
        public async Task<IActionResult> Reports()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "HR")
            {
                TempData["ErrorMessage"] = "Access denied. HR access only.";
                return RedirectToAction("Index", "Home");
            }

            var claims = await _context.Claims
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();

            return View(claims);
        }

        // Generate CSV Report
        public async Task<IActionResult> GenerateCSVReport()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "HR")
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

        // Generate Summary Report
        public async Task<IActionResult> GenerateSummaryReport()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "HR")
            {
                return Unauthorized();
            }

            var claims = await _context.Claims.ToListAsync();

            var summary = new
            {
                TotalClaims = claims.Count,
                PendingClaims = claims.Count(c => c.Status == "Pending"),
                ApprovedClaims = claims.Count(c => c.Status == "Approved"),
                RejectedClaims = claims.Count(c => c.Status == "Rejected"),
                TotalAmount = claims.Sum(c => c.TotalAmount),
                ApprovedAmount = claims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount),
                PendingAmount = claims.Where(c => c.Status == "Pending").Sum(c => c.TotalAmount),
                AverageClaimAmount = claims.Any() ? claims.Average(c => c.TotalAmount) : 0
            };

            var csv = new StringBuilder();
            csv.AppendLine("Summary Report - Contract Monthly Claim System");
            csv.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            csv.AppendLine("");
            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Total Claims,{summary.TotalClaims}");
            csv.AppendLine($"Pending Claims,{summary.PendingClaims}");
            csv.AppendLine($"Approved Claims,{summary.ApprovedClaims}");
            csv.AppendLine($"Rejected Claims,{summary.RejectedClaims}");
            csv.AppendLine($"Total Amount,R {summary.TotalAmount:N2}");
            csv.AppendLine($"Approved Amount,R {summary.ApprovedAmount:N2}");
            csv.AppendLine($"Pending Amount,R {summary.PendingAmount:N2}");
            csv.AppendLine($"Average Claim Amount,R {summary.AverageClaimAmount:N2}");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"SummaryReport_{DateTime.Now:yyyyMMdd}.csv");
        }

        // Helper: Mock Users (In real system, use database)
        private List<UserModel> GetMockUsers()
        {
            return new List<UserModel>
            {
                new UserModel { Id = 1, Username = "lecturer", FullName = "John Doe", Email = "john@cmcs.com", Role = "Lecturer", IsActive = true },
                new UserModel { Id = 2, Username = "coordinator", FullName = "Sarah Smith", Email = "sarah@cmcs.com", Role = "Coordinator", IsActive = true },
                new UserModel { Id = 3, Username = "manager", FullName = "David Johnson", Email = "david@cmcs.com", Role = "Manager", IsActive = true },
                new UserModel { Id = 4, Username = "hr", FullName = "Emma Wilson", Email = "emma@cmcs.com", Role = "HR", IsActive = true }
            };
        }
    }

    // Mock User Model (In real system, add to Models folder)
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