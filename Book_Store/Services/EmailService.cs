using Book_Store.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Book_Store.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _emailSettings = options.Value;
        }

        // =====================================================
        // 1) SEND A SINGLE EMAIL (EMBED IMAGE - CID)
        // =====================================================
        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlMessage,
            List<Product>? products = null)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(
                    new MailboxAddress("Book Store", _emailSettings.FromEmail)
                );
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder();

                // ================= EMBED IMAGES =================
                if (products != null && products.Any())
                {
                    foreach (var product in products)
                    {
                        if (string.IsNullOrEmpty(product.ImageUrl))
                            continue;

                        var imagePath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            product.ImageUrl.TrimStart('/')
                        );

                        if (!File.Exists(imagePath))
                            continue;

                        var image = builder.LinkedResources.Add(imagePath);
                        image.ContentId = MimeUtils.GenerateMessageId();

                        image.ContentType.MediaType = "image";

                        // Replace src to CID
                        htmlMessage = htmlMessage.Replace(
                            $"src='{product.ImageUrl}'",
                            $"src=\"cid:{image.ContentId}\""
                        );

                    }
                }
                // =================================================

                builder.HtmlBody = htmlMessage;
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    _emailSettings.SmtpHost,
                    _emailSettings.SmtpPort,
                    MailKit.Security.SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    _emailSettings.Username,
                    _emailSettings.Password
                );

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Gửi mail thất bại tới {toEmail}: {ex.Message}");
                throw;
            }
        }

        // =====================================================
        // 2) SEND TO MANY USERS
        // =====================================================
        public async Task SendEmailToAllUsersAsync(
            List<string> emails,
            string subject,
            string htmlMessage,
            List<Product>? products = null)
        {
            foreach (var email in emails)
            {
                await SendEmailAsync(
                    email,
                    subject,
                    htmlMessage,
                    products
                );
            }
        }

        // =====================================================
        // 3) BUILD TEMPLATE WITH MULTIPLE PRODUCTS
        // =====================================================
        public string BuildProductEmailTemplate(
            string title,
            string content,
            List<Product>? products)
        {
            string productHtml = "";

            if (products != null && products.Any())
            {
                productHtml += "<div style='display:flex;gap:15px;flex-wrap:wrap;'>";

                foreach (var product in products)
                {
                    var productUrl = $"https://localhost:7224/Customer/ProductDetail/{product.ProductId}";

                    productHtml += $@"
                    <div style='width:260px;
                                border:1px solid #ddd;
                                border-radius:12px;
                                padding:15px;
                                text-align:center;'>

                        <h4>{product.ProductName}</h4>

                        <p style='color:#d32f2f;font-weight:bold;'>
                            {product.Price:N0}₫
                        </p>

                        {(string.IsNullOrEmpty(product.ImageUrl) ? "" :
                                        $"<img src='{product.ImageUrl}'style='width:100%; height:180px; object-fit:cover; border-radius:10px;' />")}

                        <a href='{productUrl}'style='display:inline-block;margin-top:12px;padding:10px 16px;background:#1976d2;color:white;border-radius:6px;text-decoration:none;font-weight:bold;font-size:14px;'>
                            Xem chi tiết
                        </a>
                    </div>";
                }

                productHtml += "</div>";
            }

            string formattedContent = content.Replace("\n", "<br>");

            return $@"
            <div style='font-family:Arial;padding:20px;background:#f4f4f4;'>
                <div style='max-width:600px;margin:auto;background:white;
                            padding:20px;border-radius:10px;'>
                    <h2 style='color:#333;text-align:center;'>
                        {title}
                    </h2>

                    <p style='font-size:16px;
                              line-height:1.6;
                              text-align:center;'>
                        {formattedContent}
                    </p>

                    {productHtml}

                    <div style='text-align:center;'>
                        <a href='https://localhost:7224/Account/Register'
                           style='display:inline-block;
                                  margin-top:25px;
                                  padding:12px 20px;
                                  background:#1976d2;
                                  color:white;
                                  border-radius:8px;
                                  text-decoration:none;
                                  font-weight:bold;'>
                            Đăng ký tài khoản ngay
                        </a>
                    </div>

                    <hr style='margin-top:30px;' />
                    <p style='font-size:14px;color:gray;text-align:center;'>
                        Cảm ơn bạn đã theo dõi Book Store!
                    </p>
                </div>
            </div>";
        }
    }
}
