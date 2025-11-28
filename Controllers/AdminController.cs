using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;

namespace _20241129402SoruCevapPortali.Controllers
{
    public class AdminController : Controller
    {
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Question> _questionRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Answer> _answerRepo;

        public AdminController(IRepository<Category> c, IRepository<Question> q, IRepository<User> u, IRepository<Answer> a)
        {
            _categoryRepo = c; _questionRepo = q; _userRepo = u; _answerRepo = a;
        }

        public IActionResult Index() => View();

        // --- KATEGORİLER ---
        public IActionResult Categories() => View(_categoryRepo.GetAll());

        [HttpPost]
        public IActionResult AddCategory(Category p)
        {
            if (ModelState.IsValid) _categoryRepo.Add(p);
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public IActionResult DeleteCategory(int id)
        {
            var item = _categoryRepo.GetById(id);
            if (item != null) { _categoryRepo.Delete(item); return Json(new { success = true }); }
            return Json(new { success = false });
        }

        // --- KULLANICILAR ---
        public IActionResult Users() => View(_userRepo.GetAll());

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var item = _userRepo.GetById(id);
            if (item != null) { _userRepo.Delete(item); return Json(new { success = true }); }
            return Json(new { success = false });
        }

        // --- SORULAR (Ekle, Düzenle, Sil) ---
        public IActionResult Questions() => View(_questionRepo.GetAll());

        [HttpPost]
        public IActionResult DeleteQuestion(int id)
        {
            var item = _questionRepo.GetById(id);
            if (item != null) { _questionRepo.Delete(item); return Json(new { success = true }); }
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
            p.UserId = 1; // Varsayılan Admin ID'si (Vize için yeterli)
            _questionRepo.Add(p);
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
            }
            return RedirectToAction("Questions");
        }

        // --- CEVAPLAR ---
        public IActionResult Answers() => View(_answerRepo.GetAll());

        [HttpPost]
        public IActionResult DeleteAnswer(int id)
        {
            var item = _answerRepo.GetById(id);
            if (item != null) { _answerRepo.Delete(item); return Json(new { success = true }); }
            return Json(new { success = false });
        }

        // --- PROFİL ---
        public IActionResult Profile()
        {
            var admin = _userRepo.GetById(1); // İlk kullanıcıyı getir
            return View(admin);
        }

        [HttpPost]
        public IActionResult Profile(User p)
        {
            var user = _userRepo.GetById(p.Id);
            if (user != null)
            {
                user.Username = p.Username;
                user.Password = p.Password;
                _userRepo.Update(user);
            }
            return RedirectToAction("Profile");
        }
    }
}