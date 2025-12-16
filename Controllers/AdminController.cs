using _20241129402SoruCevapPortali.Hubs; // Hub klasörü referansı
using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR; // SignalR kütüphanesi
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace _20241129402SoruCevapPortali.Controllers
{
    [Authorize(Roles = "Admin,Moderator")] // Sadece yetkililer girebilir
    public class AdminController : Controller
    {
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Question> _questionRepo;
        private readonly IRepository<AppUser> _userRepo; // User -> AppUser oldu
        private readonly IRepository<Answer> _answerRepo;
        private readonly IRepository<Log> _logRepo;
        private readonly IRepository<Notification> _notificationRepo;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // <-- BUNU EKLE
        // SignalR Context (Bildirim göndermek için)
        private readonly IHubContext<GeneralHub> _hubContext;

        public AdminController(
            IRepository<Category> c,
            IRepository<Question> q,
            IRepository<AppUser> u,
            IRepository<Answer> a,
            IRepository<Log> logRepo,
            IRepository<Notification> notificationRepo,
            IHubContext<GeneralHub> hubContext, // Inject edildi
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            _categoryRepo = c;
            _questionRepo = q;
            _userRepo = u;
            _answerRepo = a;
            _logRepo = logRepo;
            _notificationRepo = notificationRepo;
            _hubContext = hubContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // --- DASHBOARD ---
        public IActionResult Index()
        {
            ViewBag.TotalCategories = _categoryRepo.GetAll().Count;
            var questions = _questionRepo.GetAll();
            ViewBag.TotalQuestions = questions.Count;
            ViewBag.PendingQuestions = questions.Count(q => !q.IsApproved);

            var answers = _answerRepo.GetAll();
            if (questions.Count > 0)
            {
                var answeredQuestionCount = answers.Select(a => a.QuestionId).Distinct().Count();
                ViewBag.AnswerRate = (int)((double)answeredQuestionCount / questions.Count * 100);
            }
            else
            {
                ViewBag.AnswerRate = 0;
            }

            return View();
        }
        // --- EKSİK OLAN KISIM BURASI ---
        [HttpGet] // Bu satır, sayfanın açılmasını sağlar
        public IActionResult SendNotification()
        {
            // Kullanıcı seçimi için listeyi dolduruyoruz
            ViewBag.Users = new SelectList(_userRepo.GetAll(), "Id", "UserName");
            return View();
        }
        // ------------------------------
        [HttpPost]
        public async Task<IActionResult> SendNotification(string message, string targetType, string? specificUserId)
        {
            // A) ALICILARI BELİRLE
            var alicilar = new List<AppUser>();

            if (targetType == "Private" && !string.IsNullOrEmpty(specificUserId))
            {
                // Tek kişiye
                var user = await _userManager.FindByIdAsync(specificUserId);
                if (user != null) alicilar.Add(user);
            }
            else if (targetType == "All")
            {
                // Herkese
                alicilar = _userManager.Users.ToList();
            }
            else
            {
                // Belirli role (Admin, User vs.)
                var usersInRole = await _userManager.GetUsersInRoleAsync(targetType);
                alicilar.AddRange(usersInRole);
            }

            // B) HERKES İÇİN AYRI BİLDİRİM OLUŞTUR
            foreach (var alici in alicilar)
            {
                var notif = new Notification
                {
                    Message = message,
                    SenderName = User.Identity.Name,
                    Date = DateTime.Now,
                    IsRead = false,          // Okunmadı
                    TargetRole = "Private",  // Artık hepsi kişiye özel
                    TargetUserId = alici.Id  // Alıcının String ID'si
                };
                _notificationRepo.Add(notif);
            }

            // C) LOGLAMA VE SİNYAL GÖNDERME
            KayitTut("Bildirim Gönderildi", $"Mesaj: {message} -> {alicilar.Count} kişiye.");
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);

            return RedirectToAction("SendNotification", new { success = true });
        }
        public IActionResult AllNotifications()
        {
            // 1. Aktif kullanıcıyı bul
            var activeUser = _userRepo.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);

            // 2. Rolü ve ID'yi al
            var myRole = User.IsInRole("Admin") ? "Admin" : "User";
            string myId = activeUser != null ? activeUser.Id : "";

            // 3. Bildirimleri getir
            var list = _notificationRepo.GetAll()
                .Where(n =>
                    n.TargetRole == "All" ||
                    n.TargetRole == myRole ||
                    // DÜZELTME: n.TargetUserId.ToString() diyerek string'e çevirdik
                    (n.TargetRole == "Private" && n.TargetUserId.ToString() == myId)
                )
                .OrderByDescending(n => n.Date)
                .ToList();

            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationsAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            // Sadece bana ait ve okunmamış olanları bul
            var unreadNotifications = _notificationRepo.GetAll()
                .Where(n => n.TargetUserId == user.Id && !n.IsRead)
                .ToList();

            foreach (var notif in unreadNotifications)
            {
                notif.IsRead = true;
                _notificationRepo.Update(notif);
            }

            return Json(new { success = true });
        }

        // --- KULLANICI YÖNETİMİ ---
        public IActionResult Users() => View(_userRepo.GetAll());

        [HttpPost]
        public IActionResult DeleteUser(string id) // Identity ID'si string'dir
        {
            // Repository string ID desteklemiyorsa FirstOrDefault ile buluyoruz
            var item = _userRepo.GetAll().FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                string kAdi = item.UserName;
                _userRepo.Delete(item);
                KayitTut("Kullanıcı Silindi", $"Silinen: {kAdi}");
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // --- KATEGORİ YÖNETİMİ ---
        public IActionResult Categories() => View(_categoryRepo.GetAll());

        [HttpPost]
        public IActionResult AddCategory(Category p)
        {
            _categoryRepo.Add(p);
            KayitTut("Kategori Eklendi", $"Yeni: {p.Name}");
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public IActionResult DeleteCategory(int id)
        {
            var item = _categoryRepo.GetById(id);
            if (item != null)
            {
                _categoryRepo.Delete(item);
                KayitTut("Kategori Silindi", $"ID: {id}");
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // --- SORU YÖNETİMİ ---
        public IActionResult Questions() => View(_questionRepo.GetAll());

        [HttpPost]
        public IActionResult DeleteQuestion(int id)
        {
            var item = _questionRepo.GetById(id);
            if (item != null)
            {
                _questionRepo.Delete(item);
                KayitTut("Soru Silindi", $"ID: {id}");
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public IActionResult AddQuestion()
        {
            ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public IActionResult AddQuestion(Question p)
        {
            p.CreatedDate = DateTime.Now;
            p.IsApproved = true;

            // Şu anki Admin kullanıcısını bul
            var user = _userRepo.GetAll().FirstOrDefault(x => x.UserName == User.Identity.Name);
            p.UserId = user != null ? user.Id : "1"; // String ID ataması

            _questionRepo.Add(p);
            KayitTut("Soru Eklendi", $"Başlık: {p.Title}");
            return RedirectToAction("Questions");
        }

        [HttpGet]
        public IActionResult EditQuestion(int id)
        {
            var q = _questionRepo.GetById(id);
            if (q == null) return RedirectToAction("Questions");
            ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "Id", "Name", q.CategoryId);
            return View(q);
        }

        [HttpPost]
        public IActionResult EditQuestion(Question p)
        {
            var existing = _questionRepo.GetById(p.Id);
            if (existing != null)
            {
                existing.Title = p.Title;
                existing.Content = p.Content;
                existing.CategoryId = p.CategoryId;
                existing.IsApproved = p.IsApproved;
                _questionRepo.Update(existing);
                KayitTut("Soru Düzenlendi", $"ID: {p.Id}");
            }
            return RedirectToAction("Questions");
        }

        // --- PROFİL GÜNCELLEME ---
        [HttpGet]
        public IActionResult Profile()
        {
            var username = User.Identity.Name;
            var user = _userRepo.GetAll().FirstOrDefault(x => x.UserName == username);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(AppUser p, Microsoft.AspNetCore.Http.IFormFile? ImageFile)
        {
            var userToUpdate = _userRepo.GetAll().FirstOrDefault(x => x.UserName == User.Identity.Name);

            if (userToUpdate != null)
            {
                userToUpdate.Name = p.Name;
                userToUpdate.Surname = p.Surname;
                userToUpdate.Email = p.Email;
                userToUpdate.PhoneNumber = p.PhoneNumber;

                // Şifre güncelleme için Identity UserManager kullanmak daha güvenlidir
                // Ancak burada basit güncelleme örneği veriyoruz.
                // Not: PasswordHash doğrudan güncellenemez, UserManager.ChangePasswordAsync kullanılmalı.

                if (ImageFile != null)
                {
                    var extension = Path.GetExtension(ImageFile.FileName);
                    var newImageName = Guid.NewGuid() + extension;
                    var location = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/", newImageName);
                    using (var stream = new FileStream(location, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    userToUpdate.PhotoUrl = "/img/" + newImageName;
                }

                _userRepo.Update(userToUpdate);
                KayitTut("Profil Güncellendi", $"{userToUpdate.UserName} bilgilerini güncelledi.");
            }
            return RedirectToAction("Profile");
        }

        // --- RAPOR İNDİRME ---
        public IActionResult ExportReport()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("--- YÖNETİCİ RAPORU ---");
            sb.AppendLine($"Tarih: {DateTime.Now}");
            sb.AppendLine($"Toplam Kullanıcı: {_userRepo.GetAll().Count}");
            sb.AppendLine($"Toplam Soru: {_questionRepo.GetAll().Count}");

            var content = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(content, "text/plain", "Rapor.txt");
        }

        // --- LOGLAMA YARDIMCISI ---
        private void KayitTut(string islem, string detay)
        {
            var log = new Log
            {
                Username = User.Identity.IsAuthenticated ? User.Identity.Name : "Sistem",
                Action = islem,
                Details = detay,
                Date = DateTime.Now
            };
            _logRepo.Add(log);
        }

        public IActionResult Logs()
        {
            return View(_logRepo.GetAll().OrderByDescending(x => x.Date).ToList());
        }

        public IActionResult Answers() => View(_answerRepo.GetAll());

        [HttpPost]
        public IActionResult DeleteAnswer(int id)
        {
            var item = _answerRepo.GetById(id);
            if (item != null)
            {
                _answerRepo.Delete(item);
                KayitTut("Cevap Silindi", $"ID: {id}");
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        // --- YETKİ (ROL) YÖNETİMİ ---

        [HttpGet]
        public IActionResult RoleManagement()
        {
            // Veritabanındaki rolleri listele
            var users = _userManager.Users.ToList();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (!string.IsNullOrEmpty(roleName))
            {
                // Rol daha önce yoksa oluştur
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                    KayitTut("Rol Eklendi", $"Yeni Rol: {roleName}");
                }
            }
            return RedirectToAction("RoleManagement");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                await _roleManager.DeleteAsync(role);
                KayitTut("Rol Silindi", $"Rol: {role.Name}");
            }
            return RedirectToAction("RoleManagement");
        }
    }

}