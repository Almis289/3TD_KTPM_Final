using Book_Store.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Book_Store.Libraries
{
    public class VnpayLibrary
    {
        private SortedDictionary<string, string> _requestData = new SortedDictionary<string, string>();
        private SortedDictionary<string, string> _responseData = new SortedDictionary<string, string>();

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            var data = new StringBuilder();
            var queryParams = _requestData
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .OrderBy(kv => kv.Key)
                .Select(kv => WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value));

            string queryString = string.Join("&", queryParams);

            string secureHash = HmacSHA512(vnpHashSecret, queryString);

            return baseUrl + "?" + queryString + "&vnp_SecureHash=" + secureHash;
        }

        public bool ValidateSignature(string vnpSecureHash, string hashSecret)
        {
            var data = new StringBuilder();
            var queryParams = _responseData
                .Where(kv => kv.Key.StartsWith("vnp_") && !string.IsNullOrEmpty(kv.Value))
                .OrderBy(kv => kv.Key)
                .Select(kv => WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value));

            string queryString = string.Join("&", queryParams);

            string calculatedHash = HmacSHA512(hashSecret, queryString);

            return calculatedHash.Equals(vnpSecureHash, StringComparison.InvariantCultureIgnoreCase);
        }

        public string GetIpAddress(HttpContext context)
        {
            string ip = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ip = context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
            }
            return ip.Length > 15 ? "127.0.0.1" : ip;
        }

        private string HmacSHA512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData ?? string.Empty);

            using var hmac = new HMACSHA512(keyBytes);
            var hash = hmac.ComputeHash(inputBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }




        // Phần GetFullResponseData nếu bạn dùng cho return
        public PaymentResponseModel GetFullResponseData(IQueryCollection collections, string hashSecret)
        {
            var vnpayData = new SortedDictionary<string, string>();
            PaymentResponseModel response = new PaymentResponseModel();

            // Lấy tất cả params từ query string trả về từ VNPAY
            foreach (var param in collections)
            {
                if (!string.IsNullOrEmpty(param.Value))
                {
                    vnpayData.Add(param.Key, param.Value.ToString());
                }
            }

            // Kiểm tra checksum (vnp_SecureHash)
            string vnp_SecureHash = collections["vnp_SecureHash"];
            vnpayData.Remove("vnp_SecureHashType");
            vnpayData.Remove("vnp_SecureHash");

            // Tạo chuỗi dữ liệu để hash (sắp xếp theo key alphabet)
            StringBuilder hashData = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in vnpayData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    hashData.Append(HttpUtility.UrlEncode(kv.Key) + "=" + HttpUtility.UrlEncode(kv.Value) + "&");
                }
            }

            string dataToHash = hashData.ToString();
            if (dataToHash.EndsWith("&"))
            {
                dataToHash = dataToHash.Substring(0, dataToHash.Length - 1);
            }

            string calculatedHash = HmacSHA512(hashSecret, dataToHash);

            // So sánh hash
            bool isValidSignature = calculatedHash.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);

            // Gán dữ liệu trả về
            response.VnPayResponseCode = collections["vnp_ResponseCode"];
            response.TransactionId = collections["vnp_TransactionNo"];
            response.OrderId = collections["vnp_TxnRef"];
            response.Amount = long.Parse(collections["vnp_Amount"]) / 100; // chia 100 vì VNPAY nhân 100
            response.BankCode = collections["vnp_BankCode"];
            response.OrderDescription = collections["vnp_OrderInfo"];
            response.PaymentTime = collections["vnp_PayDate"]; // format yyyyMMddHHmmss
            response.Message = GetResponseMessage(response.VnPayResponseCode);

            // Xác định thành công hay không
            if (isValidSignature && response.VnPayResponseCode == "00")
            {
                response.Success = true;
            }
            else
            {
                response.Success = false;
            }

            return response;
        }

        // Hàm hỗ trợ lấy message tiếng Việt theo mã lỗi VNPAY
        private string GetResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "01" => "Giao dịch đã tồn tại",
                "02" => "Tham số không hợp lệ",
                "03" => "Dữ liệu gửi sang không đúng định dạng",
                "04" => "Khởi tạo giao dịch thất bại",
                "05" => "Không tìm thấy giao dịch",
                "07" => "Giao dịch bị nghi ngờ gian lận",
                "09" => "Thẻ/Tài khoản bị khóa hoặc giao dịch bị từ chối",
                "10" => "Giao dịch thất bại do khách hàng hủy",
                "11" => "Giao dịch hết hạn",
                "12" => "Thẻ/Tài khoản bị khóa tạm thời",
                "13" => "Giao dịch thất bại do lỗi OTP",
                "24" => "Khách hàng hủy giao dịch",
                "51" => "Tài khoản không đủ tiền",
                "65" => "Tài khoản vượt hạn mức giao dịch trong ngày",
                "75" => "Ngân hàng bảo trì",
                "79" => "Nhập sai mật khẩu thanh toán quá số lần quy định",
                "99" => "Lỗi không xác định",
                _ => "Giao dịch thất bại"
            };
        }
    }
}