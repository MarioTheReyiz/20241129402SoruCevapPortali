using _20241129402SoruCevapPortali.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace _20241129402SoruCevapPortali.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // --- LOGIN (GİRİŞ) ---
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Lütfen tüm alanları doldurun.";
                return View();
            }

            // Kullanıcıyı bul
            var user = await _userManager.FindByNameAsync(username);

            if (user != null)
            {
                // Şifre kontrolü ve giriş yapma
                var result = await _signInManager.PasswordSignInAsync(user, password, false, false);

                if (result.Succeeded)
                {
                    // Admin ise panele, değilse ana sayfaya yönlendir
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.ErrorMessage = "Geçersiz kullanıcı adı veya şifre.";
            return View();
        }

        // --- REGISTER (KAYIT OL) ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(AppUser p, string password)
        {
            // Yeni Identity kullanıcısı oluşturma
            var user = new AppUser
            {
                UserName = p.UserName,
                Email = p.Email,
                Name = p.Name,
                Surname = p.Surname,
                PhoneNumber = p.PhoneNumber,
                PhotoUrl = "/img/undraw_profile.svg" // Varsayılan avatar
            };

            // Kullanıcıyı kaydet (Şifre otomatik hashlenir)
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Yeni kullanıcıya varsayılan olarak "User" rolü ata
                await _userManager.AddToRoleAsync(user, "User");
                return RedirectToAction("Login");
            }
            else
            {
                // Hataları (şifre zayıf, kullanıcı adı var vb.) ekrana bas
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
            }

            return View(p);
        }

        // --- LOGOUT (ÇIKIŞ) ---
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // --- YETKİSİZ ERİŞİM ---
        public IActionResult AccessDenied()
        {
            return View(); // Views/Auth/AccessDenied.cshtml oluşturmalısın
        }
    }
}