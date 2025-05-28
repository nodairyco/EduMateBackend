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
    private readonly string _emMail = configuration.GetValue<string>("Email:Email")!;
    private readonly string _emPassword = configuration.GetValue<string>("Email:Password")!;

    public Task SendVerificationEmailAsync(User user, string token)
    {
        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_emMail, _emPassword)
        };

        var link = $"http://localhost:5190/verifyUserEmail?token={token}";

        const string subject = "EduMate Email Verification";
        var body = $"Continue on this link for verification: {link}";

        return client.SendMailAsync(
            new MailMessage(
                from: _emMail,
                to: user.Email,
                subject: subject,
                body: body
            ));
    }

    public Task SendPasswordChangePassKeyAsync(string email, string passkey)
    {
        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_emMail, _emPassword)
        };

        const string subject = "EduMate Password Reset Key";
        var body =
            $"Your one time password reset key is: {passkey}. \n You only have 10 minutes to change the password.";

        return client.SendMailAsync(
            new MailMessage(
                from: _emMail,
                to: email,
                subject: subject,
                body: body
            ));
    }
}