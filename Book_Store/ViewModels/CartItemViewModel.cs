using Book_Store.Models;

namespace Book_Store.ViewModels
{
    public class CartItemViewModel
    {
        public List<Order> CartItems { get; set; }

        public decimal GrandTotal { get; set; }

        public decimal ShippingCost { get; set; }

        public decimal CouponCode { get; set; }


    }
}
