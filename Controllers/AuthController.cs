using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using System.Security.Claims;

namespace _20241129402SoruCevapPortali.Controllers
{
    public class AuthController : Controller
    {
        private readonly IRepository<User> _userRepository;

        public AuthController(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // 1. Veritabanından kullanıcıyı bul
            var user = _userRepository.GetAll()
                .FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                // 2. Yetkileri (Claims) hazırla
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                // 3. Giriş işlemini yap (Çerez oluştur)
                HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties).Wait();


                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }

                // Değilse Ana Sayfaya yönlendir.
                return RedirectToAction("Index", "Home");
            }

            // Hata mesajı
            ViewBag.ErrorMessage = "Geçersiz kullanıcı adı veya şifre.";
            return View();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            return RedirectToAction("Login", "Auth");
        }
    }
}