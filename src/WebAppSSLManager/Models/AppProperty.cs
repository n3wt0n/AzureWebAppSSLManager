using System.Linq;

namespace WebAppSSLManager.Models
{
    public class AppProperty
    {
        //General properties
        public string Hostname { get; set; }
        public string ResourceName { get; set; } //either the WebApp name or the FunctionApp name
        public string ResourceResGroup { get; set; } //the resource group container the WebApp or the FunctionApp
        public string ResourcePlanResGroup { get; set; }
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

        public string PlanResourceGroup => ResourcePlanResGroup ?? ResourceResGroup;

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
