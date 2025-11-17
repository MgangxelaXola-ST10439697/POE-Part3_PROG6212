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

        // Allowed file extensions and max file size
        private readonly string[] _allowedExtensions = { ".pdf", ".docx", ".xlsx" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public ClaimsController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<ClaimsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: Submit claim form
        public IActionResult Submit()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Submit view");
                TempData["ErrorMessage"] = "An error occurred while loading the form. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Submit claim - ENHANCED VERSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Claim claim, IFormFile upload)
        {
            _logger.LogInformation("Submit method called for lecturer: {LecturerName}", claim?.LecturerName);

            try
            {
                // Validate input
                if (claim == null)
                {
                    _logger.LogWarning("Claim object is null");
                    TempData["ErrorMessage"] = "Invalid claim data received.";
                    return View();
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Validation Error: {ErrorMessage}", error.ErrorMessage);
                    }
                    TempData["ErrorMessage"] = "Please correct the errors in the form.";
                    return View(claim);
                }

                // Create a new claim with validated data
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

                // Handle file upload with enhanced validation
                if (upload != null && upload.Length > 0)
                {
                    _logger.LogInformation("Processing file upload: {FileName}", upload.FileName);

                    // Validate file extension
                    var fileExtension = Path.GetExtension(upload.FileName).ToLower();
                    if (!_allowedExtensions.Contains(fileExtension))
                    {
                        _logger.LogWarning("Invalid file type attempted: {FileExtension}", fileExtension);
                        TempData["ErrorMessage"] = $"Invalid file type. Only {string.Join(", ", _allowedExtensions)} files are allowed.";
                        return View(claim);
                    }

                    // Validate file size
                    if (upload.Length > _maxFileSize)
                    {
                        _logger.LogWarning("File size exceeds limit: {FileSize} bytes", upload.Length);
                        TempData["ErrorMessage"] = "File size cannot exceed 5MB.";
                        return View(claim);
                    }

                    // Ensure uploads folder exists
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                        _logger.LogInformation("Created uploads folder: {UploadsFolder}", uploadsFolder);
                    }

                    // Generate unique filename and save
                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    try
                    {
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await upload.CopyToAsync(stream);
                        }

                        newClaim.FileName = upload.FileName;
                        newClaim.FilePath = $"/uploads/{uniqueFileName}";
                        _logger.LogInformation("File saved successfully: {FilePath}", filePath);
                    }
                    catch (IOException ioEx)
                    {
                        _logger.LogError(ioEx, "Error saving file: {FileName}", upload.FileName);
                        TempData["ErrorMessage"] = "Error saving file. Please try again.";
                        return View(claim);
                    }
                }

                // Save to database
                _context.Claims.Add(newClaim);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Claim saved successfully with ID: {ClaimId}", newClaim.Id);

                TempData["SuccessMessage"] = $"Claim submitted successfully! Your Claim ID is: {newClaim.Id}. Total Amount: R{newClaim.TotalAmount:N2}";
                return RedirectToAction(nameof(Submit));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while saving claim");
                TempData["ErrorMessage"] = "Database error occurred. Please contact support.";
                return View(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Submit method");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                return View(claim);
            }
        }

        // View claim history
        public async Task<IActionResult> History()
        {
            try
            {
                var claims = await _context.Claims
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {ClaimCount} claims for history view", claims.Count);

                ViewBag.TotalClaims = claims.Count;
                ViewBag.PendingClaims = claims.Count(c => c.Status == "Pending");
                ViewBag.ApprovedClaims = claims.Count(c => c.Status == "Approved");
                ViewBag.RejectedClaims = claims.Count(c => c.Status == "Rejected");

                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading claim history");
                TempData["ErrorMessage"] = "Error loading claim history. Please try again.";
                return View(new List<Claim>());
            }
        }

        // Coordinator view - manage claims
        public async Task<IActionResult> Manage()
        {
            try
            {
                // Check user role (basic session check)
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "Coordinator" && userRole != "Manager")
                {
                    _logger.LogWarning("Unauthorized access attempt to Manage by user role: {UserRole}", userRole);
                    TempData["ErrorMessage"] = "You do not have permission to access this page.";
                    return RedirectToAction("Index", "Home");
                }

                var claims = await _context.Claims
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {ClaimCount} claims for management view", claims.Count);
                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading manage claims view");
                TempData["ErrorMessage"] = "Error loading claims. Please try again.";
                return View(new List<Claim>());
            }
        }

        // Track claim status
        public async Task<IActionResult> Track()
        {
            try
            {
                var claims = await _context.Claims
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {ClaimCount} claims for tracking view", claims.Count);
                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading track claims view");
                TempData["ErrorMessage"] = "Error loading claim tracking. Please try again.";
                return View(new List<Claim>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid claim ID provided for approval: {ClaimId}", id);
                    TempData["ErrorMessage"] = "Invalid claim ID.";
                    return RedirectToAction(nameof(Manage));
                }

                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    _logger.LogWarning("Claim not found for approval: {ClaimId}", id);
                    TempData["ErrorMessage"] = $"Claim #{id} not found.";
                    return RedirectToAction(nameof(Manage));
                }

                if (claim.Status == "Approved")
                {
                    _logger.LogInformation("Claim already approved: {ClaimId}", id);
                    TempData["InfoMessage"] = $"Claim #{id} is already approved.";
                    return RedirectToAction(nameof(Manage));
                }

                claim.Status = "Approved";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Claim approved successfully: {ClaimId}", id);
                TempData["SuccessMessage"] = $"Claim #{id} approved! Amount: R{claim.TotalAmount:N2}";
                return RedirectToAction(nameof(Manage));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while approving claim: {ClaimId}", id);
                TempData["ErrorMessage"] = "Error approving claim. Please try again.";
                return RedirectToAction(nameof(Manage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving claim: {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while approving the claim.";
                return RedirectToAction(nameof(Manage));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid claim ID provided for rejection: {ClaimId}", id);
                    TempData["ErrorMessage"] = "Invalid claim ID.";
                    return RedirectToAction(nameof(Manage));
                }

                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    _logger.LogWarning("Claim not found for rejection: {ClaimId}", id);
                    TempData["ErrorMessage"] = $"Claim #{id} not found.";
                    return RedirectToAction(nameof(Manage));
                }

                if (claim.Status == "Rejected")
                {
                    _logger.LogInformation("Claim already rejected: {ClaimId}", id);
                    TempData["InfoMessage"] = $"Claim #{id} is already rejected.";
                    return RedirectToAction(nameof(Manage));
                }

                claim.Status = "Rejected";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Claim rejected successfully: {ClaimId}", id);
                TempData["SuccessMessage"] = $"Claim #{id} rejected.";
                return RedirectToAction(nameof(Manage));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while rejecting claim: {ClaimId}", id);
                TempData["ErrorMessage"] = "Error rejecting claim. Please try again.";
                return RedirectToAction(nameof(Manage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim: {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while rejecting the claim.";
                return RedirectToAction(nameof(Manage));
            }
        }
    }
}