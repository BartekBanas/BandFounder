// using System.Text.Json;
// using BandFounder.Application.Dtos.Email;
// using BandFounder.Application.Services;
//
// namespace Services.Tests;
//
// [TestFixture]
// public class EmailServiceTests
// {
//     private EmailService _emailService;
//
//     [SetUp]
//     public void Setup()
//     {
//         _emailService = new EmailService();
//     }
//
//     [Test]
//     public async Task SendEmailAsync_ValidInput_EmailSent()
//     {
//         // Arrange
//         string email = "recipient@example.com";
//         string subject = "Test Subject";
//         string htmlMessage = "<h1>Test Message</h1>";
//
//         var emailCredentials = new EmailCredentials
//         {
//             EmailAddress = "sender@example.com",
//             Password = "password123"
//         };
//
//         // Simuluj odczytanie pliku z danymi logowania
//         var filePath = "./emailCredentials.json";
//         var jsonData = JsonSerializer.Serialize(emailCredentials);
//         await File.WriteAllTextAsync(filePath, jsonData);
//
//         // Act
//         await _emailService.SendEmailAsync(email, subject, htmlMessage);
//
//         // Assert
//         // Brak wyjątku oznacza poprawne wykonanie
//         Assert.Pass();
//     }
//
//     [Test]
//     public void RetrieveEmailCredentials_FileNotFound_ThrowsException()
//     {
//         // Arrange
//         var invalidPath = "./nonExistentFile.json";
//
//         // Act & Assert
//         var ex = Assert.ThrowsAsync<Exception>(async () =>
//             await _emailService.RetrieveEmailCredentials(invalidPath));
//
//         Assert.That(ex.Message, Does.Contain("Error reading email credentials"));
//     }
// }