using Book_Store.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class OrderStatusConverter : ValueConverter<OrderStatus, string>
{
    public OrderStatusConverter() : base(
        status => ConvertToString(status),
        value => ConvertToEnum(value))
    { }

    private static string ConvertToString(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.DangXuLy => "Đang xử lý",
            OrderStatus.DangChuanBi => "Đang chuẩn bị",
            OrderStatus.DangVanChuyen => "Đang vận chuyển",
            OrderStatus.DaGiao => "Đã giao",
            OrderStatus.DaHuy => "Đã hủy",
            _ => throw new InvalidOperationException($"Trạng thái không hợp lệ: {status}")
        };
    }

    private static OrderStatus ConvertToEnum(string value)
    {
        return value switch
        {
            "Đang xử lý" => OrderStatus.DangXuLy,
            "Đang chuẩn bị" => OrderStatus.DangChuanBi,
            "Đang vận chuyển" => OrderStatus.DangVanChuyen,
            "Đã giao" => OrderStatus.DaGiao,
            "Đã hủy" => OrderStatus.DaHuy,
            _ => throw new InvalidOperationException($"Giá trị không hợp lệ: {value}")
        };
    }
}
