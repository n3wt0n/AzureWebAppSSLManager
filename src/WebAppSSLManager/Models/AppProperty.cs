using System.Linq;

namespace WebAppSSLManager.Models
{
    public class AppProperty
    {
        //General properties
        public string Hostname { get; set; }
        public string ResourceName { get; set; } //either the WebApp name or the FunctionApp name
        public string ResourceResGroup { get; set; } //the resource group container the WebApp or the FunctionApp
        public string ResourcePlanResGroup { get; set; } //(optional) If the AppServicePlan is in a different resource group, this is the name of the Plan's resource group
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

        public string BaseDomain => AzureDnsZoneName;
  
        public string PlanResourceGroup => ResourcePlanResGroup ?? ResourceResGroup; // Use the resource group of the AppService or FunctionApp if it has not explictly been set for the AppServicePlan
        
        public string PfxFileName
            => $"{HostnameFriendly}.pfx";

        public string PemFileName
            => $"{HostnameFriendly}.pem";
    }
}
