using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace _20241129402SoruCevapPortali.Controllers
{
    public class AuthController : Controller
    {
        private readonly IRepository<User> _userRepository;

        public AuthController(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        // --- MEVCUT LOGIN KODLARI ---
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _userRepository.GetAll().FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role ?? "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties).Wait();

                if (user.Role == "Admin" || user.Role == "Moderator" || user.Username.ToLower() == "admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ErrorMessage = "Geçersiz kullanıcı adı veya şifre.";
            return View();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            return RedirectToAction("Login");
        }

        // --- YENİ EKLENEN: KAYIT OL (REGISTER) ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User p)
        {
            // 1. Bu kullanıcı adı daha önce alınmış mı?
            var existingUser = _userRepository.GetAll().FirstOrDefault(x => x.Username == p.Username);
            if (existingUser != null)
            {
                ViewBag.ErrorMessage = "Bu kullanıcı adı zaten kullanılıyor.";
                return View();
            }

            // 2. Varsayılan Değerleri Ata
            p.Role = "User"; // Yeni gelen herkes standart üyedir
            p.PhotoUrl = "/img/undraw_profile.svg"; // Mavi kafa varsayılan resim

            // 3. Kaydet
            _userRepository.Add(p); // Repository içindeki Save() sayesinde veritabanına işlenir

            // 4. Giriş sayfasına yönlendir
            return RedirectToAction("Login");
        }
    }
}