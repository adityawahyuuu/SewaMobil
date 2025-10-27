using System;
using System.Linq;
using System.Web.Mvc;
using SewaMobil.Models;
using SewaMobil.Models.Repository;

namespace SewaMobil.Controllers
{
    [Authorize]
    public class RentalController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public RentalController()
        {
            _unitOfWork = new UnitOfWork(new CarRentalContext());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _unitOfWork.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Cek apakah user sudah login
            if (Session["UserId"] == null)
            {
                filterContext.Result = new RedirectResult("~/Account/Login");
            }
            base.OnActionExecuting(filterContext);
        }

        // GET: Rental/MyRentals
        public ActionResult MyRentals()
        {
            var userId = (int)Session["UserId"];

            // Get rentals with eager loading
            var rentals = _unitOfWork.Rentals
                .Query()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            // Load related data for each rental
            foreach (var rental in rentals)
            {
                rental.Car = _unitOfWork.Cars.GetById(rental.CarId);
            }

            return View(rentals);
        }

        // GET: Rental/Create/5
        public ActionResult Create(int id)
        {
            var car = _unitOfWork.Cars.GetById(id);

            if (car == null)
            {
                TempData["ErrorMessage"] = "Mobil tidak ditemukan.";
                return RedirectToAction("Index", "Home");
            }

            if (!car.IsActive)
            {
                TempData["ErrorMessage"] = "Mobil tidak tersedia.";
                return RedirectToAction("Index", "Home");
            }

            if (car.Status != "Tersedia")
            {
                TempData["ErrorMessage"] = "Mobil sedang disewa oleh orang lain.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Car = car;
            ViewBag.MinDate = DateTime.Now.Date.AddDays(1).ToString("yyyy-MM-dd");

            return View();
        }

        // POST: Rental/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int carId, DateTime rentalDate, DateTime returnDate, string notes)
        {
            var car = _unitOfWork.Cars.GetById(carId);

            // Validasi mobil
            if (car == null || !car.IsActive)
            {
                TempData["ErrorMessage"] = "Mobil tidak ditemukan atau tidak aktif.";
                return RedirectToAction("Index", "Home");
            }

            if (car.Status != "Tersedia")
            {
                TempData["ErrorMessage"] = "Mobil tidak tersedia untuk disewa.";
                return RedirectToAction("Index", "Home");
            }

            // Validasi tanggal sewa
            if (rentalDate.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("rentalDate", "Tanggal sewa tidak boleh kurang dari hari ini.");
                ViewBag.Car = car;
                ViewBag.MinDate = DateTime.Now.Date.AddDays(1).ToString("yyyy-MM-dd");
                return View();
            }

            // Validasi tanggal kembali
            if (returnDate.Date <= rentalDate.Date)
            {
                ModelState.AddModelError("returnDate", "Tanggal kembali harus lebih dari tanggal sewa.");
                ViewBag.Car = car;
                ViewBag.MinDate = DateTime.Now.Date.AddDays(1).ToString("yyyy-MM-dd");
                return View();
            }

            // Validasi maksimal sewa (opsional, misal max 30 hari)
            var totalDays = (returnDate.Date - rentalDate.Date).Days;
            if (totalDays > 30)
            {
                ModelState.AddModelError("returnDate", "Maksimal sewa adalah 30 hari.");
                ViewBag.Car = car;
                ViewBag.MinDate = DateTime.Now.Date.AddDays(1).ToString("yyyy-MM-dd");
                return View();
            }

            // Hitung total harga
            var totalPrice = totalDays * car.DailyRate;

            var userId = (int)Session["UserId"];

            try
            {
                var conn = _unitOfWork.Context.Database.Connection;
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                _unitOfWork.BeginTransaction();

                // Buat rental baru
                var rental = new Rental
                {
                    UserId = userId,
                    CarId = carId,
                    RentalDate = rentalDate.Date,
                    ReturnDate = returnDate.Date,
                    TotalDays = totalDays,
                    TotalPrice = totalPrice,
                    Status = "Active",
                    Notes = !string.IsNullOrEmpty(notes) ? Server.HtmlEncode(notes.Trim()) : null,
                    CreatedAt = DateTime.Now
                };

                _unitOfWork.Rentals.Add(rental);

                // Update status mobil menjadi "Disewa"
                car.Status = "Disewa";
                car.UpdatedAt = DateTime.Now;
                _unitOfWork.Cars.Update(car);

                // Save changes to get RentalId
                _unitOfWork.SaveChanges();

                // Catat history (after rental is saved and has ID)
                var history = new RentalHistory
                {
                    RentalId = rental.RentalId,
                    Action = "Rental Created",
                    ActionDate = DateTime.Now,
                    ActionBy = userId,
                    Notes = "Penyewaan mobil dibuat"
                };
                _unitOfWork.RentalHistories.Add(history);

                // Save history
                _unitOfWork.SaveChanges();
                _unitOfWork.Commit();

                TempData["SuccessMessage"] = string.Format(
                    "Penyewaan berhasil dibuat! Total: Rp {0:N0} untuk {1} hari.",
                    totalPrice,
                    totalDays
                );
                return RedirectToAction("Details", new { id = rental.RentalId });
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();

                var inner = ex;
                while (inner.InnerException != null)
                    inner = inner.InnerException;

                System.Diagnostics.Debug.WriteLine("Create Error: " + inner.Message);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat membuat penyewaan.";
                return View();
            }
        }

        // GET: Rental/Details/5
        public ActionResult Details(int id)
        {
            var rental = _unitOfWork.Rentals.GetById(id);
            
            if (rental == null)
            {
                TempData["ErrorMessage"] = "Penyewaan tidak ditemukan.";
                return RedirectToAction("MyRentals");
            }

            var userId = (int)Session["UserId"];
            var userRole = Session["UserRole"].ToString();

            // Cek authorization: hanya pemilik rental atau admin yang bisa lihat
            if (rental.UserId != userId && userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses ke halaman ini.";
                return RedirectToAction("MyRentals");
            }

            // Load related data
            rental.User = _unitOfWork.Users.GetById(rental.UserId);
            rental.Car = _unitOfWork.Cars.GetById(rental.CarId);

            // Load history
            var histories = _unitOfWork.RentalHistories
                .Query()
                .Where(h => h.RentalId == id)
                .OrderByDescending(h => h.ActionDate)
                .ToList();

            foreach (var history in histories)
            {
                if (history.ActionBy.HasValue)
                {
                    history.ActionByUser = _unitOfWork.Users.GetById(history.ActionBy.Value);
                }
            }

            ViewBag.Histories = histories;
            return View(rental);
        }

        // GET: Rental/Return/5
        public ActionResult Return(int id)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            var rental = _unitOfWork.Rentals.GetById(id);

            if (rental == null)
            {
                TempData["ErrorMessage"] = "Penyewaan tidak ditemukan.";
                return RedirectToAction("MyRentals");
            }

            var userId = (int)Session["UserId"];

            if (rental.UserId != userId)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses ke halaman ini.";
                return RedirectToAction("MyRentals");
            }

