using System.Collections.Generic;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace WebAppSSLManager
{
    public class ResourceConfiguration
    {
        public List<string> Hostnames { get; } = new List<string>();
        public Region Region { get; set; }
        public IResource Resource { get; set; }
        public List<IAppServiceCertificate> ExistingCertificates { get; set; }
    }
}
