using Book_Store.Helpers;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;



namespace Book_Store.Helpers
{
    public class VnpayLibrary
    {
        private readonly SortedList<string, string> _requestData = new();



        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                _requestData.Add(key, value);
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var query = new StringBuilder();
            var signData = new StringBuilder();

            foreach (var kv in _requestData)
            {
                var encodedKey = WebUtility.UrlEncode(kv.Key);
                var encodedValue = WebUtility.UrlEncode(kv.Value);

                query.Append($"{encodedKey}={encodedValue}&");
                signData.Append($"{encodedKey}={encodedValue}&");
            }

            if (query.Length > 0) query.Length--;
            if (signData.Length > 0) signData.Length--;

            var secureHash = HmacSHA512(hashSecret, signData.ToString());

            return $"{baseUrl}?{query}&vnp_SecureHash={secureHash}";
        }

        public static string HmacSHA512(string key, string input)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            return BitConverter.ToString(hmac.ComputeHash(
                Encoding.UTF8.GetBytes(input)))
                .Replace("-", "").ToLower();
        }
    }
}