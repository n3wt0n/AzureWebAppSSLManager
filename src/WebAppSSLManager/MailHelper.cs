using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebAppSSLManager.Models;

namespace WebAppSSLManager
{
    public static class MailHelper
    {
        private static ILogger _logger;
        private static IAsyncCollector<SendGridMessage> _messageCollector;

        public static void Init(ILogger logger, IAsyncCollector<SendGridMessage> messageCollector)
        {
            _logger = logger;
            _messageCollector = messageCollector;
        }

        public static async Task SendEmailForErrorAsync(Exception ex, string errorMessage)
        {
            var message = new StringBuilder("The WebAppSSLManager Azure Function has encountered an error.");
            message.AppendLine(errorMessage);
            message.AppendLine();
            message.AppendLine("Exception Message:");
            message.AppendLine(ex.Message);
            message.AppendLine();
            message.AppendLine("Exception StackTrace:");
            message.AppendLine(ex.StackTrace);
            message.AppendLine();

            if (ex.InnerException != null)
            {
                message.AppendLine("Inner Exception Message:");
                message.AppendLine(ex.InnerException.Message);
                message.AppendLine();
                message.AppendLine("InnerException StackTrace:");
                message.AppendLine(ex.InnerException.StackTrace);
                message.AppendLine();
            }

            await SendEmail("WebAppSSLManager - ERROR", message.ToString());
        }

        public static async Task SendEmailForActivityStartedAsync()
        {
            var message = $"The WebAppSSLManager Azure Function has started it's activity at {DateTime.Now}.";
            await SendEmail("WebAppSSLManager - Activity started", message);
        }

        public static async Task SendEmailForActivityCompletedAsync(List<(string hostname, string errorMessage)> errors)
        {
            var subject = "WebAppSSLManager - Activity completed";
            var message = $"The WebAppSSLManager Azure Function has completed it's activity at {DateTime.Now}";

            if (errors.Count > 0)
            {
                subject += " with errors";
                message += $"with {errors.Count} errors.{Environment.NewLine}The following errors occurred: {Environment.NewLine}";

                foreach (var (hostname, errorMessage) in errors)
                {
                    message += $"{hostname}: {errorMessage}{Environment.NewLine}";
                }
            }

            await SendEmail(subject, message);
        }

        private static async Task SendEmail(string subject, string message)
        {
            try
            {
                var emailMessage = new SendGridMessage();
                emailMessage.AddTo(Settings.CertificateOwnerEmail);
                emailMessage.AddContent("text/html", message);
                emailMessage.SetFrom(new EmailAddress(Settings.EmailSender));
                emailMessage.SetSubject(subject);

                await _messageCollector.AddAsync(emailMessage);

                _logger.LogInformation("Email Sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while sending email: {subject}");
            }
        }
    }
}
