namespace Book_Store.Models
{
    public class PaymentInformationModel
    {
        public string Name { get; set; } = string.Empty;
        public string OrderDescription { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string OrderType { get; set; } = "other";
        public long OrderId { get; set; }

        // Thêm để hỗ trợ chọn ngân hàng (nếu cần), có thể để null
        public string? BankCode { get; set; }
    }

}
