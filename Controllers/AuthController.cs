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
        // --- 1. E-POSTA GİRME VE KOD GÖNDERME ---
        [HttpGet]
        public IActionResult ForgotPassword() { return View(); }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var user = _userRepository.GetAll().FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Bu e-posta adresiyle kayıtlı kullanıcı bulunamadı.";
                return View();
            }

            // 6 Haneli Rastgele Kod Üret
            Random rnd = new Random();
            string code = rnd.Next(100000, 999999).ToString();

            // Veritabanına kaydet
            user.ResetCode = code;
            _userRepository.Update(user);

            // E-posta Gönder (System.Net.Mail)
            try
            {
                System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);
                client.EnableSsl = true;
                // DİKKAT: Buraya kendi e-posta ve uygulama şifreni yazmalısın!
                client.Credentials = new System.Net.NetworkCredential("msdn7788@gmail.com", "myvl hevc stca jqrj");

                System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
                mail.From = new System.Net.Mail.MailAddress("msdn7788@gmail.com", "Soru Cevap Portalı");
                mail.To.Add(email);
                mail.Subject = "Şifre Sıfırlama Kodu";
                mail.Body = $"Merhaba {user.Name},<br>Şifre sıfırlama kodunuz: <h2>{code}</h2>";
                mail.IsBodyHtml = true;

                client.Send(mail);

                TempData["ResetEmail"] = email; // Diğer sayfaya taşı
                return RedirectToAction("VerifyCode");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "E-posta gönderilemedi: " + ex.Message;
                return View();
            }
        }

        // --- 2. KODU DOĞRULAMA ---
        [HttpGet]
        public IActionResult VerifyCode()
        {
            if (TempData["ResetEmail"] == null) return RedirectToAction("ForgotPassword");
            TempData.Keep("ResetEmail");
            return View();
        }

        [HttpPost]
        public IActionResult VerifyCode(string code)
        {
            string email = TempData["ResetEmail"]?.ToString();
            var user = _userRepository.GetAll().FirstOrDefault(x => x.Email == email && x.ResetCode == code);

            if (user != null)
            {
                TempData["ResetUserId"] = user.Id; // Doğrulandı, ID'yi sakla
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = "Hatalı kod girdiniz!";
            TempData.Keep("ResetEmail");
            return View();
        }

        // --- 3. YENİ ŞİFRE ---
        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["ResetUserId"] == null) return RedirectToAction("ForgotPassword");
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string newPassword)
        {
            // --- HATA ÇÖZÜMÜ BURADA ---
            // TempData'nın dolu olup olmadığını kontrol ediyoruz.
            if (TempData["ResetUserId"] == null)
            {
                // Eğer hafıza silinmişse (sayfa yenilendiğinde vs.), güvenlik için en başa gönderiyoruz.
                ViewBag.Error = "Oturum süresi doldu, lütfen tekrar deneyin.";
                return RedirectToAction("ForgotPassword");
            }

            int userId = (int)TempData["ResetUserId"];
            var user = _userRepository.GetById(userId);

            if (user != null)
            {
                user.Password = newPassword;
                user.ResetCode = null; // Kullanılan kodu temizle (Güvenlik için)
                _userRepository.Update(user);

                // Başarılı olursa giriş sayfasına yönlendir
                return RedirectToAction("Login");
            }

            // Kullanıcı bulunamazsa
            ViewBag.Error = "Kullanıcı bulunamadı.";
            return View();
        }
    }
}