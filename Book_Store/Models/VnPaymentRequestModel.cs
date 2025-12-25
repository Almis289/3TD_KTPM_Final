namespace Book_Store.Models
{
    public class VnPaymentRequestModel
    {
        public string OrderId { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
