using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password)
        {
            // LECTURER LOGIN
            if (username == "lecturer" && password == "lect123")
            {
                HttpContext.Session.SetString("UserRole", "Lecturer");
                HttpContext.Session.SetString("UserName", "Dr. John Doe");
                HttpContext.Session.SetString("UserId", "1");
                TempData["SuccessMessage"] = "Welcome, Dr. Doe!";
                return RedirectToAction("Submit", "Claims");
            }
            // PROGRAMME COORDINATOR LOGIN
            else if (username == "coordinator" && password == "coord123")
            {
                HttpContext.Session.SetString("UserRole", "ProgrammeCoordinator");
                HttpContext.Session.SetString("UserName", "Prof. Sarah Smith");
                HttpContext.Session.SetString("UserId", "2");
                TempData["SuccessMessage"] = "Welcome, Programme Coordinator!";
                return RedirectToAction("PendingReview", "Claims");
            }
            // ACADEMIC MANAGER LOGIN
            else if (username == "manager" && password == "mgr123")
            {
                HttpContext.Session.SetString("UserRole", "AcademicManager");
                HttpContext.Session.SetString("UserName", "Prof. David Johnson");
                HttpContext.Session.SetString("UserId", "3");
                TempData["SuccessMessage"] = "Welcome, Academic Manager!";
                return RedirectToAction("ApprovalQueue", "Claims");
            }
            // ADMIN (HR) LOGIN
            else if (username == "admin" && password == "admin123")
            {
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserName", "Emma Wilson");
                HttpContext.Session.SetString("UserId", "4");
                TempData["SuccessMessage"] = "Welcome, Administrator!";
                return RedirectToAction("Dashboard", "Admin");
            }
            else
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }
    }
}