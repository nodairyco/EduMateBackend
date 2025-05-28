using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using EduMateBackend.Models;
using Microsoft.AspNetCore.Identity;

namespace EduMateBackend.Services;

public class EmailService(IConfiguration configuration)
{
    private PasswordHasher<User> _idHasher = new();

    public Task SendVerificationEmailAsync(User user, string token)
    {
        var emMail = configuration.GetValue<string>("Email:Email")!;
        var emPassword = configuration.GetValue<string>("Email:Password")!;

        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(emMail, emPassword)
        };

        var link = $"http://localhost:5190/verifyUserEmail?token={token}";

        const string subject = "EduMate Email Verification";
        var body = $"Continue on this link for verification: {link}";

        return client.SendMailAsync(
            new MailMessage(
                from: emMail,
                to: user.Email,
                subject: subject,
                body: body
            ));
    }

    

}