using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories; // Repository için gerekli
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace _20241129402SoruCevapPortali.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        // İSTATİSTİKLER İÇİN GEREKLİ OLANLAR:
        private readonly IRepository<Question> _questionRepo;
        private readonly IRepository<Answer> _answerRepo;

        // Constructor'da bunları istiyoruz
        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IRepository<Question> q,
            IRepository<Answer> a)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _questionRepo = q;
            _answerRepo = a;
        }

        // --- YARDIMCI METOD: İstatistikleri Hesapla ---
        private void CalculateStats(string userId)
        {
            var soruSayisi = _questionRepo.GetAll().Count(x => x.UserId == userId);
            var cevapSayisi = _answerRepo.GetAll().Count(x => x.UserId == userId);
            var soruLikelari = _questionRepo.GetAll().Where(x => x.UserId == userId).Sum(x => x.LikeCount);
            var cevapLikelari = _answerRepo.GetAll().Where(x => x.UserId == userId).Sum(x => x.LikeCount);

            ViewBag.SoruSayisi = soruSayisi;
            ViewBag.CevapSayisi = cevapSayisi;
            ViewBag.ToplamBegeni = soruLikelari + cevapLikelari;
        }

        // --- LOGIN ---
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Lütfen tüm alanları doldurun.";
                return View();
            }
            var user = await _userManager.FindByNameAsync(username);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
                if (result.Succeeded)
                {
                    if (await _userManager.IsInRoleAsync(user, "Admin")) return RedirectToAction("Index", "Admin");
                    return RedirectToAction("Index", "Home");
                }
            }
            ViewBag.ErrorMessage = "Geçersiz kullanıcı adı veya şifre.";
            return View();
        }

        // --- REGISTER ---
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(AppUser p, string password)
        {
            var user = new AppUser
            {
                UserName = p.UserName,
                Email = p.Email,
                Name = p.Name,
                Surname = p.Surname,
                PhoneNumber = p.PhoneNumber,
                PhotoUrl = "/img/undraw_profile.svg"
            };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                return RedirectToAction("Login");
            }
            foreach (var item in result.Errors) ModelState.AddModelError("", item.Description);
            return View(p);
        }

        // --- LOGOUT ---
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();

        // --- PROFİL DÜZENLEME ---
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            // Sayfa açılırken istatistikleri hesapla
            CalculateStats(user.Id);
            return View(user);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditProfile(AppUser model, IFormFile? ImageFile, string? NewPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // Hata olursa sayfaya geri döneceğiz, o yüzden istatistikleri yine hesapla
                CalculateStats(user.Id);

                if (user.UserName != model.UserName)
                {
                    var checkName = await _userManager.FindByNameAsync(model.UserName);
                    if (checkName == null)
                    {
                        user.UserName = model.UserName;
                        await _userManager.UpdateNormalizedUserNameAsync(user);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Bu kullanıcı adı zaten alınmış.");
                        return View(user);
                    }
                }

                if (ImageFile != null)
                {
                    var extension = Path.GetExtension(ImageFile.FileName);
                    var newImageName = Guid.NewGuid() + extension;
                    var location = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/", newImageName);
                    using (var stream = new FileStream(location, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    user.PhotoUrl = "/img/" + newImageName;
                }

                user.Name = model.Name;
                user.Surname = model.Surname;
                user.PhoneNumber = model.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);

                if (!string.IsNullOrEmpty(NewPassword) && result.Succeeded)
                {
                    await _userManager.RemovePasswordAsync(user);
                    await _userManager.AddPasswordAsync(user, NewPassword);
                }

                if (result.Succeeded) return RedirectToAction("Index", "Home");
            }
            return View(user);
        }

        // --- ŞİFRE SIFIRLAMA ---
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) { ViewBag.Error = "Kayıt bulunamadı."; return View(); }

            Random rnd = new Random();
            string code = rnd.Next(100000, 999999).ToString();
            TempData["ResetCode"] = code;
            TempData["ResetEmail"] = email;

            ViewBag.TestCode = code; // Test amaçlı
            return RedirectToAction("VerifyCode");
        }

        [HttpGet]
        public IActionResult VerifyCode() => View();

        [HttpPost]
        public IActionResult VerifyCode(string code)
        {
            if (code == TempData["ResetCode"]?.ToString())
            {
                TempData.Keep("ResetEmail");
                return RedirectToAction("ResetPassword");
            }
            ViewBag.Error = "Hatalı kod!";
            TempData.Keep("ResetCode"); TempData.Keep("ResetEmail");
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword)
        {
            var email = TempData["ResetEmail"]?.ToString();
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                await _userManager.RemovePasswordAsync(user);
                var result = await _userManager.AddPasswordAsync(user, newPassword);
                if (result.Succeeded) return RedirectToAction("Login");
            }
            return View();
        }
    }
}