using System.Net;
using System.Net.Mail;

namespace BandFounder.Application.Services;

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}

public class EmailService : IEmailService
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // TODO make the email
        const string mail = "Bandfounder@gmail.com";
        const string password = "Bandfounder123";

        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(mail, password),
            EnableSsl = true
        };

        await client.SendMailAsync(
            new MailMessage(from: mail, to: email, subject, htmlMessage)
            {
                IsBodyHtml = true
            });
    }
}