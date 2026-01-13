using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnWeb.Models;


namespace DoAnWeb.Controllers
{
    public class HomeController : Controller
    {
        private nha_thuocEntities db = new nha_thuocEntities();

        public ActionResult Index()
        {
            // Lấy 8 sản phẩm mới nhất để hiển thị trang chủ
            var sanPhamMoi = db.thuocs.OrderByDescending(t => t.ma_thuoc).Take(8).ToList();
            return View(sanPhamMoi);
        }
    }
}