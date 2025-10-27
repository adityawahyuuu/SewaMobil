using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using SewaMobil.Models;
using SewaMobil.Models.Repository;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace SewaMobil.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminController()
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
            // Cek apakah user adalah admin
            if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Admin")
            {
                filterContext.Result = new RedirectResult("~/Account/Login");
            }
            base.OnActionExecuting(filterContext);
        }

        // GET: Admin
        public ActionResult Index()
        {
            ViewBag.TotalCars = _unitOfWork.Cars.Count(c => c.IsActive);
            ViewBag.AvailableCars = _unitOfWork.Cars.Count(c => c.Status == "Tersedia" && c.IsActive);
            ViewBag.RentedCars = _unitOfWork.Cars.Count(c => c.Status == "Disewa" && c.IsActive);
            ViewBag.ActiveRentals = _unitOfWork.Rentals.Count(r => r.Status == "Active");

            return View();
        }

        #region Car Management (CRUD)

        // GET: Admin/Cars
        public ActionResult Cars(string search)
        {
            var cars = _unitOfWork.Cars.Query().Where(c => c.IsActive);

            if (!string.IsNullOrEmpty(search))
            {
                var searchTerm = Server.HtmlEncode(search.ToLower());
                cars = cars.Where(c =>
                    c.Brand.ToLower().Contains(searchTerm) ||
                    c.Model.ToLower().Contains(searchTerm) ||
                    c.LicensePlate.ToLower().Contains(searchTerm));
            }

            return View(cars.OrderBy(c => c.Brand).ThenBy(c => c.Model).ToList());
        }

        // GET: Admin/CreateCar
        public ActionResult CreateCar()
        {
            return View();
        }

        // POST: Admin/CreateCar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCar(Car car)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var conn = _unitOfWork.Context.Database.Connection;
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    _unitOfWork.BeginTransaction();

                    car.Brand = Server.HtmlEncode(car.Brand);
                    car.Model = Server.HtmlEncode(car.Model);
                    car.LicensePlate = Server.HtmlEncode(car.LicensePlate);

                    car.CreatedAt = DateTime.Now;
                    car.IsActive = true;
                    car.Status = "Tersedia";

                    _unitOfWork.Cars.Add(car);
                    _unitOfWork.SaveChanges();
                    _unitOfWork.Commit();

                    TempData["SuccessMessage"] = "Data mobil berhasil ditambahkan.";
                    return RedirectToAction("Cars");
                }
                catch (Exception ex)
                {
                    _unitOfWork.Rollback();
                    System.Diagnostics.Debug.WriteLine("CreateCar Error: " + ex.Message);
                    TempData["ErrorMessage"] = "Terjadi kesalahan saat menambahkan mobil.";
                }
            }

            return View(car);
        }

        // GET: Admin/EditCar/5
        public ActionResult EditCar(int id)
        {
            var car = _unitOfWork.Cars.GetById(id);
            if (car == null || !car.IsActive)
            {
                return HttpNotFound();
            }

            return View(car);
        }

        // POST: Admin/EditCar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCar(Car car)
        {
            if (ModelState.IsValid)
            {
                var existingCar = _unitOfWork.Cars.GetById(car.CarId);
                if (existingCar == null || !existingCar.IsActive)
                {
                    return HttpNotFound();
                }

                try
                {
                    var conn = _unitOfWork.Context.Database.Connection;
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    _unitOfWork.BeginTransaction();

                    existingCar.Brand = Server.HtmlEncode(car.Brand);
                    existingCar.Model = Server.HtmlEncode(car.Model);
                    existingCar.Year = car.Year;
                    existingCar.LicensePlate = Server.HtmlEncode(car.LicensePlate);
                    existingCar.Color = Server.HtmlEncode(car.Color);
                    existingCar.Transmission = car.Transmission;
                    existingCar.FuelType = car.FuelType;
                    existingCar.Capacity = car.Capacity;
                    existingCar.DailyRate = car.DailyRate;
                    existingCar.ImageUrl = car.ImageUrl;
                    existingCar.Description = Server.HtmlEncode(car.Description);
                    existingCar.UpdatedAt = DateTime.Now;

                    _unitOfWork.Cars.Update(existingCar);
                    _unitOfWork.SaveChanges();
                    _unitOfWork.Commit();

                    TempData["SuccessMessage"] = "Data mobil berhasil diupdate.";
                    return RedirectToAction("Cars");
                }
                catch (Exception ex)
                {
                    _unitOfWork.Rollback();
                    System.Diagnostics.Debug.WriteLine("EditCar Error: " + ex.Message);
                    TempData["ErrorMessage"] = "Terjadi kesalahan saat mengupdate mobil.";
                }
            }

            return View(car);
        }

        // GET: Admin/DeleteCar/5
        public ActionResult DeleteCar(int id)
        {
            var car = _unitOfWork.Cars.GetById(id);
            if (car == null || !car.IsActive)
            {
                return HttpNotFound();
            }

            return View(car);
        }

        // POST: Admin/DeleteCar/5
        [HttpPost, ActionName("DeleteCar")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCarConfirmed(int id)
        {
            var car = _unitOfWork.Cars.GetById(id);
            try
            {
                var conn = _unitOfWork.Context.Database.Connection;
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                _unitOfWork.BeginTransaction();

                car.IsActive = false;
                car.UpdatedAt = DateTime.Now;

                _unitOfWork.Cars.Update(car);
                _unitOfWork.SaveChanges();

                _unitOfWork.Commit();
                TempData["SuccessMessage"] = "Data mobil berhasil dihapus.";
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                TempData["ErrorMessage"] = "Gagal menghapus data mobil.";
            }

            return RedirectToAction("Cars");
        }
        #endregion

        #region Rental Management

        // GET: Admin/Rentals
        public ActionResult Rentals(string status)
        {
            var rentals = _unitOfWork.Rentals.Query();

            if (!string.IsNullOrEmpty(status))
            {
                var sanitizedStatus = Server.HtmlEncode(status);
                rentals = rentals.Where(r => r.Status == sanitizedStatus);
            }

            var rentalList = rentals
                .OrderByDescending(r => r.RentalDate)
                .ToList();

            // Eager load User dan Car
            foreach (var rental in rentalList)
            {
                rental.User = _unitOfWork.Users.GetById(rental.UserId);
                rental.Car = _unitOfWork.Cars.GetById(rental.CarId);
            }

            return View(rentalList);
        }

        // GET: Admin/RentalDetails/5
        public ActionResult RentalDetails(int id)
        {
            var rental = _unitOfWork.Rentals.GetById(id);

            if (rental == null)
            {
                return HttpNotFound();
            }

            // Eager load User dan Car
            rental.User = _unitOfWork.Users.GetById(rental.UserId);
            rental.Car = _unitOfWork.Cars.GetById(rental.CarId);

            return View(rental);
        }

        // POST: Admin/UpdateRentalStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateRentalStatus(int rentalId, string newStatus)
        {
            var rental = _unitOfWork.Rentals.GetById(rentalId);
            if (rental == null)
            {
                return HttpNotFound();
            }

            try
            {
                var conn = _unitOfWork.Context.Database.Connection;
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                _unitOfWork.BeginTransaction();

                var sanitizedStatus = Server.HtmlEncode(newStatus);
                rental.Status = sanitizedStatus;

                if (sanitizedStatus == "Completed" || sanitizedStatus == "Cancelled")
                {
                    var car = _unitOfWork.Cars.GetById(rental.CarId);
                    if (car != null)
                    {
                        car.Status = "Tersedia";
                        _unitOfWork.Cars.Update(car);
                    }
                }

                _unitOfWork.Rentals.Update(rental);
                _unitOfWork.SaveChanges();
                _unitOfWork.Commit();

                TempData["SuccessMessage"] = "Status penyewaan berhasil diupdate.";
                return RedirectToAction("RentalDetails", new { id = rentalId });
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                System.Diagnostics.Debug.WriteLine("UpdateRentalStatus Error: " + ex.Message);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengupdate status penyewaan.";
                return RedirectToAction("RentalDetails", new { id = rentalId });
            }
        }

        #endregion

        #region Reports

        // GET: Admin/Reports
        public ActionResult Reports()
        {
            return View();
        }

        // GET: Admin/ExportRentalsExcel
        public ActionResult ExportRentalsExcel(DateTime? startDate, DateTime? endDate, string status)
        {
            var rentals = _unitOfWork.Rentals.Query();

            // Apply filters
            if (startDate.HasValue)
            {
                rentals = rentals.Where(r => r.RentalDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                rentals = rentals.Where(r => r.RentalDate <= endDate.Value);
            }
            var debugList = rentals.ToList();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                var sanitizedStatus = Server.HtmlEncode(status);
                rentals = rentals.Where(r => r.Status == sanitizedStatus);
            }

            var rentalList = rentals
                .OrderByDescending(r => r.RentalDate)
                .ToList();

            foreach (var rental in rentalList)
            {
                rental.User = _unitOfWork.Users.GetById(rental.UserId);
                rental.Car = _unitOfWork.Cars.GetById(rental.CarId);
            }

            // Create Excel file using EPPlus
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Laporan Penyewaan");

                // Add header
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Penyewa";
                worksheet.Cells[1, 3].Value = "Email";
                worksheet.Cells[1, 4].Value = "Mobil";
                worksheet.Cells[1, 5].Value = "Plat Nomor";
                worksheet.Cells[1, 6].Value = "Tanggal Sewa";
                worksheet.Cells[1, 7].Value = "Tanggal Kembali";
                worksheet.Cells[1, 8].Value = "Total Hari";
                worksheet.Cells[1, 9].Value = "Harga per Hari";
                worksheet.Cells[1, 10].Value = "Total Harga";
                worksheet.Cells[1, 11].Value = "Status";

                // Style header
                using (var range = worksheet.Cells[1, 1, 1, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Add data
                int row = 2;
                foreach (var rental in rentalList)
                {
                    worksheet.Cells[row, 1].Value = rental.RentalId;
                    worksheet.Cells[row, 2].Value = rental.User.FullName;
                    worksheet.Cells[row, 3].Value = rental.User.Email;
                    worksheet.Cells[row, 4].Value = rental.Car.CarName;
                    worksheet.Cells[row, 5].Value = rental.Car.LicensePlate;
                    worksheet.Cells[row, 6].Value = rental.RentalDate.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 7].Value = rental.ReturnDate.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 8].Value = rental.TotalDays;
                    worksheet.Cells[row, 9].Value = rental.Car.DailyRate;
                    worksheet.Cells[row, 10].Value = rental.TotalPrice;
                    worksheet.Cells[row, 11].Value = rental.Status;
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Generate file
                var fileName = string.Format("Laporan_Penyewaan_{0}.xlsx", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                
                package.Save();
                var fileBytes = package.GetAsByteArray();

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        #endregion
    }
}