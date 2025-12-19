using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using _20241129402SoruCevapPortali.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IRepository<Notification> _notificationRepo;
        private readonly IHubContext<GeneralHub> _hubContext;

        public SupportController(
            UserManager<AppUser> userManager,
            IRepository<SupportTicket> ticketRepo,
            IRepository<TicketMessage> messageRepo,
            IRepository<Notification> notificationRepo,
            IHubContext<GeneralHub> hubContext
            )
        {
            _userManager = userManager;
            _ticketRepo = ticketRepo;
            _messageRepo = messageRepo;
            _notificationRepo = notificationRepo;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var myTickets = _ticketRepo.GetAll()
                                       .Where(x => x.UserId == user.Id)
                                       .OrderByDescending(x => x.CreatedDate)
                                       .ToList();
            return View(myTickets);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(string subject, string message)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(message))
            {
                var ticket = new SupportTicket { UserId = user.Id, Subject = subject, CreatedDate = DateTime.Now, IsClosed = false };
                _ticketRepo.Add(ticket);

                _messageRepo.Add(new TicketMessage { SupportTicketId = ticket.Id, SenderId = user.Id, Content = message, Date = DateTime.Now });

                _notificationRepo.Add(new Notification { Message = $"{user.UserName} yeni bir destek talebi oluşturdu: {subject}", TargetRole = "Admin", SenderName = "Sistem", Date = DateTime.Now });
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Yeni Destek Talebi Geldi!");

                return RedirectToAction("Index");
            }
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var ticket = _ticketRepo.GetById(id);
            if (ticket == null || ticket.UserId != user.Id) return RedirectToAction("Index");

            var messages = _messageRepo.GetAll().Where(x => x.SupportTicketId == id).OrderBy(x => x.Date).ToList();
            ViewBag.Messages = messages;
            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> Reply(int id, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            var ticket = _ticketRepo.GetById(id);

            if (ticket != null && ticket.UserId == user.Id && !ticket.IsClosed && !string.IsNullOrEmpty(content))
            {
                _messageRepo.Add(new TicketMessage { SupportTicketId = id, SenderId = user.Id, Content = content, Date = DateTime.Now });

                _notificationRepo.Add(new Notification { Message = $"{user.UserName} destek talebine cevap yazdı.", TargetRole = "Admin", SenderName = "Sistem", Date = DateTime.Now });

                // SIGNALR TETİKLEME
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Destek talebine cevap geldi!");
            }
            return RedirectToAction("Details", new { id = id });
        }
    }
}