using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace _20241129402SoruCevapPortali.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IRepository<SupportTicket> _ticketRepo;
        private readonly IRepository<TicketMessage> _messageRepo;
        // Repository'leri IdentityDbContext veya GenericRepo üzerinden bağladığını varsayıyorum
        // Eğer ayrı repo yapmadıysan DbContext'i direkt kullanabilirsin. 
        // Burada senin Repository yapına uygun yazıyorum:

        public SupportController(UserManager<AppUser> u, IRepository<SupportTicket> t, IRepository<TicketMessage> m)
        {
            _userManager = u;
            _ticketRepo = t;
            _messageRepo = m;
        }

        // Taleplerim Listesi
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var tickets = _ticketRepo.GetAll().Where(x => x.UserId == user.Id).OrderByDescending(x => x.CreatedDate).ToList();
            return View(tickets);
        }

        // Yeni Talep Oluştur
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(string subject, string message)
        {
            var user = await _userManager.GetUserAsync(User);

            // 1. Bileti Oluştur
            var ticket = new SupportTicket { Subject = subject, UserId = user.Id };
            _ticketRepo.Add(ticket);

            // 2. İlk Mesajı Ekle
            var msg = new TicketMessage
            {
                SupportTicketId = ticket.Id,
                SenderId = user.Id,
                Content = message
            };
            _messageRepo.Add(msg);

            return RedirectToAction("Index");
        }

        // Mesajlaşma Ekranı
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var ticket = _ticketRepo.GetById(id);

            // Başkası başkasının talebini görmesin
            if (ticket == null || ticket.UserId != user.Id) return RedirectToAction("Index");

            var messages = _messageRepo.GetAll().Where(x => x.SupportTicketId == id).OrderBy(x => x.Date).ToList();
            ViewBag.Messages = messages;

            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> AddMessage(int id, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            var ticket = _ticketRepo.GetById(id);

            if (ticket != null && !ticket.IsClosed)
            {
                var msg = new TicketMessage
                {
                    SupportTicketId = id,
                    SenderId = user.Id,
                    Content = content
                };
                _messageRepo.Add(msg);
            }
            return RedirectToAction("Details", new { id = id });
        }
    }
}