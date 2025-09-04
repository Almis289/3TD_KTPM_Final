using Book_Store.Models;

namespace Book_Store.Helpers
{
    public static class OrderStatusHelper
    {
        public static Dictionary<OrderStatus, string> GetStatusDisplay()
        {
            return new Dictionary<OrderStatus, string>
            {
                { OrderStatus.DangXuLy, "Đang xử lý" },
                { OrderStatus.DangChuanBi, "Đang chuẩn bị" },
                { OrderStatus.DangVanChuyen, "Đang vận chuyển" },
                { OrderStatus.DaGiao, "Đã giao" },
                { OrderStatus.DaHuy, "Đã hủy" }
            };
        }
    }
}
