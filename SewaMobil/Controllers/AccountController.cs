using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using SewaMobil.Models;
using SewaMobil.Models.Repository;

namespace SewaMobil.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AccountController()
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

        // GET: /Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                // Sanitize input untuk mencegah XSS
                var username = Server.HtmlEncode(model.UserName);

                // Cari user berdasarkan username
                var user = _unitOfWork.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    // Set authentication cookie
                    FormsAuthentication.SetAuthCookie(user.Username, model.RememberMe);

                    // Simpan role di session untuk authorization
                    Session["UserId"] = user.UserId;
                    Session["Username"] = user.Username;
                    Session["UserRole"] = user.Role;
                    Session["FullName"] = user.FullName;

                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        if (user.Role == "Admin")
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Username atau password salah.");
                }
            }

            return View(model);
        }

        // GET: /Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Sanitize input untuk mencegah XSS
                var username = Server.HtmlEncode(model.UserName);
                var email = Server.HtmlEncode(model.Email);

                // Cek apakah username sudah ada
                if (_unitOfWork.Users.Any(u => u.Username == username))
                {
                    ModelState.AddModelError("UserName", "Username sudah digunakan.");
                    return View(model);
                }

                // Cek apakah email sudah ada
                if (_unitOfWork.Users.Any(u => u.Email == email))
                {
                    ModelState.AddModelError("Email", "Email sudah terdaftar.");
                    return View(model);
                }

                // Buat user baru
                var user = new User
                {
                    Username = username,
                    Email = email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    FullName = username,
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _unitOfWork.Users.Add(user);
                _unitOfWork.SaveChanges();

                // Auto login setelah register
                FormsAuthentication.SetAuthCookie(user.Username, false);
                Session["UserId"] = user.UserId;
                Session["Username"] = user.Username;
                Session["UserRole"] = user.Role;
                Session["FullName"] = user.FullName;

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // GET: /Account/LogOff
        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/ChangePassword
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = (int)Session["UserId"];
                var user = _unitOfWork.Users.GetById(userId);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.OldPassword, user.Password))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                    user.UpdatedAt = DateTime.Now;

                    _unitOfWork.Users.Update(user);
                    _unitOfWork.SaveChanges();

                    return RedirectToAction("ChangePasswordSuccess");
                }
                else
                {
                    ModelState.AddModelError("", "Password lama tidak sesuai.");
                }
            }

            return View(model);
        }

        // GET: /Account/ChangePasswordSuccess
        public ActionResult ChangePasswordSuccess()
        {
            return View();
        }
    }
}