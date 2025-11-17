using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class AccountController : Controller
    {
        // GET: Login page
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password)
        {
            //  LECTURER LOGIN
            if (username == "lecturer" && password == "lect123")
            {
                HttpContext.Session.SetString("UserRole", "Lecturer");
                HttpContext.Session.SetString("UserName", "John Doe (Lecturer)");
                HttpContext.Session.SetString("UserId", "1");
                TempData["SuccessMessage"] = "Welcome, Lecturer!";
                return RedirectToAction("Submit", "Claims");
            }
            //  PROGRAMME COORDINATOR LOGIN
            else if (username == "coordinator" && password == "coord123")
            {
                HttpContext.Session.SetString("UserRole", "Coordinator");
                HttpContext.Session.SetString("UserName", "Sarah Smith (Coordinator)");
                HttpContext.Session.SetString("UserId", "2");
                TempData["SuccessMessage"] = "Welcome, Programme Coordinator!";
                return RedirectToAction("Manage", "Claims");
            }
            //  ACADEMIC MANAGER LOGIN (NEW)
            else if (username == "manager" && password == "mgr123")
            {
                HttpContext.Session.SetString("UserRole", "Manager");
                HttpContext.Session.SetString("UserName", "David Johnson (Manager)");
                HttpContext.Session.SetString("UserId", "3");
                TempData["SuccessMessage"] = "Welcome, Academic Manager!";
                return RedirectToAction("ManagerReview", "Claims");
            }
            //  HR SUPER-USER LOGIN (NEW)
            else if (username == "hr" && password == "hr123")
            {
                HttpContext.Session.SetString("UserRole", "HR");
                HttpContext.Session.SetString("UserName", "Emma Wilson (HR Admin)");
                HttpContext.Session.SetString("UserId", "4");
                TempData["SuccessMessage"] = "Welcome, HR Administrator!";
                return RedirectToAction("Dashboard", "HR");
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