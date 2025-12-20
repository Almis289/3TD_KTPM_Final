using Book_Store.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Book_Store.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;

        public EmailService(IOptions<EmailSettings> emailSettingsOptions, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env)
        {
            _emailSettings = emailSettingsOptions.Value; // Lấy .Value
            _httpContextAccessor = httpContextAccessor;
            _env = env;
        }

        // THÊM MỚI - GỬI EMAIL + NHÚNG ẢNH CID CHO NHIỀU SẢN PHẨM
        public async Task SendEmailWithInlineImagesAsync(List<string> emails, string subject, string htmlTemplate, List<Product> products)
        {
            foreach (var email in emails)
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Book Store", _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;

                var builder = new BodyBuilder();
                builder.HtmlBody = htmlTemplate;

                foreach (var p in products)
                {
                    if (string.IsNullOrEmpty(p.ImageUrl)) continue;

                    string relativePath = p.ImageUrl.TrimStart('~').TrimStart('/');
                    string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                    fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar); // Windows dùng '\', nên thay đúng
                    var imagePath = Path.GetFullPath(fullPath); // an toàn nhất

                    if (!File.Exists(imagePath)) continue;

                    var cid = $"book_{p.ProductId}_{Guid.NewGuid():N}";
                    var attachment = builder.LinkedResources.Add(imagePath);
                    attachment.ContentId = cid;

                    // Thay placeholder cố định bằng CID thật
                    builder.HtmlBody = builder.HtmlBody.Replace($"cid:placeholder_{p.ProductId}", $"cid:{cid}");
                }

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        // Gửi email đơn lẻ với inline image
        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, string? imagePath = null)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Book Store", _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder();

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                using var image = Image.Load(imagePath);

                // Resize nếu quá lớn
                int maxWidth = 300;
                int newWidth = Math.Min(maxWidth, image.Width);
                int newHeight = (int)((double)newWidth / image.Width * image.Height);
                image.Mutate(x => x.Resize(newWidth, newHeight));

                // Lưu ảnh vào MemoryStream, fallback WebP → JPEG
                using var ms = new MemoryStream();
                string extension = Path.GetExtension(imagePath).ToLower();
                IImageEncoder encoder;

                if (extension == ".png")
                    encoder = new PngEncoder();
                else
                    encoder = new JpegEncoder { Quality = 85 }; // JPEG cho jpg/jpeg/webp/fallback

                image.Save(ms, encoder);
                ms.Position = 0;

                var contentId = Guid.NewGuid().ToString();
                var imageAttachment = builder.LinkedResources.Add($"{Guid.NewGuid()}{extension}", ms.ToArray());
                imageAttachment.ContentId = contentId;
                imageAttachment.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                imageAttachment.ContentType.MediaType = "image";

                // Set subtype đúng
                imageAttachment.ContentType.MediaSubtype = encoder switch
                {
                    PngEncoder => "png",
                    JpegEncoder => "jpeg",
                    _ => "jpeg"
                };

                // Chèn image vào HTML
                builder.HtmlBody = htmlMessage.Replace("{ProductImage}", $"<img src=\"cid:{contentId}\" style=\"max-width:250px; border-radius:10px;\" />");
            }
            else
            {
                builder.HtmlBody = htmlMessage.Replace("{ProductImage}", "");
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        // Gửi email cho nhiều người
        public async Task SendEmailToAllUsersAsync(List<string> emails, string subject, string htmlMessage, string? imagePath = null)
        {
            foreach (var email in emails)
            {
                await SendEmailAsync(email, subject, htmlMessage, imagePath);
            }
        }

        // Tạo template email với sản phẩm
        public string BuildProductEmailTemplate(string title, string content, List<Product>? products = null, string footer = "Cảm ơn bạn đã ủng hộ Book Store!")
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}{request?.PathBase}".TrimEnd('/');

            string productsHtml = "";

            if (products != null && products.Any())
            {
                var displayProducts = products.Take(10).ToList(); // tối đa 10 sách

                foreach (var p in displayProducts)
                {
                    string productLink = $"{baseUrl}/Customer/ProductDetail/{p.ProductId}";
                    string safeName = WebUtility.HtmlEncode(p.ProductName);
                    if (safeName.Length > 70) safeName = safeName.Substring(0, 67) + "...";

                    // Dùng CID để nhúng ảnh inline → hiện 100% mọi nơi
                    string placeholder = $"placeholder_{p.ProductId}";

                    productsHtml += $@"
                        <tr>
                            <td align='center' style='padding: 20px 0;'>
                                <a href='{productLink}' style='text-decoration: none; color: inherit;'>
                                  <img src='cid:{placeholder}' alt='{p.ProductName}' 
                                        style='width: 100%; max-width: 280px; height: auto; border-radius: 16px; 
                                            box-shadow: 0 10px 30px rgba(0,0,0,0.15); border: 4px solid #e74c3c;' />
                                </a>
                                <div style='margin-top: 16px; text-align: center;'>
                                    <h3 style='margin: 0 0 12px; font-size: 18px; color: #333;'>
                                        <a href='{productLink}' style='text-decoration: none; color: #333;'>
                                            {safeName}
                                        </a>
                                    </h3>
                                    <a href='{productLink}' 
                                        style='display: inline-block; padding: 12px 36px; background: #e74c3c; color: white; 
                                                font-weight: bold; font-size: 16px; border-radius: 50px; text-decoration: none;
                                                box-shadow: 0 6px 20px rgba(231,76,60,0.4);'>
                                        XEM NGAY
                                    </a>
                                </div>
                            </td>
                        </tr>";
                }
            }

            var formattedContent = string.IsNullOrWhiteSpace(content) ? "" : $"<p style='font-size:16px; line-height:1.8; color:#555; margin:20px 0;'>{content.Replace("\n", "<br>")}</p>";

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            </head>
            <body style='margin:0; padding:0; background:#f8f9fa; font-family: Arial, sans-serif;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='background:#f8f9fa;'>
                    <tr>
                        <td align='center'>
                            <div style='max-width: 600px; margin: 20px auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 15px 40px rgba(0,0,0,0.1);'>
                                <!-- Header -->
                                <div style='background: linear-gradient(135deg, #667eea, #764ba2); padding: 30px 20px; text-align: center; color: white;'>
                                    <h1 style='margin:0; font-size: 28px; font-weight: bold;'>{title}</h1>
                                </div>

                                <!-- Nội dung chào -->
                                <div style='padding: 25px 30px; text-align: center; color: #555; font-size: 16px;'>
                                    {formattedContent}
                                </div>

                                <!-- Danh sách sách -->
                                <table width='100%' cellpadding='0' cellspacing='0'>
                                    {productsHtml}
                                </table>

                                <!-- Footer -->
                                <div style='background: #2c3e50; color: #bdc3c7; padding: 30px; text-align: center; font-size: 14px;'>
                                    <p style='margin: 0 0 10px;'><strong>Book Store</strong> – Nơi hội tụ những cuốn sách hay</p>
                                    <p style='margin: 10px 0;'>
                                        <a href='{baseUrl}' style='color: #1abc9c; text-decoration: none;'>www.yourbookstore.com</a>
                                    </p>
                                    <p style='margin: 20px 0 0; font-size: 12px; opacity: 0.8;'>
                                        {footer}<br>
                                    </p> <a href='{baseUrl}/Account/Register' style='color: #95a5a6; text-decoration: underline;'>Đăng ký</a>

                                </div>
                            </div>
                        </td>
                    </tr>
                </table>
            </body>
            </html>";
        }
    }
}
