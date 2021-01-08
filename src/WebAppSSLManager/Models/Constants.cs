using System;

namespace WebAppSSLManager.Models
{
    public class Constants
    {
        public const string AppPropertiesFileName = "appproperties.json";
        public const string CertInfoFileName = "certinfo.json";
        public const string AccountKeyFileName = "accountkey.pem";
        public const string DefaultCertificatePassword = "C0mpl1c4t3d57r1ng";
        public const string CertificateBlobContainer = "certificates";
        public const string DefaultEmailSender = "AzureWebAppSSLManager@dbtek.com.hk";
        public const string DefaultCA = "Let's Encrypt Authority";
        public const string DefaultIntermediate = "R3";
        public const int DefaultBatchSize = 0;
        public const int DaysBeforeExpiryToRenew = 30;
    }
}
