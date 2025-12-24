namespace Book_Store.Models
{
    public class PaymentResponseModel
    {
        public bool Success { get; set; }
        public string? VnPayResponseCode { get; set; }
        public string? Message { get; set; }
        public string? OrderId { get; set; }
        public string? TransactionId { get; set; }

        // THÊM 3 PROPERTY BỊ THIẾU Ở ĐÂY
        public decimal Amount { get; set; }           // Tổng tiền (đã chia 100)
        public string? BankCode { get; set; }         // Mã ngân hàng
        public string? PaymentTime { get; set; }      // Thời gian thanh toán (yyyyMMddHHmmss)

        // Thêm vài cái hữu ích khác (tùy chọn)
        public string? OrderDescription { get; set; }
        public string? CardType { get; set; }
        public string? BankTranNo { get; set; }
    }

}
