using Microsoft.AspNetCore.Mvc;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ClaimsController> _logger;

        private readonly string[] _allowedExtensions = { ".pdf", ".docx", ".xlsx" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public ClaimsController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<ClaimsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ===== LECTURER ACTIONS =====

        public IActionResult Submit()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "Lecturer")
                {
                    TempData["ErrorMessage"] = "Access denied. Lecturers only.";
                    return RedirectToAction("Index", "Home");
                }
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Submit view");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Claim claim, IFormFile upload)
        {
            _logger.LogInformation("Submit method called for lecturer: {LecturerName}", claim?.LecturerName);

            try
            {
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Invalid claim data received.";
                    return View();
                }

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please correct the errors in the form.";
                    return View(claim);
                }

                var newClaim = new Claim
                {
                    LecturerName = claim.LecturerName?.Trim(),
                    LecturerEmail = claim.LecturerEmail?.Trim().ToLower(),
                    HoursWorked = claim.HoursWorked,
                    HourlyRate = claim.HourlyRate,
                    Notes = claim.Notes?.Trim(),
                    Status = "Pending",
                    DateSubmitted = DateTime.Now
                };

                // Handle file upload
                if (upload != null && upload.Length > 0)
                {
                    var fileExtension = Path.GetExtension(upload.FileName).ToLower();
                    if (!_allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = $"Invalid file type. Only {string.Join(", ", _allowedExtensions)} allowed.";
                        return View(claim);
                    }

                    if (upload.Length > _maxFileSize)
                    {
                        TempData["ErrorMessage"] = "File size cannot exceed 5MB.";
                        return View(claim);
                    }

                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await upload.CopyToAsync(stream);
                    }

                    newClaim.FileName = upload.FileName;
                    newClaim.FilePath = $"/uploads/{uniqueFileName}";
                }

                _context.Claims.Add(newClaim);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Claim submitted successfully! Claim ID: {newClaim.Id}. Total: R{newClaim.TotalAmount:N2}";
                return RedirectToAction(nameof(MyClaims));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting claim");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return View(claim);
            }
        }

        public async Task<IActionResult> MyClaims()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "Lecturer")
                {
                    TempData["ErrorMessage"] = "Access denied. Lecturers only.";
                    return RedirectToAction("Index", "Home");
                }

                var claims = await _context.Claims
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lecturer claims");
                return View(new List<Claim>());
            }
        }

        public async Task<IActionResult> ClaimHistory()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "Lecturer")
                {
                    TempData["ErrorMessage"] = "Access denied.";
                    return RedirectToAction("Index", "Home");
                }

                var claims = await _context.Claims
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                ViewBag.TotalClaims = claims.Count;
                ViewBag.PendingClaims = claims.Count(c => c.Status == "Pending");
                ViewBag.ApprovedClaims = claims.Count(c => c.Status == "Approved");
                ViewBag.RejectedClaims = claims.Count(c => c.Status == "Rejected");

                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading claim history");
                return View(new List<Claim>());
            }
        }

        // ===== PROGRAMME COORDINATOR ACTIONS =====

        public async Task<IActionResult> PendingReview()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "ProgrammeCoordinator")
                {
                    TempData["ErrorMessage"] = "Access denied. Programme Coordinators only.";
                    return RedirectToAction("Index", "Home");
                }

                var claims = await _context.Claims
                    .Where(c => c.Status == "Pending")
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending claims");
                return View(new List<Claim>());
            }
        }

        public async Task<IActionResult> ReviewedClaims()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "ProgrammeCoordinator")
                {
                    TempData["ErrorMessage"] = "Access denied.";
                    return RedirectToAction("Index", "Home");
                }

                var claims = await _context.Claims
                    .Where(c => c.Status == "Coordinator Approved" || c.Status == "Coordinator Rejected")
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reviewed claims");
                return View(new List<Claim>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoordinatorApprove(int id)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(PendingReview));
                }

                claim.Status = "Coordinator Approved";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Claim #{id} approved and forwarded to Academic Manager.";
                return RedirectToAction(nameof(PendingReview));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving claim");
                TempData["ErrorMessage"] = "Error approving claim.";
                return RedirectToAction(nameof(PendingReview));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoordinatorReject(int id, string reason)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(PendingReview));
                }

                claim.Status = "Coordinator Rejected";
                claim.Notes = $"Rejection Reason: {reason}";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Claim #{id} rejected.";
                return RedirectToAction(nameof(PendingReview));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim");
                TempData["ErrorMessage"] = "Error rejecting claim.";
                return RedirectToAction(nameof(PendingReview));
            }
        }

        // ===== ACADEMIC MANAGER ACTIONS =====

        public async Task<IActionResult> ApprovalQueue()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "AcademicManager")
                {
                    TempData["ErrorMessage"] = "Access denied. Academic Managers only.";
                    return RedirectToAction("Index", "Home");
                }

                var claims = await _context.Claims
                    .Where(c => c.Status == "Coordinator Approved")
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading approval queue");
                return View(new List<Claim>());
            }
        }

        public async Task<IActionResult> ApprovedClaims()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "AcademicManager")
                {
                    TempData["ErrorMessage"] = "Access denied.";
                    return RedirectToAction("Index", "Home");
                }

                var claims = await _context.Claims
                    .Where(c => c.Status == "Manager Approved" || c.Status == "Manager Rejected")
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading approved claims");
                return View(new List<Claim>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagerApprove(int id)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(ApprovalQueue));
                }

                claim.Status = "Manager Approved";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Claim #{id} approved. Amount: R{claim.TotalAmount:N2}";
                return RedirectToAction(nameof(ApprovalQueue));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving claim");
                TempData["ErrorMessage"] = "Error approving claim.";
                return RedirectToAction(nameof(ApprovalQueue));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagerReject(int id, string reason)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(ApprovalQueue));
                }

                claim.Status = "Manager Rejected";
                claim.Notes = $"Manager Rejection: {reason}";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Claim #{id} rejected.";
                return RedirectToAction(nameof(ApprovalQueue));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim");
                TempData["ErrorMessage"] = "Error rejecting claim.";
                return RedirectToAction(nameof(ApprovalQueue));
            }
        }

        // ===== ADMIN ACTIONS =====

        public async Task<IActionResult> AllClaims()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "Admin")
                {
                    TempData["ErrorMessage"] = "Access denied. Administrators only.";
                    return RedirectToAction("Index", "Home");
                }

                var claims = await _context.Claims
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                ViewBag.TotalClaims = claims.Count;
                ViewBag.PendingClaims = claims.Count(c => c.Status.Contains("Pending"));
                ViewBag.ApprovedClaims = claims.Count(c => c.Status.Contains("Approved"));
                ViewBag.RejectedClaims = claims.Count(c => c.Status.Contains("Rejected"));

                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all claims");
                return View(new List<Claim>());
            }
        }
    }
}