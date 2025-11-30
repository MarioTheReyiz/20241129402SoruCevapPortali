using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;

namespace _20241129402SoruCevapPortali.Controllers
{
    [Authorize(Roles = "Admin,Moderator")] // Güvenlik Kilidi: Sadece yetkililer girebilir
    public class AdminController : Controller
    {
        // --- 1. DEĞİŞKENLERİ TANIMLA ---
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Question> _questionRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Answer> _answerRepo;
        private readonly IRepository<Log> _logRepo;
        private readonly IRepository<Notification> _notificationRepo;

        // --- 2. CONSTRUCTOR (YAPICI METOT) ---
        public AdminController(
            IRepository<Category> c,
            IRepository<Question> q,
            IRepository<User> u,
            IRepository<Answer> a,
            IRepository<Log> logRepo,
            IRepository<Notification> notificationRepo
        )
        {
            _categoryRepo = c;
            _questionRepo = q;
            _userRepo = u;
            _answerRepo = a;
            _logRepo = logRepo;
            _notificationRepo = notificationRepo;
        }

        // --- DASHBOARD ---
        public IActionResult Index()
        {
            // 1. Toplam Kategori Sayısı
            ViewBag.TotalCategories = _categoryRepo.GetAll().Count;

            // 2. Sorularla İlgili İstatistikler
            var questions = _questionRepo.GetAll();
            ViewBag.TotalQuestions = questions.Count;
            ViewBag.PendingQuestions = questions.Count(q => !q.IsApproved);

            // 3. Cevap Oranı Hesabı
            var answers = _answerRepo.GetAll();
            var totalQuestions = questions.Count;

            if (totalQuestions > 0)
            {
                var answeredQuestionCount = answers.Select(a => a.QuestionId).Distinct().Count();
                ViewBag.AnswerRate = (int)((double)answeredQuestionCount / totalQuestions * 100);
            }
            else
            {
                ViewBag.AnswerRate = 0;
            }

            return View();
        }

        // --- KATEGORİ YÖNETİMİ ---
        public IActionResult Categories() => View(_categoryRepo.GetAll());

