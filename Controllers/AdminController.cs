using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _20241129402SoruCevapPortali.Controllers
{
    [Authorize(Roles = "Admin,Moderator")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // Repositories
        private readonly IRepository<Question> _questionRepo;
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Answer> _answerRepo;
        private readonly IRepository<Log> _logRepo;
        private readonly IRepository<SupportTicket> _ticketRepo;
        private readonly IRepository<TicketMessage> _messageRepo;
        private readonly IRepository<UserReport> _reportRepo;
        private readonly IRepository<Notification> _notificationRepo;
        private readonly AppDbContext _context;

        public AdminController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IRepository<Question> questionRepo,
            IRepository<Category> categoryRepo,
            IRepository<Answer> answerRepo,
            IRepository<Log> logRepo,
            IRepository<SupportTicket> ticketRepo,
            IRepository<TicketMessage> messageRepo,
            IRepository<UserReport> reportRepo,
            IRepository<Notification> notificationRepo,
            AppDbContext context
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _questionRepo = questionRepo;
            _categoryRepo = categoryRepo;
            _answerRepo = answerRepo;
            _logRepo = logRepo;
            _ticketRepo = ticketRepo;
            _messageRepo = messageRepo;
            _reportRepo = reportRepo;
            _notificationRepo = notificationRepo;
            _context = context;
        }
        public IActionResult Index()
        {
            ViewBag.TotalUsers = _userManager.Users.Count();
            ViewBag.TotalQuestions = _questionRepo.GetAll().Count();

            var allReports = _reportRepo.GetAll();
            ViewBag.PendingReports = allReports != null ? allReports.Count(x => !x.IsReviewed) : 0;

            var allTickets = _ticketRepo.GetAll();
            ViewBag.OpenTickets = allTickets != null ? allTickets.Count(x => !x.IsClosed) : 0;

            var recentReports = _reportRepo.GetAll()
                                           .Where(x => !x.IsReviewed)
                                           .OrderByDescending(x => x.Date)
                                           .Take(5)
                                           .ToList();

            return View(recentReports);
        }
        public IActionResult Users()
        {
            return View(_userManager.Users.ToList());
        }
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            // --- GÜVENLİK KONTROLÜ BAŞLANGIÇ ---
            // Şu an sisteme giriş yapmış olan kullanıcının ID'sini alıyoruz
            var currentUserId = _userManager.GetUserId(User);

            // Eğer silinmeye çalışılan ID (id), giriş yapan kişinin ID'si (currentUserId) ise DUR.
            if (id == currentUserId)
            {
                return Json(new { success = false, message = "Kendi hesabınızı silemezsiniz!" });
            }
            // ------------------------------------

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            try
            {
                // 1. Cevapları Sil
                var answers = _context.Answers.Where(x => x.UserId == id).ToList();
                _context.Answers.RemoveRange(answers);

                // 2. Soruları Sil
                var questions = _context.Questions.Where(x => x.UserId == id).ToList();
                _context.Questions.RemoveRange(questions);

                // 3. Beğenileri Sil
                var likes = _context.Likes.Where(x => x.UserId == id).ToList();
                _context.Likes.RemoveRange(likes);

                // 4. Bildirimleri Sil (Model adı: TargetUserId)
                var notifications = _context.Notifications.Where(x => x.TargetUserId == id).ToList();
                _context.Notifications.RemoveRange(notifications);

                // 5. Logları Sil (Model adı: Username)
                var logs = _context.Logs.Where(x => x.Username == user.UserName).ToList();
                _context.Logs.RemoveRange(logs);

                // Değişiklikleri Kaydet
                await _context.SaveChangesAsync();

                // --- KULLANICIYI SİL ---
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Kullanıcı ve tüm verileri başarıyla silindi." });
                }
                else
                {
                    string errors = string.Join(" ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = "Silinemedi: " + errors });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }
        public IActionResult Questions()
        {
            var questions = _questionRepo.GetAll().OrderByDescending(x => x.CreatedDate).ToList();
            var users = _userManager.Users.ToDictionary(u => u.Id, u => u);
            ViewBag.Users = users;

            return View(questions);
        }

        public IActionResult ForumDetails(int id)
        {
            var question = _questionRepo.GetById(id);
            if (question == null) return RedirectToAction("Questions");

            ViewBag.Answers = _answerRepo.GetAll().Where(x => x.QuestionId == id).OrderBy(x => x.Date).ToList();
            var users = _userManager.Users.ToDictionary(u => u.Id, u => u);
            ViewBag.Users = users;

            return View(question);
        }

        public async Task<IActionResult> DeleteQuestion(int id)
        {
            // 1. Soruyu Bul
            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            // 2. SORUYA AİT CEVAPLARI SİL (Önce cevaplar, sonra soru)
            var answers = _context.Answers.Where(x => x.QuestionId == id).ToList();
            if (answers.Any())
            {
                _context.Answers.RemoveRange(answers);
            }

            // 3. Varsa Beğenileri Sil (Eğer Like tablon varsa ve QuestionId tutuyorsa)
            // var likes = _context.Likes.Where(x => x.QuestionId == id).ToList();
            // _context.Likes.RemoveRange(likes);

            // 4. Soruyu Sil
            _context.Questions.Remove(question);

            // 5. Değişiklikleri Kaydet
            await _context.SaveChangesAsync();

            // 6. JSON yerine Listeye Geri Dön (Sayfa yenilensin)
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
            var question = _questionRepo.GetById(p.Id);
            if (question != null)
            {
                question.Title = p.Title;
                question.Content = p.Content;
                question.CategoryId = p.CategoryId;
                question.IsApproved = p.IsApproved;
                _questionRepo.Update(question);
            }
            return RedirectToAction("Questions");
        }
        public IActionResult Categories()
        {
            return View(_categoryRepo.GetAll());
        }

        [HttpGet]
        public IActionResult AddCategory() => View();

        [HttpPost]
        public IActionResult AddCategory(Category p)
        {
            _categoryRepo.Add(p);
            return RedirectToAction("Categories");
        }

        public IActionResult DeleteCategory(int id)
        {
            var cat = _categoryRepo.GetById(id);
            if (cat != null) _categoryRepo.Delete(cat);
            return RedirectToAction("Categories");
        }
        [HttpGet]
        public IActionResult SendNotification()
        {
            var users = _userManager.Users.Select(x => new SelectListItem
            {
                Text = $"{x.UserName} ({x.Email})",
                Value = x.Id
            }).ToList();
            users.Insert(0, new SelectListItem { Text = "", Value = "" });
            ViewBag.UserList = users;
            return View();
        }

        [HttpPost]
        public IActionResult SendNotification(string message, string targetRole, string specificUserId)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (!string.IsNullOrEmpty(specificUserId))
                {
                    _notificationRepo.Add(new Notification { Message = message, TargetUserId = specificUserId, TargetRole = "Personal", SenderName = "Yönetim", Date = DateTime.Now });
                }
                else
                {
                    _notificationRepo.Add(new Notification { Message = message, TargetRole = targetRole ?? "All", SenderName = "Yönetim", Date = DateTime.Now });
                }
                TempData["Success"] = "Bildirim başarıyla gönderildi.";
            }
            return RedirectToAction("AllNotifications");
        }

        public IActionResult AllNotifications()
        {
            var unread = _notificationRepo.GetAll().Where(x => !x.IsRead).ToList();
            foreach (var item in unread)
            {
                item.IsRead = true;
                _notificationRepo.Update(item);
            }

            var list = _notificationRepo.GetAll().OrderByDescending(x => x.Date).ToList();
            return View(list);
        }
        public IActionResult SupportList()
        {
            var tickets = _ticketRepo.GetAll().Where(x => !x.IsClosed).OrderBy(x => x.CreatedDate).ToList();
            return View(tickets);
        }

        public IActionResult SupportDetails(int id)
        {
            var ticket = _ticketRepo.GetById(id);
            if (ticket == null) return RedirectToAction("SupportList");

            var messages = _messageRepo.GetAll().Where(x => x.SupportTicketId == id).OrderBy(x => x.Date).ToList();
            ViewBag.Messages = messages;
            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> AdminReply(int id, string content, bool closeTicket)
        {
            var admin = await _userManager.GetUserAsync(User);
            var ticket = _ticketRepo.GetById(id);

            if (ticket != null && !string.IsNullOrEmpty(content))
            {
                _messageRepo.Add(new TicketMessage { SupportTicketId = id, SenderId = admin.Id, Content = content });
                if (closeTicket) { ticket.IsClosed = true; _ticketRepo.Update(ticket); }

                _notificationRepo.Add(new Notification { Message = $"Destek talebiniz yanıtlandı: {ticket.Subject}", TargetUserId = ticket.UserId, TargetRole = "User", SenderName = "Destek Ekibi", Date = DateTime.Now });
            }
            return RedirectToAction("SupportList");
        }

        public IActionResult ReportList()
        {
            var reports = _reportRepo.GetAll().Where(x => !x.IsReviewed).OrderByDescending(x => x.Date).ToList();
            return View(reports);
        }

        public IActionResult ReviewReport(int id)
        {
            var report = _reportRepo.GetById(id);
            if (report != null) { report.IsReviewed = true; _reportRepo.Update(report); }
            return RedirectToAction("ReportList");
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult RoleManagement()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateRole(string id, string newRole)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser.Id == id) return Json(new { success = false, message = "GÜVENLİK UYARISI: Kendi yetkinizi değiştiremezsiniz!" });

            var targetUser = await _userManager.FindByIdAsync(id);
            if (targetUser == null) return Json(new { success = false, message = "Kullanıcı bulunamadı!" });

            var currentRoles = await _userManager.GetRolesAsync(targetUser);
            await _userManager.RemoveFromRolesAsync(targetUser, currentRoles);

            if (!await _roleManager.RoleExistsAsync(newRole)) await _roleManager.CreateAsync(new IdentityRole(newRole));

            await _userManager.AddToRoleAsync(targetUser, newRole);

            return Json(new { success = true, message = $"{targetUser.UserName} kullanıcısı artık {newRole} yetkisine sahip." });
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Logs()
        {
            return View(_logRepo.GetAll().OrderByDescending(x => x.Date).ToList());
        }
        public IActionResult Answers()
        {
            var answers = _answerRepo.GetAll().OrderByDescending(x => x.Date).ToList();
            var users = _userManager.Users.ToDictionary(u => u.Id, u => u);
            ViewBag.Users = users;
            return View(answers);
        }

        public IActionResult DeleteAnswer(int id)
        {
            var ans = _answerRepo.GetById(id);
            if (ans != null) _answerRepo.Delete(ans);
            return RedirectToAction("Answers");
        }
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }
    }
}