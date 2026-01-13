using DoAnWeb.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity; // Để dùng Include
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DoAnWeb.Controllers
{
    public class AdminController : Controller
    {
        private nha_thuocEntities db = new nha_thuocEntities();

        // GET: Admin/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // POST: Admin/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            var admin = db.tai_khoan.FirstOrDefault(x => x.ten_dang_nhap == username && x.mat_khau == password && x.vai_tro == "admin");
            if (admin != null)
            {
                Session["Admin"] = admin;
                Session["AdminName"] = admin.ten_dang_nhap;
                return RedirectToAction("Dashboard");
            }
            ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
            return View();
        }

        // Logout
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        // Kiểm tra đăng nhập (dùng cho mọi action)
        private ActionResult CheckLogin()
        {
            if (Session["Admin"] == null)
                return RedirectToAction("Login");
            return null;
        }

        // Dashboard
        public ActionResult Dashboard()
        {
            var check = CheckLogin();
            if (check != null) return check;

            ViewBag.TongDonHang = db.don_hang.Count();
            ViewBag.DonChoXuly = db.don_hang.Count(x => x.trang_thai == "Đang xử lý");
            ViewBag.DoanhThu = db.don_hang.Sum(x => (decimal?)x.tong_tien) ?? 0;
            ViewBag.TongKhachHang = db.khach_hang.Count();

            // Top sản phẩm bán chạy 
            var top = db.chi_tiet_don_hang
                .GroupBy(ct => ct.ma_thuoc)
                .Select(g => new { MaThuoc = g.Key, SoLuong = g.Sum(ct => ct.so_luong) })
                .OrderByDescending(g => g.SoLuong)
                .Take(10)
                .ToList();

            var topList = top.Select(g => new
            {
                Ten = db.thuocs.FirstOrDefault(t => t.ma_thuoc == g.MaThuoc)?.ten_thuoc ?? "Không xác định",
                SoLuong = g.SoLuong
            }).ToList();

            ViewBag.TopSanPham = topList;

            var today = DateTime.Today;

// Ngày: 7 ngày gần nhất
var sevenDaysAgo = today.AddDays(-6);
var dailyData = db.don_hang
    .Where(d => d.ngay_dat >= sevenDaysAgo && d.ngay_dat <= today)
    .ToList()
    .GroupBy(d => d.ngay_dat.Value.Date)
    .Select(g => new { Ngay = g.Key, Tong = g.Sum(x => x.tong_tien ?? 0) })
    .OrderBy(g => g.Ngay)
    .ToList();

ViewBag.DailyLabels = JsonConvert.SerializeObject(dailyData.Select(r => r.Ngay.ToString("dd/MM")));
ViewBag.DailyData = JsonConvert.SerializeObject(dailyData.Select(r => r.Tong));

// Tháng: 12 tháng gần nhất
var twelveMonthsAgo = today.AddMonths(-11);
var firstMonth = new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1);
var monthlyData = db.don_hang
    .Where(d => d.ngay_dat >= firstMonth && d.ngay_dat <= today)
    .ToList()
    .GroupBy(d => new { d.ngay_dat.Value.Year, d.ngay_dat.Value.Month })
    .Select(g => new { Thang = new DateTime(g.Key.Year, g.Key.Month, 1), Tong = g.Sum(x => x.tong_tien ?? 0) })
    .OrderBy(g => g.Thang)
    .ToList();

ViewBag.MonthlyLabels = JsonConvert.SerializeObject(monthlyData.Select(r => r.Thang.ToString("MM/yyyy")));
ViewBag.MonthlyData = JsonConvert.SerializeObject(monthlyData.Select(r => r.Tong));

// Năm: 5 năm gần nhất
var fiveYearsAgo = today.Year - 4;
var yearlyData = db.don_hang
    .Where(d => d.ngay_dat.Value.Year >= fiveYearsAgo && d.ngay_dat.Value.Year <= today.Year)
    .ToList()
    .GroupBy(d => d.ngay_dat.Value.Year)
    .Select(g => new { Nam = g.Key, Tong = g.Sum(x => x.tong_tien ?? 0) })
    .OrderBy(g => g.Nam)
    .ToList();

ViewBag.YearlyLabels = JsonConvert.SerializeObject(yearlyData.Select(r => r.Nam.ToString()));
ViewBag.YearlyData = JsonConvert.SerializeObject(yearlyData.Select(r => r.Tong));
            return View();
        }

        // Quản lý khách hàng
        [HttpGet]
        public ActionResult KhachHang(string search = "")
        {
            var check = CheckLogin();
            if (check != null) return check;

            var khach = db.khach_hang.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                khach = khach.Where(k => k.ho_ten.ToLower().Contains(search) || k.so_dien_thoai.Contains(search));
            }
            ViewBag.Search = search;
            return View(khach.OrderByDescending(k => k.ma_khach).ToList());
        }

        // Chi tiết khách hàng
        public ActionResult ChiTietKhachHang(int id)
        {
            var check = CheckLogin();
            if (check != null) return check;

            var kh = db.khach_hang.Find(id);
            if (kh == null) return HttpNotFound();

            ViewBag.DonHang = db.don_hang
                .Where(d => d.ma_khach == id)
                .OrderByDescending(d => d.ngay_dat)
                .ToList();

            return View(kh);
        }

        // Quản lý đơn hàng
        [HttpGet]
        public ActionResult DonHang(string search = "")
        {
            var check = CheckLogin();
            if (check != null) return check;

            var don = db.don_hang.Include("khach_hang").AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                int maDon;
                if (int.TryParse(search, out maDon))
                {
                    don = don.Where(d => d.ma_don == maDon);
                }
                else
                {
                    don = don.Where(d => d.khach_hang.ho_ten.ToLower().Contains(search.ToLower()));
                }
            }

            ViewBag.Search = search;
            return View(don.OrderByDescending(d => d.ma_don).ToList());
        }

        // Chi tiết đơn hàng
        public ActionResult ChiTietDonHang(int id)
        {
            var check = CheckLogin();
            if (check != null) return check;

            var don = db.don_hang
                .Include("khach_hang")
                .Include("chi_tiet_don_hang")
                .Include("chi_tiet_don_hang.thuoc")
                .FirstOrDefault(d => d.ma_don == id);

            if (don == null) return HttpNotFound();
            return View(don);
        }

        // Cập nhật trạng thái đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatTrangThai(int id, string trang_thai)
        {
            var check = CheckLogin();
            if (check != null) return check;

            var don = db.don_hang.Find(id);
            if (don != null)
            {
                don.trang_thai = trang_thai;
                db.SaveChanges();
                TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công!";
            }
            return RedirectToAction("ChiTietDonHang", new { id = id });
        }

        // Quản lý sản phẩm (danh sách)
        [HttpGet]
        public ActionResult SanPham(string search = "")
        {
            var check = CheckLogin();
            if (check != null) return check;

            var sp = db.thuocs.Include("loai_thuoc").AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                sp = sp.Where(s => s.ten_thuoc.ToLower().Contains(search));
            }
            ViewBag.Search = search;
            return View(sp.OrderByDescending(s => s.ma_thuoc).ToList());
        }
    }
}