using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _20241129402SoruCevapPortali.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRepository<Question> _questionRepo;
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Answer> _answerRepo;
        private readonly IRepository<Notification> _notificationRepo;
        private readonly IRepository<Like> _likeRepo;
        private readonly UserManager<AppUser> _userManager;
        public HomeController(
            IRepository<Question> q,
            IRepository<Category> c,
            IRepository<Answer> a,
            UserManager<AppUser> u,
            IRepository<Notification> n,
            IRepository<Like> l
            )
        {
            _questionRepo = q;
            _categoryRepo = c;
            _answerRepo = a;
            _userManager = u;
            _notificationRepo = n;
            _likeRepo = l;
        }
        public IActionResult Index(string search, int? categoryId, string sortOrder)
        {
            ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "Id", "Name");
            var query = _questionRepo.GetAll().Where(x => x.IsApproved);
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(x => x.Title.ToLower().Contains(search) || x.Content.ToLower().Contains(search));
            }
            if (categoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == categoryId.Value);
            }
            switch (sortOrder)
            {
                case "likes":
                    query = query.OrderByDescending(x => x.LikeCount);
                    break;
                case "alpha":
                    query = query.OrderBy(x => x.Title);
                    break;
                case "date_asc":
                    query = query.OrderBy(x => x.CreatedDate);
                    break;
                default:
                    query = query.OrderByDescending(x => x.CreatedDate);
                    break;
            }

            var resultList = query.ToList();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.Users = _userManager.Users.ToDictionary(k => k.Id, v => v);

            return View(resultList);
        }
        public IActionResult Details(int id)
        {
            var question = _questionRepo.GetById(id);
            if (question == null) return NotFound();
            var answers = _answerRepo.GetAll().Where(x => x.QuestionId == id).OrderBy(x => x.Date).ToList();
            ViewBag.Answers = answers;
            var category = _categoryRepo.GetById(question.CategoryId);
            ViewBag.CategoryName = category?.Name;
            ViewBag.Users = _userManager.Users.ToDictionary(k => k.Id, v => v);

            return View(question);
        }
        [Authorize]
        public IActionResult CreateQuestion()
        {
            ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateQuestion(Question p)
        {
            var user = await _userManager.GetUserAsync(User);
            p.UserId = user.Id;
            p.CreatedDate = DateTime.Now;
            p.IsApproved = true;

            _questionRepo.Add(p);
            await AddXp(user.Id, 15);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddAnswer(int questionId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && !string.IsNullOrEmpty(content))
            {
                var answer = new Answer
                {
                    QuestionId = questionId,
                    Content = content,
                    UserId = user.Id,
                    Date = DateTime.Now
                };
                _answerRepo.Add(answer);
                await AddXp(user.Id, 10);
            }
            return RedirectToAction("Details", new { id = questionId });
        }

        [Authorize]
        public async Task<IActionResult> MyQuestions()
        {
            var user = await _userManager.GetUserAsync(User);
            var myQuestions = _questionRepo.GetAll()
                                           .Where(x => x.UserId == user.Id)
                                           .OrderByDescending(x => x.CreatedDate)
                                           .ToList();
            return View(myQuestions);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = _questionRepo.GetById(id);
            var user = await _userManager.GetUserAsync(User);
            if (question != null && (question.UserId == user.Id || User.IsInRole("Admin")))
            {
                var answers = _answerRepo.GetAll().Where(x => x.QuestionId == id).ToList();
                foreach (var ans in answers) _answerRepo.Delete(ans);
                _questionRepo.Delete(question);
                return RedirectToAction("Index");
            }
            return RedirectToAction("Details", new { id = id });
        }
        [Authorize]
        public async Task<IActionResult> LikeQuestion(int id)
        {
            var me = await _userManager.GetUserAsync(User);
            var question = _questionRepo.GetById(id);

            if (question != null && me != null)
            {
                var existingLike = _likeRepo.GetAll().FirstOrDefault(x => x.UserId == me.Id && x.QuestionId == id);
                if (existingLike == null)
                {
                    var newLike = new Like { UserId = me.Id, QuestionId = id, Date = DateTime.Now };
                    _likeRepo.Add(newLike);

                    question.LikeCount++;
                    _questionRepo.Update(question);
                    await AddXp(question.UserId, 5);
                }
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [Authorize]
        public async Task<IActionResult> LikeAnswer(int id)
        {
            var me = await _userManager.GetUserAsync(User);
            var answer = _answerRepo.GetById(id);

            if (answer != null && me != null)
            {
                var existingLike = _likeRepo.GetAll().FirstOrDefault(x => x.UserId == me.Id && x.AnswerId == id);
                if (existingLike == null)
                {
                    var newLike = new Like { UserId = me.Id, AnswerId = id, Date = DateTime.Now };
                    _likeRepo.Add(newLike);

                    answer.LikeCount++;
                    _answerRepo.Update(answer);
                    await AddXp(answer.UserId, 5);
                }
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }
        [Authorize]
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            var notifs = _notificationRepo.GetAll()
                .Where(x => (x.TargetUserId == user.Id || x.TargetRole == "All") && x.Date <= DateTime.Now)
                .OrderByDescending(x => x.Date)
                .ToList();
            return View(notifs);
        }

        [Authorize]
        [HttpPost]
        public IActionResult MarkAsRead(int id)
        {
            var notif = _notificationRepo.GetById(id);
            if (notif != null)
            {
                notif.IsRead = true;
                _notificationRepo.Update(notif);
            }
            return Json(new { success = true });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(0);

            var count = _notificationRepo.GetAll()
                .Count(x => (x.TargetUserId == user.Id || x.TargetRole == "All") && !x.IsRead);

            return Json(count);
        }

        [Authorize]
        public async Task<IActionResult> UserProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index");
            var soruSayisi = _questionRepo.GetAll().Count(x => x.UserId == user.Id);
            var cevapSayisi = _answerRepo.GetAll().Count(x => x.UserId == user.Id);
            var soruLikelari = _questionRepo.GetAll().Where(x => x.UserId == user.Id).Sum(x => x.LikeCount);
            var cevapLikelari = _answerRepo.GetAll().Where(x => x.UserId == user.Id).Sum(x => x.LikeCount);
            var toplamBegeni = soruLikelari + cevapLikelari;
            ViewBag.SoruSayisi = soruSayisi;
            ViewBag.CevapSayisi = cevapSayisi;
            ViewBag.ToplamBegeni = toplamBegeni;

            return View(user);
        }

        public IActionResult Privacy() => View();
        private async Task AddXp(string userId, int amount)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.ExperiencePoints += amount;
                int calculatedLevel = 1 + (user.ExperiencePoints / 100);
                if (calculatedLevel > 10) calculatedLevel = 10;

                user.Level = calculatedLevel;
                switch (user.Level)
                {
                    case 1: user.Badge = "Çaylak"; break;
                    case 2: user.Badge = "Bronz"; break;
                    case 3: user.Badge = "Gümüþ"; break;
                    case 4: user.Badge = "Altýn"; break;
                    case 5: user.Badge = "Platin"; break;
                    case 6: user.Badge = "Elmas"; break;
                    case 7: user.Badge = "Usta"; break;
                    case 8: user.Badge = "Grandmaster"; break;
                    case 9: user.Badge = "Efsane"; break;
                    case 10: user.Badge = "MVP"; break;
                    default: user.Badge = "Çaylak"; break;
                }

                await _userManager.UpdateAsync(user);
            }
        }
    }
}