            if (rental.Status != "Active")
            {
                TempData["ErrorMessage"] = "Penyewaan ini sudah tidak aktif.";
                return RedirectToAction("Details", new { id = rental.RentalId });
            }

            rental.Car = _unitOfWork.Cars.GetById(rental.CarId);

            return View(rental);
        }

        // POST: Rental/Return
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Return(int id, string notes)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            var rental = _unitOfWork.Rentals.GetById(id);
            if (rental == null || rental.Status != "Active")
                return RedirectToAction("MyRentals");

            var userId = (int)Session["UserId"];

            try
            {
                var conn = _unitOfWork.Context.Database.Connection;
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                _unitOfWork.BeginTransaction();

                var now = DateTime.Now;
                rental.ActualReturnDate = now;
                rental.Status = "Completed";
                rental.UpdatedAt = now;

                if (!string.IsNullOrEmpty(notes))
                {
                    var sanitized = Server.HtmlEncode(notes.Trim());
                    rental.Notes = string.IsNullOrEmpty(rental.Notes)
                        ? sanitized
                        : rental.Notes + " | Pengembalian: " + sanitized;
                }

                _unitOfWork.Rentals.Update(rental);

                var car = _unitOfWork.Cars.GetById(rental.CarId);
                if (car != null)
                {
                    car.Status = "Tersedia";
                    car.UpdatedAt = now;
                    _unitOfWork.Cars.Update(car);
                }

                _unitOfWork.SaveChanges();

                var history = new RentalHistory
                {
                    RentalId = rental.RentalId,
                    Action = "Car Returned",
                    ActionDate = now,
                    ActionBy = userId,
                    Notes = "Mobil dikembalikan"
                };
                _unitOfWork.RentalHistories.Add(history);

                _unitOfWork.SaveChanges();
                _unitOfWork.Commit();

                TempData["SuccessMessage"] = "Mobil berhasil dikembalikan!";
                return RedirectToAction("Details", new { id = rental.RentalId });
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();

                var inner = ex;
                while (inner.InnerException != null)
                    inner = inner.InnerException;

                System.Diagnostics.Debug.WriteLine("Return Error: " + inner.Message);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengembalikan mobil.";
                return RedirectToAction("Return", new { id = rental.RentalId });
            }
        }

        // GET: Rental/Cancel/5
        public ActionResult Cancel(int id)
        {
            var rental = _unitOfWork.Rentals.GetById(id);

            if (rental == null)
            {
                TempData["ErrorMessage"] = "Penyewaan tidak ditemukan.";
                return RedirectToAction("MyRentals");
            }

            var userId = (int)Session["UserId"];

            if (rental.UserId != userId)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses ke halaman ini.";
                return RedirectToAction("MyRentals");
            }

            if (rental.Status != "Active")
            {
                TempData["ErrorMessage"] = "Penyewaan ini sudah tidak aktif.";
                return RedirectToAction("Details", new { id = rental.RentalId });
            }

            // Load car data
            rental.Car = _unitOfWork.Cars.GetById(rental.CarId);

            return View(rental);
        }

        // POST: Rental/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelConfirmed(int id, string notes)
        {
            var rental = _unitOfWork.Rentals.GetById(id);

            if (rental == null)
            {
                TempData["ErrorMessage"] = "Penyewaan tidak ditemukan.";
                return RedirectToAction("MyRentals");
            }

            var userId = (int)Session["UserId"];

            if (rental.UserId != userId)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses.";
                return RedirectToAction("MyRentals");
            }

            if (rental.Status != "Active")
            {
                TempData["ErrorMessage"] = "Penyewaan ini sudah tidak aktif.";
                return RedirectToAction("Details", new { id = rental.RentalId });
            }

            try
            {
                var conn = _unitOfWork.Context.Database.Connection;
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                _unitOfWork.BeginTransaction();

                var cancelDateTime = DateTime.Now;

                // Update rental
                rental.Status = "Cancelled";
                rental.UpdatedAt = cancelDateTime;

                // Add cancellation notes
                if (!string.IsNullOrEmpty(notes))
                {
                    var sanitizedNotes = Server.HtmlEncode(notes.Trim());
                    rental.Notes = string.IsNullOrEmpty(rental.Notes)
                        ? "Dibatalkan: " + sanitizedNotes
                        : rental.Notes + " | Dibatalkan: " + sanitizedNotes;
                }

                _unitOfWork.Rentals.Update(rental);

                // Update status mobil menjadi "Tersedia"
                var car = _unitOfWork.Cars.GetById(rental.CarId);
                if (car != null)
                {
                    car.Status = "Tersedia";
                    car.UpdatedAt = cancelDateTime;
                    _unitOfWork.Cars.Update(car);
                }

                // Save rental and car updates
                _unitOfWork.SaveChanges();

                // Catat history
                var history = new RentalHistory
                {
                    RentalId = rental.RentalId,
                    Action = "Rental Cancelled",
                    ActionDate = cancelDateTime,
                    ActionBy = userId,
                    Notes = "Penyewaan dibatalkan oleh user"
                };
                _unitOfWork.RentalHistories.Add(history);

                // Save history
                _unitOfWork.SaveChanges();
                _unitOfWork.Commit();

                TempData["SuccessMessage"] = "Penyewaan berhasil dibatalkan.";
                return RedirectToAction("MyRentals");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();

                var inner = ex;
                while (inner.InnerException != null)
                    inner = inner.InnerException;

                System.Diagnostics.Debug.WriteLine("Cancel Error: " + inner.Message);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat membatalkan penyewaan.";
                return RedirectToAction("Cancel", new { id = rental.RentalId });
            }
        }
    }
}