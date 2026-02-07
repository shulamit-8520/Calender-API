using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Calendar.Api.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; }
        public string SenderPassword { get; set; }
        public string SenderName { get; set; }
        public bool EnableSsl { get; set; }
    }

    public interface IEmailService
    {
        Task SendEventReminderAsync(string recipientEmail, string recipientName, string eventTitle, string eventDescription, DateTime eventDate);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEventReminderAsync(string recipientEmail, string recipientName, string eventTitle, string eventDescription, DateTime eventDate)
        {
            try
            {
                using (var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
                {
                    smtpClient.EnableSsl = _emailSettings.EnableSsl;
                    smtpClient.Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                        Subject = $"תזכורת: {eventTitle}",
                        Body = CreateEmailBody(recipientName, eventTitle, eventDescription, eventDate),
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(recipientEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email sent successfully to {recipientEmail} for event: {eventTitle}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {recipientEmail}: {ex.Message}");
                throw;
            }
        }

        private string CreateEmailBody(string recipientName, string eventTitle, string eventDescription, DateTime eventDate)
        {
            return $@"
<!DOCTYPE html>
<html dir='rtl' lang='he'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f4f4f4;
            margin: 0;
            padding: 20px;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: white;
            border-radius: 10px;
            padding: 30px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 20px;
            border-radius: 10px 10px 0 0;
            text-align: center;
            margin: -30px -30px 20px -30px;
        }}
        .event-title {{
            font-size: 24px;
            font-weight: bold;
            color: #333;
            margin: 20px 0;
        }}
        .event-date {{
            font-size: 18px;
            color: #667eea;
            margin: 10px 0;
        }}
        .event-description {{
            background-color: #f8f9fa;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
            color: #555;
            line-height: 1.6;
        }}
        .footer {{
            text-align: center;
            margin-top: 30px;
            color: #888;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📅 תזכורת לאירוע</h1>
        </div>
        
        <p>שלום {recipientName},</p>
        <p>רצינו להזכיר לך שיש לך אירוע היום!</p>
        
        <div class='event-title'>
            🎯 {eventTitle}
        </div>
        
        <div class='event-date'>
            📆 תאריך: {eventDate:dd/MM/yyyy} בשעה {eventDate:HH:mm}
        </div>
        
        <div class='event-description'>
            <strong>פרטי האירוע:</strong><br/>
            {eventDescription}
        </div>
        
        <div class='footer'>
            <p>מערכת לוח השנה שלך 🗓️</p>
            <p style='font-size: 12px; color: #aaa;'>
                מייל זה נשלח אוטומטית כתזכורת לאירוע שלך
            </p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
