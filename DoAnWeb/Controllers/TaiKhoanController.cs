using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnWeb.Models;

namespace DoAnWeb.Controllers
{
    public class TaiKhoanController : Controller
    {
        private nha_thuocEntities db = new nha_thuocEntities();

        [HttpGet]
        public ActionResult Login() => View();

        [HttpPost]
        public ActionResult Login(string username, string password)
        {

            var user = db.tai_khoan.FirstOrDefault(u => u.ten_dang_nhap == username && u.mat_khau == password);
            if (user != null)
            {
                Session["User"] = user;
                Session["UserName"] = user.ten_dang_nhap;
                return RedirectToAction("Dashboard", "Admin");
            }
            ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu";
            return View();
        }

        [HttpGet]
        public ActionResult Register() => View();

        [HttpPost]
        public ActionResult Register(tai_khoan tk, string ho_ten, string so_dien_thoai)
        {
            if (ModelState.IsValid)
            {
                // 1. Tạo tài khoản
                tk.ngay_tao = System.DateTime.Now;
                tk.vai_tro = "khach_hang";
                db.tai_khoan.Add(tk);
                db.SaveChanges();

                // 2. Tạo thông tin khách hàng
                var kh = new khach_hang
                {
                    ho_ten = ho_ten,
                    so_dien_thoai = so_dien_thoai,
                    ma_tai_khoan = tk.ma_tai_khoan
                };
                db.khach_hang.Add(kh);
                db.SaveChanges();

                return RedirectToAction("Login");
            }
            return View();
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}