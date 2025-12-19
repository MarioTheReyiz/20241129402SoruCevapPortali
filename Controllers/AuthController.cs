using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using _20241129402SoruCevapPortali.ViewModels;
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
        private readonly IRepository<Question> _questionRepo;
        private readonly IRepository<Answer> _answerRepo;
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
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel p)
        {
            if (ModelState.IsValid)
            {
                var existingEmail = await _userManager.FindByEmailAsync(p.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("", "Bu e-posta adresi zaten sistemde kayıtlı! Lütfen başka bir e-posta deneyin.");
                    return View(p);
                }
                if (_userManager.Users.Any(u => u.PhoneNumber == p.PhoneNumber))
                {
                    ModelState.AddModelError("", "Bu telefon numarası başka bir üye tarafından kullanılıyor.");
                    return View(p);
                }
                AppUser user = new AppUser()
                {
                    UserName = p.UserName,
                    Email = p.Email,
                    PhoneNumber = p.PhoneNumber,
                    PhotoUrl = "/img/undraw_profile.svg"
                };
                var result = await _userManager.CreateAsync(user, p.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    return RedirectToAction("Login");
                }
                else
                {
                    foreach (var item in result.Errors)
                    {
                        if (item.Code == "DuplicateUserName")
                            ModelState.AddModelError("", "Bu kullanıcı adı zaten alınmış.");
                        else
                            ModelState.AddModelError("", item.Description);
                    }
                }
            }
            return View(p);
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
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

            ViewBag.TestCode = code;
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