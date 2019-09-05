using System.Linq;

namespace WebAppSSLManager.Models
{
    public class AppProperty
    {
        //General properties
        public string Hostname { get; set; }
        public string AzureWebAppName { get; set; }
        public string AzureWebAppResGroup { get; set; }
        public string AzureDnsZoneName { get; set; }
        public string AzureDnsResGroup { get; set; }

        //Slots-specific properties
        public bool IsSlot { get; set; }
        public string SlotName { get; set; }

        //FunctionApps-specific properties
        public bool IsFunctionApp { get; set; }

        //Others
        public string HostnameFriendly
            => Hostname.Trim().Replace("*.", "");

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
