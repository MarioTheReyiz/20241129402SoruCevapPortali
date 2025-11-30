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

        public IActionResult Index()
        {
            // 1. Toplam Kategori Sayısı
            ViewBag.TotalCategories = _categoryRepo.GetAll().Count;

            // 2. Sorularla İlgili İstatistikler
            var questions = _questionRepo.GetAll();
            ViewBag.TotalQuestions = questions.Count;
            ViewBag.PendingQuestions = questions.Count(q => !q.IsApproved); // Onay bekleyenler

            // 3. Cevap Oranı Hesabı
            var answers = _answerRepo.GetAll();
            var totalQuestions = questions.Count;

            if (totalQuestions > 0)
            {
                // En az bir cevabı olan tekil soru sayısını buluyoruz
                var answeredQuestionCount = answers.Select(a => a.QuestionId).Distinct().Count();
                ViewBag.AnswerRate = (int)((double)answeredQuestionCount / totalQuestions * 100);
            }
            else
            {
                ViewBag.AnswerRate = 0;
            }

            return View();
        }

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


        // --- FORUM DETAYI (SORU VE CEVAPLARI GÖRME) ---
        public IActionResult ForumDetails(int id)
        {
            // 1. Soruyu Bul
            var question = _questionRepo.GetById(id);
            if (question == null) return RedirectToAction("Questions");

            // Soruyu soran kullanıcıyı getir (Eğer User null geliyorsa)
            question.User = _userRepo.GetById(question.UserId);

            // 2. Bu soruya ait cevapları filtrele
            // Repository'de "Where" komutu direkt çalışmayabilir, o yüzden GetAll() ile çekip RAM'de filtreliyoruz (Vize için uygundur)
            var answers = _answerRepo.GetAll().Where(x => x.QuestionId == id).ToList();

            // 3. Her cevabın yazarını (User) içine doldur
            foreach (var answer in answers)
            {
                answer.User = _userRepo.GetById(answer.UserId);
            }

            // Cevapları View'a taşımak için ViewBag kullanıyoruz
            ViewBag.Answers = answers;

            return View(question);
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
        [HttpGet]
        public IActionResult Profile()
        {
            // Giriş yapan kullanıcıyı ismine göre bul
            var username = User.Identity.Name;
            var user = _userRepo.GetAll().FirstOrDefault(x => x.Username == username);

            // Eğer kimse yoksa (session koptuysa) yine de admini (Id=1) getir (hata vermesin diye)
            if (user == null) user = _userRepo.GetById(1);

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(User p, IFormFile? ImageFile) // IFormFile parametresi eklendi
        {
            // Giriş yapmış kullanıcıyı bul (İsimden)
            var currentUser = _userRepo.GetAll().FirstOrDefault(x => x.Username == User.Identity.Name);

            // Eğer bulamazsa (ki bulmalı), ID ile gelen p.Id'ye bak
            var userToUpdate = currentUser ?? _userRepo.GetById(p.Id);

            if (userToUpdate != null)
            {
                userToUpdate.Name = p.Name;
                userToUpdate.Surname = p.Surname;
                userToUpdate.Email = p.Email;
                userToUpdate.PhoneNumber = p.PhoneNumber;
                userToUpdate.Password = p.Password; // Şifreyi güncelle

                // --- RESİM YÜKLEME İŞLEMİ ---
                if (ImageFile != null)
                {
                    // Dosya uzantısını al (örn: .jpg)
                    var extension = Path.GetExtension(ImageFile.FileName);
                    // Rastgele yeni bir isim ver (çakışma olmasın)
                    var newImageName = Guid.NewGuid() + extension;
                    // Kaydedilecek yer: wwwroot/img/
                    var location = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/", newImageName);

                    // Dosyayı kaydet
                    using (var stream = new FileStream(location, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    // Veritabanına resim yolunu yaz
                    userToUpdate.PhotoUrl = "/img/" + newImageName;
                }
                // -----------------------------

                _userRepo.Update(userToUpdate);
            }
            return RedirectToAction("Profile");
        }
        // --- RAPOR OLUŞTURMA (TXT İNDİRME) ---
        public IActionResult ExportReport()
        {
            // Verileri çekelim
            var categoryCount = _categoryRepo.GetAll().Count;
            var questions = _questionRepo.GetAll();
            var answerCount = _answerRepo.GetAll().Count;
            var userCount = _userRepo.GetAll().Count;

            // Rapor içeriğini hazırlayalım
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
            sb.AppendLine("Bu rapor sistem tarafından otomatik oluşturulmuştur.");

            // Dosya haline getirip indir
            var content = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(content, "text/plain", $"Yonetim_Raporu_{DateTime.Now.ToString("ddMMyyyy")}.txt");
        }
        // --- YETKİ YÖNETİMİ (SADECE ADMIN) ---

        [HttpGet]
        public IActionResult RoleManagement()
        {
            // GÜVENLİK KONTROLÜ: Sadece "Admin" girebilir!
            if (User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value != "Admin")
            {
                return RedirectToAction("Index"); // Yetkisiz ise dashboard'a at
            }

            var users = _userRepo.GetAll();
            return View(users);
        }

        [HttpPost]
        public IActionResult UpdateRole(int id, string newRole)
        {
            // GÜVENLİK KONTROLÜ: Sadece "Admin" değiştirebilir!
            if (User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value != "Admin")
            {
                return Json(new { success = false, message = "Yetkiniz yok!" });
            }

            var user = _userRepo.GetById(id);
            if (user != null)
            {
                // Kendi yetkini düşürmeyi engelle (Opsiyonel güvenlik)
                if (user.Username == User.Identity.Name)
                {
                    return Json(new { success = false, message = "Kendi yetkinizi değiştiremezsiniz." });
                }

                user.Role = newRole;
                _userRepo.Update(user);
                return Json(new { success = true, message = "Rol güncellendi." });
            }
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }
    }
}