using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace _20241129402SoruCevapPortali.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IRepository<UserReport> _reportRepo;

        public ReportController(UserManager<AppUser> u, IRepository<UserReport> r)
        {
            _userManager = u;
            _reportRepo = r;
        }

        [HttpGet]
        public IActionResult Create(string reportedUserId)
        {
            ViewBag.ReportedId = reportedUserId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserReport p, IFormFile? ScreenshotFile)
        {
            var user = await _userManager.GetUserAsync(User);
            p.ReporterId = user.Id;
            p.Date = DateTime.Now;
            p.IsReviewed = false;

            if (ScreenshotFile != null)
            {
                var ext = Path.GetExtension(ScreenshotFile.FileName);
                var newName = Guid.NewGuid() + ext;
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/reports/", newName);
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/reports/"));

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await ScreenshotFile.CopyToAsync(stream);
                }
                p.ScreenshotUrl = "/img/reports/" + newName;
            }

            _reportRepo.Add(p);

            TempData["Success"] = "Kullanıcı başarıyla raporlandı. Yönetim inceleyecektir.";
            return RedirectToAction("Index", "Home");
        }
    }
}