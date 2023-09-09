using Microsoft.Extensions.Logging;
using System;

namespace WebAppSSLManager.Models
{
    public static class Settings
    {
        private static ILogger _logger;

        public static string SubscriptionID { get; private set; }
        public static string ServicePrincipalClientID { get; private set; }
        public static string ServicePrincipalClientSecret { get; private set; }
        public static string ServicePrincipalTenantID { get; private set; }
        public static string AzureStorageAccountConnectionString { get; private set; }
        public static string CertificateOwnerEmail { get; private set; }
        public static string CertificatePassword { get; private set; }
        public static string EmailSender { get; private set; }
        public static bool UseStaging { get; private set; }
        public static int BatchSize { get; private set; }
        public static int DaysBeforeExpiryToRenew { get; private set; }
        public static TimeSpan TimeBeforeExpiryToRenew { get; private set; }
        public static TimeSpan WaitTimeBeforeValidate { get; private set; }

        public static void Init(ILogger logger)
        {
            _logger = logger;

            SubscriptionID = Environment.GetEnvironmentVariable("SubscriptionID");
            if (string.IsNullOrWhiteSpace(SubscriptionID))
            {
                _logger.LogError("SubscriptionID environment variable is null");
                throw new ArgumentNullException("SubscriptionID environment variable is null");
            }

            ServicePrincipalClientID = Environment.GetEnvironmentVariable("ServicePrincipalClientID");
            if (string.IsNullOrWhiteSpace(ServicePrincipalClientID))
            {
                _logger.LogError("ServicePrincipalClientID environment variable is null");
                throw new ArgumentNullException("ServicePrincipalClientID environment variable is null");
            }

            ServicePrincipalClientSecret = Environment.GetEnvironmentVariable("ServicePrincipalClientSecret");
            if (string.IsNullOrWhiteSpace(ServicePrincipalClientSecret))
            {
                _logger.LogError("ServicePrincipalClientSecret environment variable is null");
                throw new ArgumentNullException("ServicePrincipalClientSecret environment variable is null");
            }

            ServicePrincipalTenantID = Environment.GetEnvironmentVariable("ServicePrincipalTenantID");
            if (string.IsNullOrWhiteSpace(ServicePrincipalTenantID))
            {
                _logger.LogError("ServicePrincipalTenantID environment variable is null");
                throw new ArgumentNullException("ServicePrincipalTenantID environment variable is null");
            }

            AzureStorageAccountConnectionString = Environment.GetEnvironmentVariable("AzureStorageAccountConnectionString");
            if (string.IsNullOrWhiteSpace(AzureStorageAccountConnectionString))
            {
                _logger.LogError("AzureStorageAccountConnectionString environment variable is null");
                throw new ArgumentNullException("AzureStorageAccountConnectionString environment variable is null");
            }

            CertificatePassword = Environment.GetEnvironmentVariable("CertificatePassword");
            if (string.IsNullOrWhiteSpace(CertificatePassword))
            {
                _logger.LogWarning("CertificatePassword environment variable is null. Reverting to default password");
                CertificatePassword = Constants.DefaultCertificatePassword;
            }

            CertificateOwnerEmail = Environment.GetEnvironmentVariable("CertificateOwnerEmail");
            if (string.IsNullOrWhiteSpace(CertificateOwnerEmail))
            {
                _logger.LogError("CertificateOwnerEmail environment variable is null");
                throw new ArgumentNullException("CertificateOwnerEmail environment variable is null");
            }

            EmailSender = Environment.GetEnvironmentVariable("EmailSender");
            if (string.IsNullOrWhiteSpace(EmailSender))
            {
                _logger.LogWarning("EmailSender environment variable is null. Reverting to default");
                EmailSender = Constants.DefaultEmailSender;
            }

            if(int.TryParse(Environment.GetEnvironmentVariable("BatchSize"), out int batchSize) && batchSize >= 0)
                BatchSize = batchSize;
            else
            {
                _logger.LogWarning("BatchSize environment variable is null or invalid. Reverting to default");
                BatchSize = Constants.DefaultBatchSize;
            }

            bool.TryParse(Environment.GetEnvironmentVariable("UseStaging"), out var useStaging);
            UseStaging = useStaging;

            if (int.TryParse(Environment.GetEnvironmentVariable("DaysBeforeExpiryToRenew"), out int days) && days >= 0)
                DaysBeforeExpiryToRenew = days;
            else
            {
                _logger.LogWarning("DaysBeforeExpiryToRenew environment variable is null or invalid. Reverting to default");
                DaysBeforeExpiryToRenew = Constants.DaysBeforeExpiryToRenew;
            }

            TimeBeforeExpiryToRenew = TimeSpan.FromDays(DaysBeforeExpiryToRenew);

            if (int.TryParse(Environment.GetEnvironmentVariable("WaitTimeBeforeValidateInSeconds"), out int seconds) && seconds >= 0)
                WaitTimeBeforeValidate = TimeSpan.FromSeconds(seconds);
            else
            {
                _logger.LogWarning("WaitTimeBeforeValidateInSeconds environment variable is null or invalid. Reverting to default");
                WaitTimeBeforeValidate = Constants.WaitTimeBeforeValidate;
            }
        }
    }
}
