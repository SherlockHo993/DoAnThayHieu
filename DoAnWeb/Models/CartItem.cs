using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnWeb.Models
{
    public class CartItem
    {
        public int MaThuoc { get; set; }
        public string TenThuoc { get; set; }
        public string HinhAnh { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }
}