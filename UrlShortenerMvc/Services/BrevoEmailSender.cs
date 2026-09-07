using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace UrlShortenerMvc.Services;

public class BrevoEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var host = configuration["Brevo:Host"]
                   ?? throw new InvalidOperationException("Brevo host is not configured.");

        var port = int.Parse(configuration["Brevo:Port"] ?? "587");

        var username = configuration["Brevo:Username"]
                       ?? throw new InvalidOperationException("Brevo username is not configured.");

        var password = configuration["Brevo:Password"]
                       ?? throw new InvalidOperationException("Brevo password is not configured.");

        var fromEmail = configuration["Brevo:FromEmail"]
                        ?? throw new InvalidOperationException("Brevo FromEmail is not configured.");

        var fromName = configuration["Brevo:FromName"] ?? "Url Shortener";

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = StyledHtml(htmlMessage)
        }.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(username, password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

    private static string StyledHtml(string message) =>
        $"""
         <!DOCTYPE html>
         <html>
         <head>
             <meta charset="UTF-8">
             <meta name="viewport" content="width=device-width, initial-scale=1.0">
         </head>
         <body style="margin:0;padding:0;background-color:#f3f1ff;font-family:Arial,Helvetica,sans-serif;">
             <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f3f1ff;padding:40px 15px;">
                 <tr>
                     <td align="center">
                         <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 10px 30px rgba(81,65,223,0.10);">

                             <!-- Header -->
                             <tr>
                                 <td style="background-color:#faf9f6;padding:30px;text-align:center;">
                                     <div style="color:#111827;font-size:28px;font-weight:bold;">
                                         <span style="display:inline-block;width:40px;height:40px;border-radius:9px;color:#fff;background:linear-gradient(135deg,#7c6cff,#5141df);box-shadow:0 7px 18px rgba(92,73,236,0.22);margin-right:8px;">↗</span>Url Shortner
                                     </div>
                                 </td>
                             </tr>

                             <!-- Content -->
                             <tr>
                                 <td style="padding:40px 35px;color:#374151;font-size:16px;line-height:1.6;">
                                     {message}
                                 </td>
                             </tr>

                             <!-- Footer -->
                             <tr>
                                 <td style="background:#f7f5ff;padding:20px;text-align:center;color:#8b87a8;font-size:12px;">
                                     © 2026 Url Shortner - 
                                     <a href="https://short.runasp.net" style="color:#5141df;text-decoration:none;font-weight:bold;">short.runasp.net</a>
                                 </td>
                             </tr>
                         </table>
                     </td>
                 </tr>
             </table>
         </body>
         </html>
         """;
}