using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail; // Gerekli
using System.Security.Claims;
using System.Threading.Tasks;

namespace _20241129402SoruCevapPortali.Controllers
{
    public class AuthController : Controller
    {
        private readonly IRepository<User> _userRepository;
        private readonly IConfiguration _configuration;

        public AuthController(IRepository<User> userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
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
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Lütfen bir e-posta adresi giriniz.";
                return View();
            }

            var user = _userRepository.GetAll().FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                // Güvenlik gereği "Böyle bir kullanıcı yok" demek yerine
                // "Eğer kayıtlıysa kod gönderilmiştir" demek daha iyidir ama şimdilik böyle kalsın.
                ViewBag.Error = "Bu e-posta adresiyle kayıtlı kullanıcı bulunamadı.";
                return View();
            }

            // 6 Haneli Rastgele Kod Üret
            Random rnd = new Random();
            string code = rnd.Next(100000, 999999).ToString();

            // Veritabanına kaydet
            user.ResetCode = code;
            _userRepository.Update(user);

            // E-posta Gönderimi (Asenkron ve Config'den okuyarak)
            try
            {
                // Ayarları appsettings.json'dan çekiyoruz
                var emailSettings = _configuration.GetSection("EmailSettings");
                string senderEmail = emailSettings["Mail"];
                string senderPassword = emailSettings["Password"];
                string host = emailSettings["Host"];
                int port = int.Parse(emailSettings["Port"]);

                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false; // Önce bunu false yapmalısın
                    client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, "Soru Cevap Portalı"),
                        Subject = "Şifre Sıfırlama Kodu",
                        Body = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                                <h3>Merhaba {user.Name},</h3>
                                <p>Şifrenizi sıfırlamak için aşağıdaki kodu kullanabilirsiniz:</p>
                                <h1 style='color: #4e73df; letter-spacing: 5px;'>{code}</h1>
                                <p>Bu kodu siz talep etmediyseniz, bu e-postayı görmezden gelin.</p>
                            </div>",
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(email);

                    // Asenkron gönderim
                    await client.SendMailAsync(mailMessage);
                }

                TempData["ResetEmail"] = email;
                return RedirectToAction("VerifyCode");
            }
            catch (SmtpException smtpEx)
            {
                // SMTP hatalarını özel yakala
                ViewBag.Error = $"SMTP Hatası: {smtpEx.Message}. Durum Kodu: {smtpEx.StatusCode}";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Genel bir hata oluştu: " + ex.Message;
                return View();
            }
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