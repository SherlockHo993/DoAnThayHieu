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
        public ActionResult Index(int? maLoai, int page = 1, int pageSize = 12)
        {
            var products = db.thuocs
                .Include("loai_thuoc") // Load luôn loại thuốc để tránh null
                .AsQueryable();

            if (maLoai.HasValue)
            {
                products = products.Where(p => p.ma_loai == maLoai.Value);
            }

            int totalItems = products.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var pagedProducts = products
                .OrderBy(p => p.ma_thuoc) // Hoặc OrderByDescending(p => p.ma_thuoc) nếu muốn mới nhất
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Categories = db.loai_thuoc.ToList();
            ViewBag.MaLoai = maLoai;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(pagedProducts);
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