using Book_Store.Models;
using MailKit.Net.Smtp;
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
using System.Threading.Tasks;

namespace Book_Store.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(EmailSettings emailSettings)
        {
            _emailSettings = emailSettings;
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
        public string BuildProductEmailTemplate(string title, string content, Product? product = null, string footer = "Cảm ơn bạn đã theo dõi Book Store!")
        {
            string productSection = "";

            if (product != null)
            {
                productSection = $@"
                <div style='margin-top:20px; padding:10px; border:1px solid #eee; border-radius:8px; text-align:center;'>
                    <h3 style='margin-bottom:10px;'>{product.ProductName}</h3>
                    <p style='margin-bottom:10px;'>Giá: {product.Price:C}</p>
                    <p style='margin-bottom:10px;'>{product.ProductDetail?.Description ?? ""}</p>
                    {{ProductImage}}
                </div>";
            }

            var formattedContent = content.Replace("\n", "<br>");

            return $@"
            <div style='font-family: Arial, sans-serif; padding:20px; background:#f4f4f4;'>
                <div style='max-width:600px; margin:auto; background:white; padding:20px; border-radius:10px;'>
                    <h2 style='color:#333;'>{title}</h2>
                    <p style='font-size:16px; line-height:1.6;'>{formattedContent}</p>
                    {productSection}
                    <hr style='margin-top:30px;' />
                    <p style='font-size:14px; color:gray;'>{footer}</p>
                </div>
            </div>";
        }
    }
}
