using Book_Store.Models;

namespace Book_Store.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model);

        VnPaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
