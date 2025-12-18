using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace _20241129402SoruCevapPortali.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IRepository<Question> _questionRepo;
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<SupportTicket> _ticketRepo; // EKLENDİ
        private readonly IRepository<TicketMessage> _messageRepo; // EKLENDİ
        private readonly IRepository<UserReport> _reportRepo; // EKLENDİ
        private readonly IRepository<Notification> _notificationRepo; // EKLENDİ

        public AdminController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IRepository<Question> questionRepo,
            IRepository<Category> categoryRepo,
            IRepository<SupportTicket> ticketRepo, // EKLENDİ
            IRepository<TicketMessage> messageRepo, // EKLENDİ
            IRepository<UserReport> reportRepo, // EKLENDİ
            IRepository<Notification> notificationRepo // EKLENDİ
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _questionRepo = questionRepo;
            _categoryRepo = categoryRepo;
            _ticketRepo = ticketRepo;
            _messageRepo = messageRepo;
            _reportRepo = reportRepo;
            _notificationRepo = notificationRepo;
        }

        // --- DASHBOARD ---
        // --- DASHBOARD (İSTATİSTİKLER) ---
        public IActionResult Index()
        {
            // 1. Toplam Üye Sayısı
            ViewBag.TotalUsers = _userManager.Users.Count();

            // 2. Toplam Soru Sayısı
            ViewBag.TotalQuestions = _questionRepo.GetAll().Count();

            // 3. Bekleyen (İncelenmemiş) Rapor Sayısı
            ViewBag.PendingReports = _reportRepo.GetAll().Count(x => !x.IsReviewed);

            // 4. Açık (Cevap Bekleyen) Destek Talebi Sayısı
            ViewBag.OpenTickets = _ticketRepo.GetAll().Count(x => !x.IsClosed);

            return View();
        }

        // --- KULLANICI YÖNETİMİ ---
        public IActionResult UserList()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("UserList");
        }

        // --- KATEGORİ YÖNETİMİ ---
        public IActionResult CategoryList()
        {
            var values = _categoryRepo.GetAll();
            return View(values);
        }

        [HttpGet]
        public IActionResult AddCategory() => View();

        [HttpPost]
        public IActionResult AddCategory(Category p)
        {
            _categoryRepo.Add(p);
            return RedirectToAction("CategoryList");
        }

        public IActionResult DeleteCategory(int id)
        {
            var cat = _categoryRepo.GetById(id);
            if (cat != null) _categoryRepo.Delete(cat);
            return RedirectToAction("CategoryList");
        }

        // --- DESTEK TALEPLERİ (YENİ EKLENEN) ---
        public IActionResult SupportList()
        {
            var tickets = _ticketRepo.GetAll().Where(x => !x.IsClosed).OrderBy(x => x.CreatedDate).ToList();
            return View(tickets);
        }

        public IActionResult SupportDetails(int id)
        {
            var ticket = _ticketRepo.GetById(id);
            var messages = _messageRepo.GetAll().Where(x => x.SupportTicketId == id).OrderBy(x => x.Date).ToList();
            ViewBag.Messages = messages;
            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> AdminReply(int id, string content, bool closeTicket)
        {
            var admin = await _userManager.GetUserAsync(User);
            var ticket = _ticketRepo.GetById(id);

            if (ticket != null)
            {
                // 1. Admin Cevabını Kaydet
                var msg = new TicketMessage
                {
                    SupportTicketId = id,
                    SenderId = admin.Id,
                    Content = content
                };
                _messageRepo.Add(msg);

                // 2. Kapatılacaksa kapat
                if (closeTicket)
                {
                    ticket.IsClosed = true;
                    _ticketRepo.Update(ticket);
                }

                // 3. Kullanıcıya BİLDİRİM Gönder
                var notif = new Notification
                {
                    Message = $"Destek talebine cevap geldi: {ticket.Subject}",
                    TargetUserId = ticket.UserId,
                    TargetRole = "User",
                    SenderName = "Yönetim",
                    Date = System.DateTime.Now
                };
                _notificationRepo.Add(notif);
            }
            return RedirectToAction("SupportList");
        }

        // --- RAPORLAR (YENİ EKLENEN) ---
        public IActionResult ReportList()
        {
            var reports = _reportRepo.GetAll().Where(x => !x.IsReviewed).OrderByDescending(x => x.Date).ToList();
            return View(reports);
        }

        public IActionResult ReviewReport(int id)
        {
            var report = _reportRepo.GetById(id);
            if (report != null)
            {
                report.IsReviewed = true;
                _reportRepo.Update(report);
            }
            return RedirectToAction("ReportList");
        }
    }
}