using SendGrid.Helpers.Mail;
using SendGrid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CTBookerPro.Models
{
    public class EmailSender
    {
        private readonly string _apiKey;

        public EmailSender(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string content, HttpPostedFileBase attachment = null)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress("terencep429@gmail.com", "CTBookerPro");
            var to = new EmailAddress(toEmail);

            var msg = new SendGridMessage
            {
                From = from,
                Subject = subject,
                PlainTextContent = content,
                HtmlContent = content
            };

            msg.AddTo(to);

            if (attachment != null && attachment.ContentLength > 0)
            {
                using (var stream = new MemoryStream())
                {
                    attachment.InputStream.CopyTo(stream);
                    var fileBytes = stream.ToArray();
                    var fileBase64 = Convert.ToBase64String(fileBytes);
                    msg.AddAttachment(attachment.FileName, fileBase64);
                }
            }

            var response = await client.SendEmailAsync(msg);
        }

        public async Task SendBulkEmailAsync(List<string> toEmails, string subject, string content, HttpPostedFileBase attachment = null)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress("spen0042@student.monash.edu", "CTBookerPro");

            var msg = new SendGridMessage
            {
                From = from,
                Subject = subject,
                PlainTextContent = content,
                HtmlContent = content
            };

            msg.AddTos(toEmails.Select(email => new EmailAddress(email)).ToList());

            if (attachment != null && attachment.ContentLength > 0)
            {
                using (var stream = new MemoryStream())
                {
                    attachment.InputStream.CopyTo(stream);
                    var fileBytes = stream.ToArray();
                    var fileBase64 = Convert.ToBase64String(fileBytes);
                    msg.AddAttachment(attachment.FileName, fileBase64);
                }
            }

            var response = await client.SendEmailAsync(msg);
        }
    }
}