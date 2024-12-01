using BandFounder.Application.Dtos.Email;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace BandFounder.Application.Services;

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}

public class EmailService : IEmailService
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var emailCredentials = await RetrieveEmailCredentials();

        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(emailCredentials.EmailAddress, emailCredentials.Password),
            EnableSsl = true
        };

        await client.SendMailAsync(
            new MailMessage(from: emailCredentials.EmailAddress, to: email, subject, htmlMessage)
            {
                IsBodyHtml = true
            });
    }

    public async Task<EmailCredentials> RetrieveEmailCredentials(string filePath = "./emailCredentials.json")
    {
        try
        {
            var data = await File.ReadAllTextAsync(filePath);
            var emailCredentials = JsonSerializer.Deserialize<EmailCredentials>(data);
            if (emailCredentials is null)
            {
                throw new Exception("Email credentials could not be read");
            }

            return emailCredentials;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading email credentials: {ex.Message}", ex);
        }
    }
}