namespace Book_Store.Models
{
    public class VnPaymentResponseModel
    {
        public bool Success { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string? VnPayResponseCode { get; set; }
    }
}
