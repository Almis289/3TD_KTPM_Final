namespace Book_Store.Models
{
    public class PaypalPaymentModel
    {
        public string orderID { get; set; }
        public string payerID { get; set; }
        public decimal amount { get; set; }

        public string shippingAddress { get; set; }
    }
}
