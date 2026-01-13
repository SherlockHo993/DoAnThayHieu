using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnWeb.Models;

namespace DoAnWeb.Controllers
{
    public class GioHangController : Controller
    {
        private nha_thuocEntities db = new nha_thuocEntities();

        // Lấy giỏ hàng từ Session
        private List<CartItem> GetCart()
        {
            List<CartItem> cart = Session["GioHang"] as List<CartItem>;
            if (cart == null)
            {
                cart = new List<CartItem>();
                Session["GioHang"] = cart;
            }
            return cart;
        }

        public ActionResult AddToCart(int id)
        {
            var p = db.thuocs.Find(id);
            if (p != null)
            {
                var cart = GetCart();
                var item = cart.FirstOrDefault(x => x.MaThuoc == id);
                if (item != null)
                {
                    item.SoLuong++;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        MaThuoc = p.ma_thuoc,
                        TenThuoc = p.ten_thuoc,
                        DonGia = p.don_gia,
                        HinhAnh = p.hinh_anh,
                        SoLuong = 1
                    });
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult Index()
        {
            var cart = GetCart();
            ViewBag.TongTien = cart.Sum(x => x.ThanhTien);
            return View(cart);
        }

        public ActionResult Remove(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.MaThuoc == id);
            if (item != null) cart.Remove(item);
            return RedirectToAction("Index");
        }




        // POST: Xử lý thanh toán và lưu đơn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(string ho_ten, string so_dien_thoai, string dia_chi, string ghi_chu = "")
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(ho_ten) || string.IsNullOrEmpty(so_dien_thoai) || string.IsNullOrEmpty(dia_chi))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ thông tin giao hàng!";
                ViewBag.TongTien = cart.Sum(x => x.ThanhTien);
                ViewBag.TongSoLuong = cart.Sum(x => x.SoLuong);
                return View(cart);
            }

            try
            {
                khach_hang khachHang;

                // Trường hợp đã login
                if (Session["User"] != null)
                {
                    var user = Session["User"] as tai_khoan;
                    khachHang = db.khach_hang.FirstOrDefault(k => k.ma_tai_khoan == user.ma_tai_khoan);

                    if (khachHang != null)
                    {
                        // Cập nhật thông tin
                        khachHang.ho_ten = ho_ten;
                        khachHang.so_dien_thoai = so_dien_thoai;
                        khachHang.dia_chi = dia_chi;
                    }
                    else
                    {
                        // Tạo mới khách hàng gắn với tài khoản
                        khachHang = new khach_hang
                        {
                            ho_ten = ho_ten,
                            so_dien_thoai = so_dien_thoai,
                            dia_chi = dia_chi,
                            ma_tai_khoan = user.ma_tai_khoan
                        };
                        db.khach_hang.Add(khachHang);
                    }
                }
                else
                {
                    // Khách vãng lai: kiểm tra SĐT đã tồn tại chưa
                    khachHang = db.khach_hang.FirstOrDefault(k => k.so_dien_thoai == so_dien_thoai);

                    if (khachHang != null)
                    {
                        // Cập nhật thông tin nếu tìm thấy
                        khachHang.ho_ten = ho_ten;
                        khachHang.dia_chi = dia_chi;
                    }
                    else
                    {
                        // Tạo mới hoàn toàn
                        khachHang = new khach_hang
                        {
                            ho_ten = ho_ten,
                            so_dien_thoai = so_dien_thoai,
                            dia_chi = dia_chi
                        };
                        db.khach_hang.Add(khachHang);
                    }
                }

                db.SaveChanges(); // Lưu để lấy ma_khach

                // Tạo đơn hàng
                var donHang = new don_hang
                {
                    ma_khach = khachHang.ma_khach,
                    ngay_dat = DateTime.Now,
                    tong_tien = cart.Sum(x => x.ThanhTien),
                    trang_thai = "Đang xử lý"
                };
                db.don_hang.Add(donHang);
                db.SaveChanges();

                // Lưu chi tiết đơn hàng
                foreach (var item in cart)
                {
                    var ct = new chi_tiet_don_hang
                    {
                        ma_don = donHang.ma_don,
                        ma_thuoc = item.MaThuoc,
                        so_luong = item.SoLuong,
                        don_gia = item.DonGia
                    };
                    db.chi_tiet_don_hang.Add(ct);
                }
                db.SaveChanges();

                // Xóa giỏ hàng
                Session["GioHang"] = null;

                // Chuyển sang trang thành công, truyền thông tin
                ViewBag.MaDon = donHang.ma_don;
                ViewBag.HoTen = ho_ten;
                ViewBag.TongTien = donHang.tong_tien;
                ViewBag.NgayDat = donHang.ngay_dat;

                return View("Success");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi đặt hàng: " + ex.Message;
                ViewBag.TongTien = cart.Sum(x => x.ThanhTien);
                ViewBag.TongSoLuong = cart.Sum(x => x.SoLuong);
                return View(cart);
            }
        }
        [HttpGet]
        public ActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index");
            }

            // Tính tổng tiền và số lượng
            ViewBag.TongTien = cart.Sum(x => x.ThanhTien);
            ViewBag.TongSoLuong = cart.Sum(x => x.SoLuong);

            // Nếu đã login → tự điền thông tin khách
            string hoTen = "";
            string soDienThoai = "";
            string diaChi = "";

            if (Session["User"] != null)
            {
                var user = Session["User"] as tai_khoan;
                if (user != null)
                {
                    var kh = db.khach_hang.FirstOrDefault(k => k.ma_tai_khoan == user.ma_tai_khoan);
                    if (kh != null)
                    {
                        hoTen = kh.ho_ten ?? "";
                        soDienThoai = kh.so_dien_thoai ?? "";
                        diaChi = kh.dia_chi ?? "";
                    }
                }
            }

            ViewBag.HoTen = hoTen;
            ViewBag.SoDienThoai = soDienThoai;
            ViewBag.DiaChi = diaChi;

            return View(cart);
        }
    }
}