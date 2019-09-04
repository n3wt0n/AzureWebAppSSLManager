using Microsoft.Extensions.Logging;
using System;

namespace WebAppSSLManager.Models
{
    public static class Settings
    {
        private static ILogger _logger;
        public static string CertificatePassword { get; private set; }

        public static void Init(ILogger logger)
        {
            _logger = logger;
            CertificatePassword = Environment.GetEnvironmentVariable("CertificatePassword") ?? Constants.DefaultCertificatePassword;
        }
    }


}
