using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnWeb.Models;

namespace DoAnWeb.Controllers
{
    public class ThuocController : Controller
    {
        private nha_thuocEntities db = new nha_thuocEntities();

        // GET: Thuoc (Có thể lọc theo loại)
        public ActionResult Index(int? maLoai)
        {
            var products = db.thuocs.AsQueryable();
            if (maLoai.HasValue)
            {
                products = products.Where(p => p.ma_loai == maLoai.Value);
            }
            ViewBag.Categories = db.loai_thuoc.ToList(); // Để render menu bên trái
            return View(products.ToList());
        }

        // GET: Thuoc/Details/5
        public ActionResult Details(int id)
        {
            var product = db.thuocs.Find(id);
            if (product == null) return HttpNotFound();

            // Gợi ý sản phẩm cùng loại
            ViewBag.RelatedProducts = db.thuocs
                                        .Where(t => t.ma_loai == product.ma_loai && t.ma_thuoc != id)
                                        .Take(4).ToList();
            return View(product);
        }
    }
}