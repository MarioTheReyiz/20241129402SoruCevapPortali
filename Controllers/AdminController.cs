using Microsoft.AspNetCore.Mvc;
using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace _20241129402SoruCevapPortali.Controllers
{
    public class AdminController : Controller
    {
        // Tüm Repository'leri içeri alıyoruz
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Question> _questionRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Answer> _answerRepo;

        public AdminController(IRepository<Category> categoryRepo, IRepository<Question> questionRepo, IRepository<User> userRepo, IRepository<Answer> answerRepo)
        {
            _categoryRepo = categoryRepo;
            _questionRepo = questionRepo;
            _userRepo = userRepo;
            _answerRepo = answerRepo;
        }

        public IActionResult Index() => View();

        // --- 1. KATEGORİ YÖNETİMİ ---
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

        // --- 2. SORU YÖNETİMİ (Listeleme, Ekleme, Düzenleme, Silme) ---
        public IActionResult Questions() => View(_questionRepo.GetAll());

        [HttpPost]
        public IActionResult DeleteQuestion(int id)
        {
            var item = _questionRepo.GetById(id);
            if (item != null) { _questionRepo.Delete(item); return Json(new { success = true }); }
            return Json(new { success = false });
        }

        // Soru Düzenleme Sayfasını Getir
        [HttpGet]
        public IActionResult EditQuestion(int id)
        {
            var question = _questionRepo.GetById(id);
            if (question == null) return RedirectToAction("Questions");

            // Kategorileri Dropdown için hazırla
            ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "Id", "Name");
            return View(question);
        }

        // Soru Düzenlemeyi Kaydet
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
                _questionRepo.Update(existing); // Repository'de Update metodu olmalı
            }
            return RedirectToAction("Questions");
        }

        // Yeni Soru Ekleme Sayfası
        [HttpGet]
        public IActionResult AddQuestion()
        {
            ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public IActionResult AddQuestion(Question p)
        {
            // Admin eklediği için onaylı olsun ve adminin ID'si atansın (Varsayılan Admin ID: 1 kabul ettik)
            p.UserId = 1;
            p.CreatedDate = DateTime.Now;
            p.IsApproved = true;
            _questionRepo.Add(p);
            return RedirectToAction("Questions");
        }

        // --- 3. KULLANICI YÖNETİMİ ---
        public IActionResult Users()
        {
            var users = _userRepo.GetAll();
            return View(users);
        }

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var item = _userRepo.GetById(id);
            if (item != null) { _userRepo.Delete(item); return Json(new { success = true }); }
            return Json(new { success = false });
        }

        // --- 4. CEVAP YÖNETİMİ ---
        public IActionResult Answers()
        {
            var answers = _answerRepo.GetAll();
            return View(answers);
        }

        [HttpPost]
        public IActionResult DeleteAnswer(int id)
        {
            var item = _answerRepo.GetById(id);
            if (item != null) { _answerRepo.Delete(item); return Json(new { success = true }); }
            return Json(new { success = false });
        }

        // --- 5. PROFİL SAYFASI ---
        public IActionResult Profile()
        {
            // Şimdilik ID'si 1 olan admini getiriyoruz
            var admin = _userRepo.GetById(1);
            return View(admin);
        }

        [HttpPost]
        public IActionResult Profile(User p)
        {
            var existing = _userRepo.GetById(p.Id);
            if (existing != null)
            {
                existing.Username = p.Username;
                existing.Password = p.Password; // Şifreyi günceller
                _userRepo.Update(existing);
            }
            return RedirectToAction("Profile");
        }
    }
}