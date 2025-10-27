using System;
using System.Linq;
using System.Web.Mvc;
using SewaMobil.Models;
using SewaMobil.Models.Repository;

namespace SewaMobil.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public HomeController()
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

        public ActionResult Index()
        {
            // Tampilkan mobil yang tersedia
            var availableCars = _unitOfWork.Cars
                .Find(c => c.Status == "Tersedia" && c.IsActive)
                .OrderBy(c => c.Brand)
                .ThenBy(c => c.Model)
                .ToList();

            return View(availableCars);
        }

        public ActionResult Error()
        {
            return View();
        }
    }
}
