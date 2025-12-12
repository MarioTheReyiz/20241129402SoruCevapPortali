using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR; // SignalR kütüphanesi
using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using _20241129402SoruCevapPortali.Hubs; // Hub klasörü referansı
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System;

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

        // SignalR Context (Bildirim göndermek için)
        private readonly IHubContext<GeneralHub> _hubContext;

        public AdminController(
            IRepository<Category> c,
            IRepository<Question> q,
            IRepository<AppUser> u,
            IRepository<Answer> a,
            IRepository<Log> logRepo,
            IRepository<Notification> notificationRepo,
            IHubContext<GeneralHub> hubContext // Inject edildi
        )
        {
            _categoryRepo = c;
            _questionRepo = q;
            _userRepo = u;
            _answerRepo = a;
            _logRepo = logRepo;
            _notificationRepo = notificationRepo;
            _hubContext = hubContext;
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

        // --- BİLDİRİM GÖNDERME (SIGNALR İLE CANLI) ---
        [HttpGet]
        public IActionResult SendNotification()
        {
            // SelectList'te Username kullanıyoruz
            ViewBag.Users = new SelectList(_userRepo.GetAll(), "Id", "UserName");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendNotification(string message, string targetType, string? specificUserId)
        {
            // 1. Veritabanına Kaydet
            var notif = new Notification
            {
                Message = message,
                SenderName = User.Identity.Name,
                Date = DateTime.Now,
                TargetRole = targetType
                // Not: Eğer kişiye özel bildirim yapacaksan Notification modelindeki TargetUserId tipini string yapman gerekebilir.
            };
            _notificationRepo.Add(notif);

            // 2. Log Tut
            KayitTut("Bildirim Gönderildi", $"Mesaj: {message} -> {targetType}");

            // 3. SIGNALR İLE ANLIK GÖNDER (YENİ)
            // İstemci tarafındaki "ReceiveNotification" metodunu tetikler
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);

            return RedirectToAction("SendNotification", new { success = true });
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
        public async Task<IActionResult> Profile(AppUser p, Microsoft.AspNetCore.Http.IFormFile? ImageFile)
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
    }
}