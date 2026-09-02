using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Marketplace.BLL.Common;
using Marketplace.BLL.Interfaces;
using Microsoft.Extensions.Options;

namespace Marketplace.BLL.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ISystemSettingsService _settingsService;

        public EmailSender(IOptions<EmailSettings> settings, ISystemSettingsService settingsService)
        {
            _settings = settings.Value;
            _settingsService = settingsService;
        }

        // The admin Integrations page can override any of these in SystemSettings; an
        // admin-saved value always wins, falling back to appsettings.json when nothing's
        // been saved yet (see AdminController.BuildIntegrationsAsync for the same pattern).
        private async Task<string> GetSettingAsync(string key, string fallback)
        {
            var overridden = await _settingsService.GetAsync("Email", key);
            return string.IsNullOrWhiteSpace(overridden) ? fallback : overridden;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var host = await GetSettingAsync("Host", _settings.Host);
            var portValue = await GetSettingAsync("Port", _settings.Port.ToString());
            var username = await GetSettingAsync("Username", _settings.Username);
            var password = await GetSettingAsync("Password", _settings.Password);
            var fromEmail = await GetSettingAsync("FromEmail", _settings.FromEmail);
            var fromName = await GetSettingAsync("FromName", _settings.FromName);
            var port = int.TryParse(portValue, out var parsedPort) ? parsedPort : _settings.Port;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlMessage };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
