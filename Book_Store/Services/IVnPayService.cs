using Book_Store.Models;
using Microsoft.AspNetCore.Http;
using Book_Store.Models; // hoặc namespace của PaymentInformationModel
using Microsoft.AspNetCore.Http.Extensions;

namespace Book_Store.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
        // Nếu có thêm method khác thì khai báo ở đây, không có { }
    }
}
