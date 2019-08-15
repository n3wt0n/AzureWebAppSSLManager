using System.Linq;

namespace WebAppSSLManager.Models
{
    public class AppProperty
    {
        public string Hostname { get; set; }
        public string AzureWebAppName { get; set; }
        public string AzureWebAppResGroup { get; set; }
        public string AzureDnsZoneName { get; set; }
        public string AzureDnsResGroup { get; set; }

        public string HostnameFriendly
            => Hostname.Replace("*.", "");

        public string BaseDomain
        {
            get
            {
                var basedomain = HostnameFriendly;

                while (basedomain.Count(f => f == '.') > 1)
                {
                    basedomain = basedomain.Substring(basedomain.IndexOf('.') + 1);
                }

                return basedomain;
            }
        }

        public string PfxFileName
            => $"{HostnameFriendly}.pfx";

        public string PemFileName
            => $"{HostnameFriendly}.pem";
    }
}
