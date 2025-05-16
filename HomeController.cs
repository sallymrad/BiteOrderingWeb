using System.Diagnostics;
using BiteOrderWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BiteOrderWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("SuperAdmin"))
                    return RedirectToAction("SuperAdminHome", "SuperAdmin");

                else if (User.IsInRole("Admin"))
                    return RedirectToAction("Home", "Admin");
                else if (User.IsInRole("Driver"))
                    return RedirectToAction("Home", "Driver");

                else if (User.IsInRole("User")) 
                    return RedirectToAction("Index", "Client");
            }
            return View();
        }
        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }
        [Authorize(Roles="SuperAdmin")]
        public IActionResult SuperAdmin()
        {
            return View();
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            return View();
        }
        [Authorize(Roles = "Client")]
        public IActionResult Client()
        {
            return View();
        }
        [Authorize(Roles = "Driver")]
        public IActionResult Driver()
        {
            return View();
        }
       


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