        [HttpPost]
        public IActionResult AddCategory(Category p)
        {
            // Doğrudan ekleme yapıyoruz (Vize kolaylığı için validasyonu esnettim)
            _categoryRepo.Add(p);
            KayitTut("Kategori Eklendi", $"Yeni Kategori: {p.Name}");
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public IActionResult DeleteCategory(int id)
        {
            var item = _categoryRepo.GetById(id);
            if (item != null)
            {
                string ad = item.Name;
                _categoryRepo.Delete(item);

                // LOG EKLENDİ
                KayitTut("Kategori Silindi", $"Silinen: {ad} (ID: {id})");

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // --- KULLANICI YÖNETİMİ ---
        public IActionResult Users() => View(_userRepo.GetAll());

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var item = _userRepo.GetById(id);
            if (item != null)
            {
                string kAdi = item.Username;
                _userRepo.Delete(item);

                // LOG EKLENDİ
                KayitTut("Kullanıcı Silindi", $"Silinen: {kAdi} (ID: {id})");

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

                // LOG EKLENDİ
                KayitTut("Soru Silindi", $"Soru ID: {id} silindi.");

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
            p.UserId = 1; // Varsayılan Admin ID'si

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

                KayitTut("Soru Düzenlendi", $"ID: {p.Id} güncellendi.");
            }
            return RedirectToAction("Questions");
        }

        // --- FORUM DETAYI ---
        public IActionResult ForumDetails(int id)
        {
            var question = _questionRepo.GetById(id);
            if (question == null) return RedirectToAction("Questions");

            question.User = _userRepo.GetById(question.UserId);

            var answers = _answerRepo.GetAll().Where(x => x.QuestionId == id).ToList();
            foreach (var answer in answers)
            {
                answer.User = _userRepo.GetById(answer.UserId);
            }

            ViewBag.Answers = answers;
            return View(question);
        }

        // --- CEVAPLAR ---
        public IActionResult Answers() => View(_answerRepo.GetAll());

        [HttpPost]
        public IActionResult DeleteAnswer(int id)
        {
            var item = _answerRepo.GetById(id);
            if (item != null)
            {
                _answerRepo.Delete(item);

                // LOG EKLENDİ
                KayitTut("Cevap Silindi", $"Cevap ID: {id} silindi.");

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // --- PROFİL ---
        [HttpGet]
        public IActionResult Profile()
        {
            var username = User.Identity.Name;
            var user = _userRepo.GetAll().FirstOrDefault(x => x.Username == username);
            if (user == null) user = _userRepo.GetById(1);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(User p, IFormFile? ImageFile)
        {
            var currentUser = _userRepo.GetAll().FirstOrDefault(x => x.Username == User.Identity.Name);
            var userToUpdate = currentUser ?? _userRepo.GetById(p.Id);

            if (userToUpdate != null)
            {
                userToUpdate.Name = p.Name;
                userToUpdate.Surname = p.Surname;
                userToUpdate.Email = p.Email;
                userToUpdate.PhoneNumber = p.PhoneNumber;
                userToUpdate.Password = p.Password;

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
                KayitTut("Profil Güncellendi", $"{userToUpdate.Username} bilgilerini güncelledi.");
            }
            return RedirectToAction("Profile");
        }

        // --- RAPOR İNDİRME ---
        public IActionResult ExportReport()
        {
            var categoryCount = _categoryRepo.GetAll().Count;
            var questions = _questionRepo.GetAll();
            var answerCount = _answerRepo.GetAll().Count;
            var userCount = _userRepo.GetAll().Count;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("==========================================");
            sb.AppendLine("   SORU CEVAP PORTALI - YÖNETİCİ RAPORU   ");
            sb.AppendLine("==========================================");
            sb.AppendLine($"Rapor Tarihi: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}");
            sb.AppendLine("");
            sb.AppendLine("--- GENEL İSTATİSTİKLER ---");
            sb.AppendLine($"Toplam Üye Sayısı       : {userCount}");
            sb.AppendLine($"Toplam Kategori Sayısı  : {categoryCount}");
            sb.AppendLine($"Toplam Soru Sayısı      : {questions.Count}");
            sb.AppendLine($"Toplam Cevap Sayısı     : {answerCount}");
            sb.AppendLine("");
            sb.AppendLine("--- SORU DURUMLARI ---");
            sb.AppendLine($"Yayındaki Sorular       : {questions.Count(x => x.IsApproved)}");
            sb.AppendLine($"Onay Bekleyen Sorular   : {questions.Count(x => !x.IsApproved)}");
            sb.AppendLine("");
            sb.AppendLine("==========================================");

            var content = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

            // LOG EKLENDİ
            KayitTut("Rapor Alındı", "Yönetici raporu indirildi.");

            return File(content, "text/plain", $"Yonetim_Raporu_{DateTime.Now.ToString("ddMMyyyy")}.txt");
        }

        // --- BİLDİRİM YÖNETİMİ ---
        [HttpGet]
        public IActionResult SendNotification()
        {
            ViewBag.Users = new SelectList(_userRepo.GetAll(), "Id", "Username");
            return View();
        }

        [HttpPost]
        public IActionResult SendNotification(string message, string targetType, int? specificUserId)
        {
            var notif = new Notification
            {
                Message = message,
                SenderName = User.Identity.Name,
                Date = DateTime.Now
            };

            if (targetType == "SpecificUser" && specificUserId.HasValue)
            {
                notif.TargetUserId = specificUserId;
                notif.TargetRole = "Private";
            }
            else
            {
                notif.TargetRole = targetType;
            }

            _notificationRepo.Add(notif);

            // LOG EKLENDİ
            KayitTut("Bildirim Gönderildi", $"Mesaj: {message} -> {targetType}");

            return RedirectToAction("SendNotification", new { success = true });
        }

        // --- YETKİ YÖNETİMİ ---
        [HttpGet]
        public IActionResult RoleManagement()
        {
            if (User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value != "Admin")
            {
                return RedirectToAction("Index");
            }
            var users = _userRepo.GetAll();
            return View(users);
        }

        [HttpPost]
        public IActionResult UpdateRole(int id, string newRole)
        {
            if (User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value != "Admin")
            {
                return Json(new { success = false, message = "Yetkiniz yok!" });
            }

            var user = _userRepo.GetById(id);
            if (user != null)
            {
                if (user.Username == User.Identity.Name)
                {
                    return Json(new { success = false, message = "Kendi yetkinizi değiştiremezsiniz." });
                }

                string eskiRol = user.Role;
                user.Role = newRole;
                _userRepo.Update(user);

                // LOG EKLENDİ
                KayitTut("Rol Değiştirildi", $"{user.Username}: {eskiRol} -> {newRole}");

                return Json(new { success = true, message = "Rol güncellendi." });
            }
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        // --- SİSTEM KAYITLARI (LOGS) ---
        public IActionResult Logs()
        {
            var logs = _logRepo.GetAll().OrderByDescending(x => x.Date).ToList();
            return View(logs);
        }

        // --- LOG TUTMA YARDIMCISI ---
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
        // --- TÜM BİLDİRİMLERİ GÖR ---
        public IActionResult AllNotifications()
        {
            // 1. Benim Rolüm ve ID'm ne?
            var username = User.Identity.Name;
            var user = _userRepo.GetAll().FirstOrDefault(x => x.Username == username);
            var myRole = user?.Role ?? "User";
            var myId = user?.Id ?? 0;

            // 2. Bana uygun bildirimleri getir (Sistemdeki tümü değil, sadece görmem gerekenler)
            var myNotifs = _notificationRepo.GetAll()
                .Where(n =>
                    n.TargetRole == "All" ||
                    n.TargetRole == myRole ||
                    (n.TargetRole == "Private" && n.TargetUserId == myId)
                )
                .OrderByDescending(n => n.Date) // En yeni en üstte
                .ToList();

            return View(myNotifs);
        }
    }
}